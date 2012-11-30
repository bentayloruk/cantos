namespace Cantos

[<AutoOpen>]
///Module containing the "standard" generator functions.
module Generators = 

    open System
    open System.IO
    open FrontMatter
     
    let tempFileExclusions:FileExclusion = fun fi -> fi.Name.EndsWith("~") || fi.Name.EndsWith(".swp")
    let appDirExclusions:DirectoryExclusion = fun di -> di.Name.StartsWith("_")

    let getFileInfosEx exclude path = 
        seq { for path in Directory.GetFiles(path) do
                let fi = FileInfo(path)
                if not (exclude fi) then yield fi }

    //Shadow the filtered version for now.
    let getFileInfos = getFileInfosEx tempFileExclusions

    ///Gets all descendant files (bar filtered) and their relative path as an array of parts (inc. filename).
    let rec descendantFiles path exDir exFile parts =
        seq { 
                for file in Directory.GetFiles(path) do
                    let fi = FileInfo(file)
                    if not (exFile fi) then
                        yield fi, fi.Name :: parts |> List.rev |> Array.ofList

                for dir in Directory.GetDirectories(path) do
                    let dirInfo = new System.IO.DirectoryInfo(dir)
                    if not (exDir dirInfo) then
                        yield! descendantFiles dir exDir exFile (dirInfo.Name::parts)
            }

    ///Creates an Ouput from the given FileInfo location.
    let getOutput outPath (fileInfo:FileInfo) =
        let path = fileInfo.FullName
        let hadFrontMatter, lineCount, meta = getFileFrontMatterMeta path
        if hadFrontMatter then
            let readContents = fun () -> File.offsetFileReader path lineCount 
            TextOutput({ Path = outPath; Meta = meta; HadFrontMatter=true; ReaderF = readContents; })
        else
            //No front matter so don't process (this is ala Jekyll but may change).
            BinaryOutput({ Path = outPath; Meta = Map.empty; StreamF = fun () -> File.fileReadStream path })

    ///Generates the basic site content output.
    let siteOutputs (site:Site) = 
        let outputs = 
            descendantFiles site.InPath.AbsolutePath appDirExclusions tempFileExclusions []
            |> Seq.map (fun (fi, parts) -> 
                let path = site.OutPath.CreateRelative(Path.Combine(parts))
                getOutput path fi
            )
        site.Meta, outputs//dirOutputsRec site.InPath.AbsolutePath site.OutPath

    let toBlogPost (output:Output) = output
            
    ///Generates blog post output.
    let blogOutputs dirName (site:Site) = 
        
        let path = site.InPath.CreateFeaturePath(dirName)
        let outPath = site.OutPath.CreateRelative("posts")
        let posts =
            getFileInfos path.AbsolutePath
            //TODO get path information from blog post file name.  Generalise this outputs business.
            |> Seq.map (fun fi -> getOutput (outPath.CreateRelative(fi.Name)) fi)
            |> Seq.map toBlogPost
            //|> Seq.filter metaPublished
            |> List.ofSeq//As we want counts etc for meta. 

        //Place holder for creating posts meta hash.
        let siteMeta = site.Meta.Add("posts.count", Int(posts.Length)) 

        siteMeta, posts :> seq<Output> 

///Used to create one or more books in the site.
[<AutoOpen>]
module BookGenerator =

    ///Removes leading numbers and -.  Example:  1010-myname -> myname.  
    let deNumberWang (name:string) =
        let wangIndex = name.IndexOf('-')
        if wangIndex = -1 then name else
            let maybeNumber = name.Substring(0, wangIndex)
            let (parsed, number) = System.Int32.TryParse(maybeNumber)
            if parsed = true then name.Substring(wangIndex+1) else name 

    ///Removes leading numbers from file and dir paths (e.g. /2222-dirname/1234-file.html -> /dirname/file.html).
    let deNumberWangPath (path:string) =
        //Example:
        //This -> "developer\0100-introduction\0075-enticify-connector-for-commerce-server.md"
        //Becomes this -> "developer\introduction\enticify-connector-for-commerce-server.md"
        path.Split(Path.dirSeparatorChars)
        |> Array.map deNumberWang 
        |> Path.combine

    let deNumberWangRelativePath (path:RootedPath) =
        let rel = path.RelativePath
        path.SwitchRelative(deNumberWangPath rel)

    open System.IO
    open FrontMatter

    ///Represents a Table of Contents.
    type Toc = { Chapters:list<Chapter>; Path:RootedPath; }
    and Chapter = { Headings:list<Heading> }
    and Heading = { Href:string; Title:string; EnableLink: bool; }

    ///Creates an AHead for from a file.
    let maybeHeadingFromRootedPath sitePath = 
        Some( { Heading.Href = "TBD"; Title = "TBD"; EnableLink = true; } )

    ///Get dir structure from dotted directory name.
    let dirPathConvention path = DirectoryInfo(path).Name.Split('.') |> Path.combine

    ///Creates a TOC for files in the given site path.
    let toBookOutputs (path:string) (site:Site) = 
        
        let bookRootPath = site.OutPath.CreateRelative(dirPathConvention path)

        //Get chapters...
        Dir.getDirs path
        |> Seq.map (fun chapterPath ->
            let chapterDir = deNumberWang (DirectoryInfo(chapterPath).Name)
            //Get pages...
            getFileInfos chapterPath
            |> Seq.map (fun fi ->
                let outPath = Path.Combine(chapterDir, deNumberWang fi.Name) 
                let outPath = bookRootPath.CreateRelative(outPath)
                getOutput outPath fi) 
            )
        |> Seq.concat
            
        //TODO put the TOC stuff back.
        (*
        let headings = 

            Dir.getFiles chapterDir
            |> Seq.map (fun filePath -> path.RelativeRootedPath(filePath)) 
            |> Seq.choose maybeHeadingFromRootedPath 
            |> List.ofSeq

        if headings.Length > 0 then
            yield { Chapter.Headings = headings }

        { 
            Path = path;
            Chapters = chaptersWithAtLeastOneHeading;
        }
        *)

    ///Generates blog post output.
    let bookOutputs dirName (site:Site) = 
        let path = site.InPath.CreateFeaturePath(dirName)
        if Dir.exists path.AbsolutePath then
            let outputs = seq {
                for path in Dir.getDirs path.AbsolutePath do
                    yield! toBookOutputs path site } 
            site.Meta, outputs 
        else
            site.Tracer.Error(sprintf "Books generator is configured but path does not exist.  Looking in: %s" path.AbsolutePath)
            site.Meta, Seq.empty

