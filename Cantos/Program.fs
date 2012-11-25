namespace Cantos

open System
open System.IO

module Program =

    ///App options.
    type options =
        { SourcePath:string
          DestinationPath:string
          PreviewServerPort:int } 

    ///Very basic args for now!
    let optionsFromArgs (args:array<string>) =
        let srcPath =   
            if args.Length > 0 then args.[0] else Dir.currentDir
            |> Path.getFullPath 
        if not (Dir.exists srcPath) then raiseArgEx (sprintf "Source path does not exist.\n%s" srcPath) "Source Path"
        { SourcePath = srcPath;
          DestinationPath = Path.combine [| srcPath; "_site" |]
          PreviewServerPort = 8888
          }

    [<EntryPoint>]
    let main argv = 

        (*
        For now we are running Cantos with some defaults.
        Plan this being done from an fsx script (FAKE style), command line or YAML config.  One to discuss.
        *)

        let tracer = ConsoleTracer() :> ITracer

        try 
            //For now, if we have one arg, it is the path to the site source.
            let options = optionsFromArgs argv;
            let srcPath, destPath = options.SourcePath, options.DestinationPath

            //Set up some default functions/values.
            let fileStreamInfos =
                let tempFileExclusions:FileExclusion = fun fi -> fi.Name.EndsWith("~") || fi.Name.EndsWith(".swp")
                let appDirExclusions:DirectoryExclusion = fun di -> di.Name.StartsWith("_")
                fileStreamInfosFiltered tracer appDirExclusions tempFileExclusions
            let srcRelativePath parts = Path.Combine(srcPath :: parts |> Array.ofList)
            let runPreviewServer = FireflyServer.runPreviewServer srcPath options.PreviewServerPort
                
            //Generate streams.
            let outputStreams = [

                //Basic site files.
                yield! fileStreamInfos srcPath destPath

                //Blog posts.
                //TODO map according to post properties.
                yield! fileStreamInfos (srcRelativePath [ "_posts" ]) destPath 
            ]

            //Process streams.
            let (processors:list<Processor>) = [ markdownProcessor ]
            let applyProcessors streamInfo = 
                processors |> List.fold (fun acc proc -> proc acc) streamInfo
            let processedStreams = outputStreams |> Seq.map applyProcessors 

            //Clean output directory.
            Dir.cleanDir (fun di -> di.Name = ".git") destPath 

            //Write
            let write output =
                match output with
                | TextOutput(t) ->
                    use tr = t.ReaderF()
                    File.WriteAllText(t.Path.AbsolutePath, tr.ReadToEnd())//Change to stream write.
                | BinaryOutput(b) ->
                    use fs = File.Create(b.Path.AbsolutePath)
                    use s = b.StreamF()
                    s.CopyTo(fs)

            processedStreams |> Seq.iter write

            tracer.Info (sprintf "Cantos success.  Wrote site to %s." destPath)

            //Preview it!
            runPreviewServer 
            let _ = Console.ReadLine()
            0
        with
        | ex ->
            tracer.Info "Cantos fail.  You must provide the site source path as the single argument."
            1
