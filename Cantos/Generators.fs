namespace Cantos

///Generators generate output StreamInfo instances.  These represent streams that can be processed and written.

[<AutoOpen>]
///Module containing the "standard" generator functions.
module Generators = 

    open System
    open System.IO
    open FrontMatter
     
    let fileStream path = File.Open(path, FileMode.Open, FileAccess.Read) :> Stream
    let tempFileExclusions:FileExclusion = fun fi -> fi.Name.EndsWith("~") || fi.Name.EndsWith(".swp")
    let appDirExclusions:DirectoryExclusion = fun di -> di.Name.StartsWith("_")
        
    let private offsetFileReader path skipLines = 
        let stream = fileStream path
        let reader = new StreamReader(stream)
        [1..skipLines] |> List.iter (fun _ -> reader.ReadLine() |> ignore)
        reader :> TextReader

    let fileReader path = offsetFileReader path 0

    let webStreamInfos tracer url skipUrl =
        raiseNotImpl "Placeholder for someday maybe generate from web content ;)"

    let getDescendantFileInfos (skipDir:DirectoryExclusion) (skipFile:FileExclusion) inRootPath =
        let filePaths = Dir.descendantFilePaths inRootPath skipDir
        seq { for filePath in filePaths do
              let fileInfo = FileInfo(filePath)
              if not (skipFile fileInfo) then yield fileInfo }

    ///Creates an Ouput from the given FileInfo location.
    let getOutputForFileInfo inRootPath outRootPath (fileInfo:FileInfo) =
        let path = fileInfo.FullName

        let rootedPath = 
            let length = if endsWithDirSeparatorChar inRootPath then inRootPath.Length else inRootPath.Length + 1 //Yuk.
            RootedPath.Create(outRootPath, path.Substring(length))

        let fmBlock =
            use reader = fileReader path 
            readFrontMatterBlock reader

        match fmBlock with

        | Some(fmBlock) ->
            let meta = metaValues fmBlock
            let readContents = fun () -> offsetFileReader path fmBlock.LineCount 
            TextOutput({ Path = rootedPath; Meta = meta; HadFrontMatter=true; ReaderF = readContents; })

        | None ->
            //No front matter so don't process (this is ala Jekyll but may change).
            BinaryOutput({ Path = rootedPath; Meta = Map.empty; StreamF = fun () -> fileStream path })

    ///Generates output for all files in and below inPath.  Skips temp files and directories beginnning with _.
    let dirOutputs inPath outPath = 
        getDescendantFileInfos appDirExclusions tempFileExclusions inPath
        |> Seq.map (getOutputForFileInfo inPath outPath)

    ///Generates the basic site content output.
    let siteOutputs inPath outPath (siteMeta:MetaMap) = 
        //No changes to meta.
        siteMeta, dirOutputs inPath outPath 

    let toBlogPost output =
        mapTextOutput (fun toi -> { toi with TextOutputInfo.Path = toi.Path.ChangeExtension(FileExtension.Create("post")) }) output
            
    ///Generates blog post output.
    let blogOutputs postsPath siteOutPath (siteMeta:MetaMap) = 
        let posts =
            dirOutputs postsPath siteOutPath
            |> Seq.map toBlogPost
            |> List.ofSeq//As we want counts etc for meta. 

        //Place holder for creating posts meta hash.
        let siteMeta = siteMeta.Add("posts.count", Int(posts.Length)) 

        siteMeta, posts :> seq<Output> 
