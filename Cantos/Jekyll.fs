module Cantos.Jekyll

(*
Wires up Cantos to run sorta like Jekyll maybe ;)
*)

let cantosConfig (inPath:string) =

    let siteOutPath =
        let path = @"_site" + Path.dirSeparator
        let path = Path.combine [| inPath; path |]
        SitePath.Create(path, "")

    let siteConfig =
        { SiteInPath = SitePath.Create(inPath, "")
          SiteOutPath = siteOutPath
          Tracer = ConsoleTracer() }

    let inputConfig =

        let (dirFilters:list<DirectoryInfoExclusion>) = [ (fun di -> di.Name.StartsWith("_")) ]
        let (fileFilters:list<FileInfoExclusion>) = [ (fun fi -> fi.Name.EndsWith("~") || fi.Name.EndsWith(".swp")) ]

        { InputPath = siteConfig.SiteInPath
          InputDirExclusions = dirFilters 
          InputFileExclusions = fileFilters }

    let (pathProcessors:list<SitePathProcessor>) = [] 

    (siteConfig, inputConfig, pathProcessors)

