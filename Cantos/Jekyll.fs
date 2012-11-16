///Replicates default Jekyll configuration.
module Cantos.Jekyll

let runLikeJekyll (inPath:string) =

    let outPath = Path.combine [| inPath; "_site" |]

    let siteConfig =
        { SiteInPath = SitePath.Create(inPath, "")
          SiteOutPath = SitePath.Create(outPath, "")
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

    let (pathProcessors:list<SitePathProcessor>) = [] 

    //Output:  Prepare target folder.
    let cleanDirExceptGitFolder = Dir.cleanDir (fun di -> di.Name = ".git")
    cleanDirExceptGitFolder siteConfig.SiteOutPath.AbsolutePath 

    //Process markdown with no front matter.
    inputs
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

    ()
