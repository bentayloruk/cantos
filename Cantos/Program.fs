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

        let fixPath = Path.getFullPath >> ensureEndsWithDirSeparatorChar
        let indexedArgOrF i f = if args.Length >= i+1 then args.[i] else f() 

        let srcPath = indexedArgOrF 0 (fun () -> "") |> fixPath
        let destPath = indexedArgOrF 1 (fun () -> Path.combine [| srcPath; "_site" |]) |> fixPath
        if not (Dir.exists srcPath) then raiseArgEx (sprintf "Source path does not exist.\n%s" srcPath) "Source Path"

        { SourcePath = srcPath;
          DestinationPath = destPath 
          PreviewServerPort = 8888 }

    //Bit of pre-amble...
    let logStart site = 
        logInfo "[ Cantos - F#st and furious static website generator ]"
        logInfo (sprintf "InPath: %s" (site.InPath.ToString()))
        logInfo (sprintf "OutPath: %s" (site.OutPath.ToString()))

    [<EntryPoint>]
    let main argv = 

        (*
        For now we are running Cantos with some defaults.
        Plan this being done from an fsx script (FAKE style), command line or YAML config.  One to discuss.
        *)

        try 
            //TODO config/options is bit of mess.  Clean this up when trying fsx approach.
            let options = optionsFromArgs argv;
            let runPreviewServer = fun () -> FireflyServer.runPreviewServer options.DestinationPath options.PreviewServerPort

            let siteMeta = MetaValue.Mapping(["time", MetaValue.DateTime(DateTime.Now)] |> Map.ofList)

            let site =
                { InPath = Uri(options.SourcePath)
                  OutPath = Uri(options.DestinationPath)
                  Meta = [ "site", siteMeta ] |> Map.ofList
                  RegisterTemplateType = initSafeType
                  }
            
            logStart site 

            //Clean output directory.
            Dir.cleanDir (fun di -> di.Name = ".git") options.DestinationPath 

            //Run generators.
            let outputs =
                [ generateBlog; generateBooks; generateBasicSite; ]
                |> Seq.map (fun g -> g site)
                |> Seq.concat

            //Set up the DotLiquid transform (our default and main templating engine).
            let liquidTransform = 
                //REVIEW tidy this up - Set up our "global" liquid environment (the includes and functions available).
                let includesPath = site.InPath.CombineWithParts(["_includes"])
                let rpf = renderParameters (IncludeFileSystem.Create(includesPath)) [typeof<JekyllFunctions>]
                liquidContentTransformer rpf 

            //Let people enhance the site meta.  Review: We do this due to streaming.  Too much?
            //Blog needs to content transform, so provide one.
            let contentTransform = markdownTransformer >> liquidTransform site.Meta
            let site = 
                let meta = 
                    Seq.fold (fun siteMeta metaMaker ->
                        let meta = metaMaker site siteMeta
                        meta
                        ) site.Meta [ (blogMeta contentTransform); ]
                { site with Meta = meta }

            //Transform and write content (new contentTransform with new Meta).
            let contentTransform = markdownTransformer >> liquidTransform site.Meta
            let siteTransform = layoutTransformer liquidTransform site
            outputs
            |> Seq.map (contentTransform >> siteTransform)
            |> Seq.iter writeContent

            logSuccess (sprintf "Success.  Output written to:\n%s.\n" options.DestinationPath)

            //Preview it!  //TODO preveiw based on cmd line flag.
            runPreviewServer() 
            logInfo ("Hosting site at http://localhost:" + options.PreviewServerPort.ToString())

            let _ = Console.ReadLine()
            0
        with
        | ex ->
            logError(sprintf "Cantos exception.\n%s" ex.Message)
            1
