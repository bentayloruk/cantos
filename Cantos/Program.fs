namespace Cantos

open System
open System.IO
open Markdown

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
        |> Seq.iter (fun input ->

            //Get the in and out path.
            let srcPath = input.SitePath.AbsolutePath
            let destPath = 
                let p = input.SitePath.SwitchRoot(siteConfig.SiteOutPath.AbsolutePath)
                if pathProcessors.Length > 0 then
                    pathProcessors |> Seq.fold (fun path proc -> proc path) p
                else p

            //Copy (and maybe transform).
            siteConfig.Tracer.Info(sprintf "Copying %s" input.SitePath.AbsolutePath)
            Dir.ensureDir destPath.AbsolutePath

            match input.FrontMatter with 

            | Some(frontMatter) -> 

                match srcPath with

                | MarkdownFile(path) -> 
                    //TODO streams NOT file paths!
                    //TODO don't write the front matter!
                    let markdownContent = input.ContentReader().ReadToEnd()
                    let md = Markdown.mdToHtml markdownContent
                    let outPath = destPath.ChangeExtension(".html")
                    File.writeAllText outPath.AbsolutePath md

                | _ ->
                    //Copy the contents (using the reader that skips front matter).
                    let contents = input.ContentReader().ReadToEnd()
                    File.writeAllText destPath.AbsolutePath contents

            | None -> File.Copy(srcPath, destPath.AbsolutePath)

        )

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
            write (sprintf "Cantos success.  Wrote site to %s." siteConfig.SiteOutPath.AbsolutePath)
            0
        else
            write "Cantos fail.  You must provide the site source path as the single argument."
            1
