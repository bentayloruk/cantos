module Cantos.Jekyll

(*
Wires up Cantos to run sorta like Jekyll maybe ;)
*)

let cantosConfig (inPath:string) =

    let siteConfig =
        { SiteInPath = SitePath.Create(inPath, "")
          SiteOutPath = SitePath.Create(Path.combine [| inPath; @"_site\" |], "")
          Tracer = ConsoleTracer() }

    let inputConfig =

        let (dirFilters:list<DirectoryInfoExclusion>) = [ (fun di -> di.Name.StartsWith("_")) ]
        let (fileFilters:list<FileInfoExclusion>) = [ (fun fi -> fi.Name.EndsWith("~") || fi.Name.EndsWith(".swp")) ]

        { InputPath = siteConfig.SiteInPath
          InputDirExclusions = dirFilters 
          InputFileExclusions = fileFilters }

    let (pathProcessors:list<SitePathProcessor>) = [] 

    (siteConfig, inputConfig, pathProcessors)

