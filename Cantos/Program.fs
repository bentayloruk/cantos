namespace Cantos

open System
open System.IO

module App = 

    [<EntryPoint>]
    let main argv = 

        let siteConfig =
            { SiteInPath = SitePath.Create(@"C:\Users\Ben Taylor\Projects\new.enticify.com\site\", "")
              SiteOutPath = SitePath.Create(@"C:\Users\Ben Taylor\Projects\enticify.com.cantos\", "")
              Tracer = ConsoleTracer() }

        //List files from SiteInPath.
        //Filter files from list.
        //Copy files to SiteOutPath. 

        //File and dir excludors. 
        let (dirFilters:list<DirectoryInfoExclusion>) = [ (fun di -> di.Name.StartsWith("_")) ]
        let (fileFilters:list<FileInfoExclusion>) = [ (fun fi -> fi.Name.EndsWith("~") || fi.Name.EndsWith(".swp")) ]

        //Input config.
        let inputConfig = 
            { InputPath = siteConfig.SiteInPath
              InputDirExclusions = dirFilters 
              InputFileExclusions = fileFilters }

        let inputs =
            inputFileInfos inputConfig siteConfig
            |> Seq.toList

        //Input Pass 2: Build the global context hash.
        //Global TOCs.
        let devToc = Toc.forPath (siteConfig.InSitePath(@"docs\commerce-server\developer\")) "DevToc"
        let adminToc = Toc.forPath (siteConfig.InSitePath(@"docs\commerce-server\user\")) "AdminToc"

        let deNumberWangToc (toc:Toc.Toc) (sitePath:SitePath) =
            if toc.SitePath.IsSameRelativePathOrParent(sitePath) then
                sitePath.SwitchRelative(Output.deNumberWangPath sitePath.RelativePath)
            else sitePath 

        let (pathProcessors:list<SitePathProcessor>) = 
            [ deNumberWangToc devToc;
              deNumberWangToc adminToc ]

        //let (pathProcessors:list<SitePathProcessor>) = [ (fun sitePath -> sitePath) ]

//        let templates = templateInfos(siteConfig.InSitePath(@"_templates\"))

        //Output:  Prepare target folder.
        let cleanDirExceptGitFolder = Dir.cleanDir (fun di -> di.Name = ".git")
        cleanDirExceptGitFolder siteConfig.SiteOutPath.AbsolutePath 

        //Process markdown with no front matter.
        inputs
        |> Seq.filter (fun input -> input.FileInfo.Extension = ".md")
        |> Seq.iter (fun input ->

            let srcPath = input.SitePath.AbsolutePath

            let destPath = 
                let p = input.SitePath.SwitchRoot(siteConfig.SiteOutPath.AbsolutePath).ChangeExtension(".html")
                pathProcessors |> Seq.fold (fun path proc -> proc path) p

            siteConfig.Tracer.Info(sprintf "Copying %s" input.SitePath.AbsolutePath)
            Dir.ensureDir destPath.AbsolutePath
            let md = Markdown.mdToHtml (File.readAllText srcPath)
            File.writeAllText destPath.AbsolutePath md)

        //Global Posts.
        //Global etc

        //Input Pass 3:  
        //Do the copy and transforms.

        0