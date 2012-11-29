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
        tracer.Info <| sprintf "Cantos started %s." (DateTime.Now.ToString())

        try 
            //For now, if we have one arg, it is the path to the site source.
            let options = optionsFromArgs argv;
            let srcPath, destPath = options.SourcePath, options.DestinationPath

            //Set up some default functions/values.
            let srcRelativePath parts = Path.Combine(srcPath :: parts |> Array.ofList)
            let srcRelativeRootedPath parts = RootedPath.Create(srcPath, Path.Combine(parts |> Array.ofList))
            let runPreviewServer = fun () -> FireflyServer.runPreviewServer destPath options.PreviewServerPort
                
            let (siteMeta:MetaMap) = Map.empty

            //List generators.
            let (generators:list<Generator>) = [
                (siteOutputs srcPath destPath)
                (blogOutputs (srcRelativePath [ "_posts" ]) destPath)
                ]

            //Apply generators.
            let rec run generators outputs meta =   
                match generators with
                | h::t ->
                    let m, o = h meta
                    run t (Seq.append outputs o)  m
                | [] -> meta, outputs
            let siteMeta, outputs = run generators [] Map.empty 
            let outputs = outputs |> List.ofSeq //As we want siteMeta fully built!

            //TODO decide if this is the best way to do TOC.
            //Add some toc meta.  
            let siteMeta, outputs = tocMeta (srcRelativeRootedPath [ "BookA" ]) siteMeta outputs
            let siteMeta, outputs = tocMeta (srcRelativeRootedPath [ "BookB" ]) siteMeta outputs

            //Clean output directory.
            Dir.cleanDir (fun di -> di.Name = ".git") destPath 

            //List the transformers.
            let transformers = [    
                liquidContentTransformer
                markdownTransformer
                (layoutTransformer (srcRelativePath [ "_layouts" ]))
                ]

            //Transform and write!
            for output in outputs do

                //Transform
                let output = transformers |> List.fold (fun acc proc -> proc acc) output 

                //Write
                match output with

                | TextOutput(toi) ->
                    Dir.ensureDir toi.Path.AbsolutePath
                    use r = toi.ReaderF()
                    File.WriteAllText(toi.Path.AbsolutePath, r.ReadToEnd())//Change to stream write.

                | BinaryOutput(b) ->
                    Dir.ensureDir b.Path.AbsolutePath
                    use fs = File.Create(b.Path.AbsolutePath)
                    use s = b.StreamF()
                    s.CopyTo(fs)

            //Preview it!
            tracer.Info (sprintf "Cantos success.  Wrote site to %s." destPath)
            //TODO preveiw based on cmd line flag.
            runPreviewServer() 
            let _ = Console.ReadLine()
            0
        with
        | ex ->
            tracer.Info <| sprintf "Cantos exception.\n%s" ex.Message
            1
