namespace Cantos

///Generators generate output StreamInfo instances.  These represent streams that can be processed and written.

[<AutoOpen>]
module FileSystemGenerator = 

    open System
    open System.IO
    open FrontMatter
     
    let fileStream path = File.Open(path, FileMode.Open, FileAccess.Read) :> Stream
        
    let offsetFileReader path skipLines = 
        let stream = fileStream path
        let reader = new StreamReader(stream)
        [1..skipLines] |> List.iter (fun _ -> reader.ReadLine() |> ignore)
        reader :> TextReader

    let fileReader path = offsetFileReader path 0

    let webStreamInfos tracer url skipUrl =
        raiseNotImpl "Placeholder for someday maybe generate from web content ;)"

    let fileStreamInfosFiltered tracer (skipDir:DirectoryExclusion) (skipFile:FileExclusion) (inPath:FilePath) (outPath:FilePath) =
        let filePaths = Dir.descendantFilePaths inPath skipDir

        seq { for filePath in filePaths do
              let fileInfo = FileInfo(filePath)

              if not (skipFile fileInfo) then
                  use reader = fileReader filePath

                  let sitePath = SitePath.Create(outPath, filePath.Substring(inPath.Length))

                  yield

                      match readFrontMatterBlock reader with

                      | Some(fmBlock) ->
                        let meta = yamlArgs fmBlock
                        let reader = fun () -> offsetFileReader filePath fmBlock.LineCount 
                        TextOutput({ Path = sitePath; Meta = meta; HadFrontMatter=true; ReaderF = reader; })

                      | None ->
                        //Don't text process.
                        BinaryOutput({ Path = sitePath; Meta = Map.empty; StreamF = fun () -> fileStream filePath })
        }


