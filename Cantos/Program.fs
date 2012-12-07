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
            |> ensureEndsWithDirSeparatorChar

        if not (Dir.exists srcPath) then raiseArgEx (sprintf "Source path does not exist.\n%s" srcPath) "Source Path"

        { SourcePath = srcPath;
          DestinationPath = ensureEndsWithDirSeparatorChar (Path.combine [| srcPath; "_site" |])
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
                  Meta = [ "site", siteMeta ] |> Map.ofList }
            
            logStart site 

            //Clean output directory.
            Dir.cleanDir (fun di -> di.Name = ".git") options.DestinationPath 

            //Run generators.
            let generators = [ generateBlog; (*generateBooks;*) generateBasicSite; ]
            let site, outputs =
                generators
                |> Seq.fold (fun (site, outputs) generator ->
                    site, Seq.concat [ outputs; generator site ]) (site, Seq.empty)

            //Write content.
            outputs |> Seq.iter writeContent
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
