namespace Cantos

///Generators generate output StreamInfo instances.  These represent streams that can be processed and written.

[<AutoOpen>]
module FileSystemGenerator = 

    open System
    open System.IO
    open FrontMatter
     
    let fileStream path = File.Open(path, FileMode.Open, FileAccess.Read) :> Stream
    let tempFileExclusions:FileExclusion = fun fi -> fi.Name.EndsWith("~") || fi.Name.EndsWith(".swp")
    let appDirExclusions:DirectoryExclusion = fun di -> di.Name.StartsWith("_")
        
    let offsetFileReader path skipLines = 
        let stream = fileStream path
        let reader = new StreamReader(stream)
        [1..skipLines] |> List.iter (fun _ -> reader.ReadLine() |> ignore)
        reader :> TextReader

    let fileReader path = offsetFileReader path 0

    let webStreamInfos tracer url skipUrl =
        raiseNotImpl "Placeholder for someday maybe generate from web content ;)"

    let fileStreamInfosFiltered (skipDir:DirectoryExclusion) (skipFile:FileExclusion) inRootPath outRootPath =
        let filePaths = Dir.descendantFilePaths inRootPath skipDir

        seq { for filePath in filePaths do
              let fileInfo = FileInfo(filePath)

              if not (skipFile fileInfo) then
                  use reader = fileReader filePath

                  let rootedPath =
                    let length = if endsWithDirSeparatorChar inRootPath then inRootPath.Length else inRootPath.Length + 1 //Yuk.
                    RootedPath.Create(outRootPath, filePath.Substring(length))

                  yield

                      match readFrontMatterBlock reader with

                      | Some(fmBlock) ->
                        let meta = yamlArgs fmBlock
                        let reader = fun () -> offsetFileReader filePath fmBlock.LineCount 
                        TextOutput({ Path = rootedPath; Meta = meta; HadFrontMatter=true; ReaderF = reader; })

                      | None ->
                        //Don't text process.
                        BinaryOutput({ Path = rootedPath; Meta = Map.empty; StreamF = fun () -> fileStream filePath })
        }


    let templateGenerator sourcePath = 
        //Cheat and use stream function and then map.
        let toTemplate outputInfo = 
            match outputInfo with 
            | TextOutput(textOutput) -> Some({Name = textOutput.Path.FileName}) 
            | BinaryOutput(_) -> None 

        let templateStreams = fileStreamInfosFiltered appDirExclusions tempFileExclusions sourcePath sourcePath

        templateStreams
        |> Seq.choose toTemplate





