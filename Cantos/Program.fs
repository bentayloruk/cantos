namespace Cantos

open System
open System.IO

module Program =

    type RunResult = 
        | Success = 0
        | Fail = 1 

    //Run cantos.  MAYBE move this elsewhere.
    let runCantos (siteConfig:SiteConfig) (inputConfig:InputConfig) (pathProcessors:list<SitePathProcessor>) = 

        //INPUT - get the input file infos.
        let inputFileInfos =
            fileInfos inputConfig siteConfig
            |> Seq.toList

        //CLEAN OUT DIR
        let cleanDirExceptGitFolder = Dir.cleanDir (fun di -> di.Name = ".git")
        cleanDirExceptGitFolder siteConfig.SiteOutPath.AbsolutePath 

        //OUTPUT
        //HACKED IN FOR NOW - ONLY OPERATING ON .MD FILES.
        inputFileInfos
        |> Seq.filter (fun input -> input.FileInfo.Extension = ".md")
        |> Seq.iter (fun input ->

            let srcPath = input.SitePath.AbsolutePath

            //Mess with output path if required.
            let destPath = 
                let p = input.SitePath.SwitchRoot(siteConfig.SiteOutPath.AbsolutePath).ChangeExtension(".html")
                if pathProcessors.Length > 0 then
                    pathProcessors |> Seq.fold (fun path proc -> proc path) p
                else p

            siteConfig.Tracer.Info(sprintf "Copying %s" input.SitePath.AbsolutePath)
            Dir.ensureDir destPath.AbsolutePath
            let md = Markdown.mdToHtml (File.readAllText srcPath)
            File.writeAllText destPath.AbsolutePath md)

    //Entry point.
    [<EntryPoint>]
    let main argv = 

        (*
        For now we are running the Cantos like Jekyll.
        However, plan this being done from an fsx script (FAKE style), command line or YAML config.  One to discuss.
        *)

        let write (msg:string) = Console.WriteLine(msg)

        //For now, if we have one arg, it is the path to the site source.
        if argv.Length = 1 then

            //Ensure directory arg is separator terminated (as SitePath demands it!).
            let path = argv.[0]
            let path = if Path.endsWithDirSeparatorChar path then path else path + Path.DirectorySeparatorChar.ToString()

            //Config like Jekyll for now.
            let siteConfig, inputConfig, pathProcessors = Jekyll.cantosConfig path 

            //Run it!
            runCantos siteConfig inputConfig pathProcessors
            write "Cantos success"
            0
        else
            write "Cantos fail"
            1
