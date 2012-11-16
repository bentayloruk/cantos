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
        
    ///Creates InputFileInfo records according to the provided config.
    let fileInfos inputConfig (siteConfig:SiteConfig) =

        let dirFilter = combinePredicates<DirectoryInfo> inputConfig.InputDirExclusions
        let fileFilter = combinePredicates<FileInfo> inputConfig.InputFileExclusions

        let inputDirs = seq {
            yield inputConfig.InputPath.AbsolutePath;
            yield! Dir.descendantDirs inputConfig.InputPath.AbsolutePath dirFilter }

        let inputFileInfos = 
            inputDirs 
            |> Seq.map Dir.getFiles
            |> Seq.concat
            |> Seq.map fileInfo 
            |> Seq.choose (fun fileInfo ->
                if fileFilter fileInfo = true then None 
                else Some(fileInfo))

        //Catch and trace exceptions from front matter reading.
        let safeReadFrontMatter (fi:FileInfo) =
            try readFileFrontMatterArgs fi.FullName
            with
            | ex -> 
                siteConfig.Tracer.Error (sprintf "Error reading front matter %s.  Path: %s" ex.Message fi.FullName)
                None

        let fileInfoToInputInfo (fi:FileInfo) = 
            { FileInfo = fi;
              FrontMatter = safeReadFrontMatter fi;
              SitePath = siteConfig.SiteInPath.RelativeSitePath(fi.FullName) }

        inputFileInfos |> Seq.map fileInfoToInputInfo
