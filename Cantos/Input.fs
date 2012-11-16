namespace Cantos

open System
open System.IO

[<AutoOpen>]
module Input =
    open FrontMatter

    //---------- Types 
    type InputConfig = 
        { InputPath: SitePath;
          InputDirExclusions: list<DirectoryInfoExclusion>;
          InputFileExclusions: list<FileInfoExclusion>; }

    type InputFileInfo = 
        { FileInfo:FileInfo;
          SitePath:SitePath
          FrontMatter:option<FrontMatterValueList>;
          }

    //---------- Functions


    let private fileInfo path = FileInfo(path)
        
    ///Combines multiple predicates into a single predicate.
    let private combinePredicates<'a> predicates =
        fun (subject:'a) ->
            predicates 
            |> Seq.exists (fun test -> test(subject))

    //Catch and trace exceptions from front matter reading.
    let safeReadFrontMatter (fi:FileInfo) (tracer:ITracer) =
        try readFileFrontMatterArgs fi.FullName
        with
        | ex -> 
            tracer.Error (sprintf "Error reading front matter %s.  Path: %s" ex.Message fi.FullName)
            None

    ///Creates InputFileInfo records according to the provided config.
    let fileInfos inputConfig (siteConfig:SiteConfig) =

        let dirFilter = combinePredicates<DirectoryInfo> inputConfig.InputDirExclusions

        let inputFileInfoFilter = 
            let filter = combinePredicates<FileInfo> inputConfig.InputFileExclusions
            fun fileInfo -> if filter fileInfo = true then None else Some(fileInfo)

        let inputFileInfos = 
            let filePaths = Dir.descendantFilePaths inputConfig.InputPath.AbsolutePath dirFilter
            filePaths
            |> Seq.map fileInfo 
            |> Seq.choose inputFileInfoFilter 

        let fileInfoToInputInfo (fi:FileInfo) = 
            { FileInfo = fi;
              FrontMatter = safeReadFrontMatter fi siteConfig.Tracer;
              SitePath = siteConfig.SiteInPath.RelativeSitePath(fi.FullName) }

        inputFileInfos |> Seq.map fileInfoToInputInfo
