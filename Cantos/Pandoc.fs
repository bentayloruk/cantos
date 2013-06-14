[<RequireQualifiedAccess>]
module Pandoc

open System.IO
open YamlDotNet.RepresentationModel
open System
open System.Diagnostics

///Looks in program file folder for Pandoc.exe
let findPandoc () =
    let pandocFolder = "Pandoc"
    let progFilePaths = [
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), pandocFolder); 
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), pandocFolder); 
        ]
    progFilePaths 
    |> Seq.map (fun path -> 
                            match Directory.Exists(path) with
                            | true -> Directory.GetFileSystemEntries(path, "pandoc.exe", SearchOption.AllDirectories)
                            | false -> [||]) 
    |> Seq.concat
    |> List.ofSeq
    |> function | [] -> None | h::_ -> printfn "Found pandoc here %s" h; Some(h)


//Runs pandoc for filePath to html.  Drops in same location.
let toHtml pandocPath (input:TextReader) =
    use p = new Process()
    p.StartInfo.UseShellExecute <- false
    p.StartInfo.RedirectStandardOutput <- true
    p.StartInfo.RedirectStandardError <- true
    p.StartInfo.FileName <- pandocPath
    let arguments = "-t html "
    p.StartInfo.Arguments <- arguments
    p.StartInfo.RedirectStandardInput <- true
    try
        //TODO this reading is quick and dirty.  Need to redo this.
        p.Start() |> ignore
        p.StandardInput.Write(input.ReadToEnd())
        p.StandardInput.Close()
        let output = p.StandardOutput.ReadToEnd()
        let failText = p.StandardError.ReadToEnd()
        p.WaitForExit()//TODO add back in some timeout facility like FAKE
        (p.ExitCode, failText, output)
    with
    | exn -> failwithf "Start of process %s failed. %s" p.StartInfo.FileName exn.Message



