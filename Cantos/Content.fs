namespace Cantos

[<AutoOpen>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Content = 

    open System
    open System.IO
    open System.Diagnostics

    open FrontMatter

    let htmlExtension = ".html"
    let markdownFileExtensions = [".md"; ".markdown";]

    let (|Text|_|) = function | TextContent(x) -> Some(x) | BinaryContent(_) -> None

    let (|Meta|_|) = function | TextContent(x) -> Some(x.Meta) | BinaryContent(x) -> Some(x.Meta)

    let (|Markdown|_|) (content:Content) =
        match content with
        | TextContent(x) ->
            if x.Uri.HasFileExtension(markdownFileExtensions) then Some(x) else None
        | BinaryContent(_) -> None

    let getUri = function | TextContent(x) -> x.Uri | BinaryContent(x) -> x.Uri

    ///Creates an outUri,Content pair.  Out uri is relative to site out path as was to site in path.
    let outUriContent site content =
        let uri =
            //Of course, assumes no content path is outside of InPath...
            let uri =
                getUri content
                |> site.InPath.MakeRelativeUri
                |> site.OutPath.CombineWithRelativeUri

            //TODO - not sure I like this.  What if it really is raw markdown!  Go back to content format?
            match content with
            | Markdown md -> uri.WithExtension(htmlExtension)
            | _ -> uri

        uri, content

    let textTransform transform textContent =
        let wrap = fun () ->
            use reader = textContent.ReaderF()
            transform reader :> TextReader
        TextContent({textContent with ReaderF = wrap})
        
    //Review:  Make this generic so can match can be for any Content type?
    let matchTextTransform (|Pat|_|) transform (content:Content) =
        match content with
        | Pat x -> textTransform transform x
        | _ -> content

    ///Applys the site Content and Site transformers.
    let applyTransforms site transforms content = transforms |> Seq.fold (fun c t -> t site c) content 

    //Creates Content for the file at path.
    let getContent path =
        let hadFrontMatter, lineCount, meta = getFileFrontMatterMeta path
        if hadFrontMatter then
            let readContents = fun () -> File.offsetFileReader path lineCount 
            TextContent({ Meta = meta; HadFrontMatter=true; ReaderF = readContents; Uri = Uri(path); })
        else
            //TODO 
            BinaryContent({ Meta = Map.empty; Uri = Uri(path); StreamF = fun () -> File.fileReadStream path;  })

    let hasFrontMatter (content:Content) =
        match content with
        | TextContent(x) -> if x.HadFrontMatter then Some(content) else None
        | BinaryContent(_) -> None

    let textContent (content:Content) = match content with | TextContent(x) -> Some(content) | _ -> None

    ///Write content to the provided uri.
    let writeContent (uri:Uri, content) = 

        match content with

        | TextContent(x) ->
            Dir.ensureDir uri.LocalPathUnescaped
            use r = x.ReaderF()
            File.WriteAllText(uri.LocalPathUnescaped , r.ReadToEnd())//Change to stream write.

        | BinaryContent(x) ->
            Dir.ensureDir uri.LocalPathUnescaped
            use fs = File.Create(uri.LocalPathUnescaped)
            use s = x.StreamF()
            s.CopyTo(fs)

    //TODO this file stuff is ugly here.  Decide if compact with IO.
    let tempFileExclusions:FileExclusion = fun fi -> fi.Name.EndsWith("~") || fi.Name.EndsWith(".swp")
    let appDirExclusions:DirectoryExclusion = fun di -> di.Name.StartsWith("_")

    let getFileInfosEx exclude path = 
        seq { for path in Directory.GetFiles(path) do
                let fi = FileInfo(path)
                if not (exclude fi) then yield fi }

    //Shadow the filtered version for now.
    let getFileInfos = getFileInfosEx tempFileExclusions

    ///Gets all descendant files (bar filtered) and their relative path as an array of parts (inc. filename).
    let descendantFilesEx path exDir exFile =
        let rec inner path' parts =
            seq { 
                    for file in Directory.GetFiles(path') do
                        let fi = FileInfo(file)
                        if not (exFile fi) then
                            let parts = fi.Name :: parts |> List.rev |> Array.ofList
                            yield fi.FullName, parts, Path.Combine(parts)

                    for dir in Directory.GetDirectories(path') do
                        let dirInfo = new DirectoryInfo(dir)
                        if not (exDir dirInfo) then
                            yield! inner dir (dirInfo.Name::parts)
                }
        inner path []

    //Get all descendant files and their relative path as an array.  Excludes using tempFileExclusions and appDirExclusions.
    let descendantFiles (uri:Uri) = descendantFilesEx uri.LocalPathUnescaped appDirExclusions tempFileExclusions

    let descendantFilePaths (uri:Uri) = descendantFiles uri |> Seq.map (fun (path, _, _) -> path)

    ///Gets all the child files.
    let childFilePaths path =
        Dir.getFiles path
        |> Seq.map (fun path -> 
            let fi = FileInfo(path)
            fi.FullName, [| fi.Name |], fi.Name
            )

    ///Gets all the child files.
    let childFilePathsEx (uri:Uri) = Dir.getFiles uri.LocalPathUnescaped