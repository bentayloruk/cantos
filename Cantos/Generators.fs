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

    let fileStreamInfosFiltered tracer (skipDir:DirectoryExclusion) (skipFile:FileExclusion) rootPath outPath =
        let filePaths = Dir.descendantFilePaths rootPath skipDir

        seq { for filePath in filePaths do
              let fileInfo = FileInfo(filePath)

              if not (skipFile fileInfo) then
                  use reader = fileReader filePath

                  let rootedPath =
                    let length = if endsWithDirSeparatorChar rootPath then rootPath.Length else rootPath.Length + 1 //Yuk.
                    RootedPath.Create(outPath, filePath.Substring(length))

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


