module Cantos.Enticify

(*
This is @bentayloruk's personal playground for building www.enticify.com
You probably want to look at the Jekyll module.
*)

let cantosConfig () =

    //Simple site config.
    let siteConfig =
        { SiteInPath = SitePath.Create(@"C:\Users\Ben Taylor\Projects\new.enticify.com\site\", "")
          SiteOutPath = SitePath.Create(@"C:\Users\Ben Taylor\Projects\enticify.com.cantos\", "")
          Tracer = ConsoleTracer() }

    //INPUT
    let inputFileInfos =

        let (dirFilters:list<DirectoryInfoExclusion>) = [ (fun di -> di.Name.StartsWith("_")) ]
        let (fileFilters:list<FileInfoExclusion>) = [ (fun fi -> fi.Name.EndsWith("~") || fi.Name.EndsWith(".swp")) ]

        let inputConfig = 
            { InputPath = siteConfig.SiteInPath
              InputDirExclusions = dirFilters 
              InputFileExclusions = fileFilters }

        fileInfos inputConfig siteConfig |> Seq.toList

    //TOCs
    let devToc = Toc.forPath (siteConfig.InSitePath(@"docs\commerce-server\developer\")) "DevToc"
    let adminToc = Toc.forPath (siteConfig.InSitePath(@"docs\commerce-server\user\")) "AdminToc"

    let deNumberWangToc (toc:Toc.Toc) (sitePath:SitePath) =
        if toc.SitePath.IsSameRelativePathOrParent(sitePath) then
            sitePath.SwitchRelative(Output.deNumberWangPath sitePath.RelativePath)
        else sitePath 

    let (pathProcessors:list<SitePathProcessor>) = 
        [ deNumberWangToc devToc;
          deNumberWangToc adminToc ]

    //CLEAN OUT DIR
    let cleanDirExceptGitFolder = Dir.cleanDir (fun di -> di.Name = ".git")
    cleanDirExceptGitFolder siteConfig.SiteOutPath.AbsolutePath 

    //OUTPUT
    inputFileInfos 
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


