module Snippets

open System
open System.Text.RegularExpressions
open System.IO

type Line = {
    SourceLineNum:int
    SnippetLineNum:int
    Text:string
    }

type Snippet = {
    Id:string
    File:string
    Lines:Line list
    }

let (|SnippetOpenLine|_|) (tag:string) (line:string) = 
    if Regex.IsMatch(line, "^(\s*)//(\s*)(?i)" + tag + "(\s*):") then
        let id = line.Substring(line.IndexOf(':')+1).Trim()
        Some(id)
    else None

let (|SnippetCloseLine|) (tag:string) (line:string) = 
    Regex.IsMatch(line, "^(\s*)//(\s*)(?i)END" + tag + "(\s*)")

type SnippetParserArgs = {
    SnippetTag:string
    FileNamePatterns:string list
    FileExceptionHandler:exn -> string -> unit
    LineExceptionHandler:exn -> string -> unit
    DirectoryExceptionHandler:exn -> string -> unit
    }

let snippets args file =
    File.ReadLines(file)
    |> Seq.fold (fun (lineNum, snippet, snippets) line ->
        let lineNum = lineNum + 1

        match snippet with

        | None -> 
            match line with
            | SnippetOpenLine args.SnippetTag snippetId ->
                let snippet = {
                    Snippet.Id = snippetId 
                    File = file
                    Lines = []
                    }
                (lineNum, Some(snippet), snippets)
            | _ -> (lineNum, None, snippets)

        | Some(snippet) ->
            match line with
            | SnippetOpenLine args.SnippetTag id -> failwith "Encountered a snippet start line before the previous snippet closed." 
            | SnippetCloseLine args.SnippetTag true ->
                let snippet = {snippet with Lines = List.rev snippet.Lines }
                (lineNum, None, snippet :: snippets)
            | _ ->
                let line = {
                    SourceLineNum = lineNum
                    SnippetLineNum = snippet.Lines.Length + 1
                    Text = line
                    }
                (lineNum, Some({ snippet with Lines = line :: snippet.Lines }), snippets)

    ) (0, None, [])
    |> fun (_,_,snippets) -> snippets 
    |> List.ofSeq
    |> List.rev//Does not really matter as we reference them by Id, but why not.

let parsesnippets specifiedDirectories (args:SnippetParserArgs) =
    seq {
        for specifiedDir in specifiedDirectories do
            for search in args.FileNamePatterns do
                let files =
                    try Directory.EnumerateFiles(specifiedDir, search, SearchOption.AllDirectories)
                    with e -> args.DirectoryExceptionHandler e specifiedDir; Seq.empty 
                for file in files do
                    yield snippets args file
        }
        |> Seq.concat

let printSnippets path snippetTag =
    let args = {
        SnippetTag = snippetTag 
        FileNamePatterns = ["*.cs"]
        FileExceptionHandler = fun _ _ -> ()
        LineExceptionHandler = fun _ _ -> ()
        DirectoryExceptionHandler = fun _ _ -> ()
    }
    for snippet in parsesnippets [path] args do
        printfn "%s: %s" snippetTag snippet.Id
        printfn "%s" snippet.File
        for line in snippet.Lines do
            printfn "%i: %s" line.SourceLineNum line.Text

let printSamples path = printSnippets path "SAMPLE"