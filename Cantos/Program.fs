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
          PreviewServerPort = 8888
          }

    [<EntryPoint>]
    let main argv = 

        (*
        For now we are running Cantos with some defaults.
        Plan this being done from an fsx script (FAKE style), command line or YAML config.  One to discuss.
        *)

        try 
            //TODO config/options is bit of mess.  Clean this up when trying fsx approach.
            let options = optionsFromArgs argv;
            let srcPath, destPath = options.SourcePath, options.DestinationPath
            let runPreviewServer = fun () -> FireflyServer.runPreviewServer destPath options.PreviewServerPort

            let site =
                { InPath = Uri(srcPath)
                  OutPath = Uri(destPath)
                  Meta = Map.empty }
                
            //Clean output directory.
            Dir.cleanDir (fun di -> di.Name = ".git") destPath 

            //TODO build the meta.
            let (siteMeta:MetaMap) = Map.empty

            //Run generators.
            let generators = [ generateBlog; (*generateBooks;*) generateBasicSite; ]
            let outputs = seq { for generator in generators do yield! generator site } 

            //Write content.
            outputs |> Seq.iter writeContent
            logSuccess (sprintf "Cantos success.  Wrote site to %s." destPath)

            //Preview it!  //TODO preveiw based on cmd line flag.
            runPreviewServer() 

            let _ = Console.ReadLine()
            0
        with
        | ex ->
            logError(sprintf "Cantos exception.\n%s" ex.Message)
            1
