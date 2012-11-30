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
            //TODO config/options is bit of mess.  Clean this up when trying fsx approach.
            let options = optionsFromArgs argv;
            let srcPath, destPath = options.SourcePath, options.DestinationPath
            let site =
                { InPath = RootedPath.Create(srcPath, "")
                  OutPath = RootedPath.Create(destPath, "")
                  Tracer = tracer
                  Meta = Map.empty }

            //Set up some default functions/values.
            let srcRelativePath parts = Path.Combine(srcPath :: parts |> Array.ofList)
            let srcRelativeRootedPath parts = RootedPath.Create(srcPath, Path.Combine(parts |> Array.ofList))
            let runPreviewServer = fun () -> FireflyServer.runPreviewServer destPath options.PreviewServerPort
                
            let (siteMeta:MetaMap) = Map.empty

            //List generators.
            let (generators:list<Generator>) = [
                siteOutputs
                (blogOutputs "_posts")
                (bookOutputs "_books")
                ]

            //Apply generators.
            let rec run generators outputs site =   
                match generators with
                | h::t ->
                    let meta, o = h site 
                    //We update site with returned meta as we don't want generators messing with read only paths!
                    let site = { site with Site.Meta = meta} 
                    run t (Seq.append outputs o) site 
                | [] -> site, outputs
            let siteMeta, outputs = run generators [] site
            let outputs = outputs |> List.ofSeq //As we want siteMeta fully built!

            //Clean output directory.
            Dir.cleanDir (fun di -> di.Name = ".git") destPath 

            //List the transformers.
            let transformers = [    
                liquidContentTransformer
                markdownTransformer
                (layoutTransformer "_layouts")
                ]

            //Transform and write!
            for output in outputs do

                //Transform
                let output =
                    transformers
                    |> List.fold (fun x f -> f site x) output 

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
