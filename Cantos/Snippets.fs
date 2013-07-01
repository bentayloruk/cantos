module Snippets

open System
open System.Text.RegularExpressions
open System.IO

type SnippetLine = {
    SourceLineNum:int
    SnippetLineNum:int
    Text:string
    }

type Snippet = {
    Id:string
    File:string
    Lines:SnippetLine list
    }

let private (|SnippetOpenLine|_|) (tag:string) (line:string) = 
    if Regex.IsMatch(line, "^(\s*)//(\s*)(?i)" + tag + "(\s*):") then
        let id = line.Substring(line.IndexOf(':')+1).Trim()
        Some(id)
    else None

let private (|SnippetCloseLine|) (tag:string) (line:string) = 
    Regex.IsMatch(line, "^(\s*)//(\s*)(?i)END" + tag + "(\s*)")

type SnippetParserArgs = {
    SnippetTag:string
    FileNamePatterns:string list
    FileExceptionHandler:exn -> string -> unit
    LineExceptionHandler:exn -> string -> unit
    DirectoryExceptionHandler:exn -> string -> unit
    }

let private snippets args file =
    File.ReadLines(file)
    |> Seq.fold (fun (lineNum, snippet, snippets) line ->
        let lineNum = lineNum + 1

        match snippet with

        | None -> 
            match line with
            | SnippetOpenLine args.SnippetTag snippetId ->
                //Found open line, so fire up new snippet.
                let snippet = {
                    Snippet.Id = snippetId 
                    File = file
                    Lines = []
                    }
                (lineNum, Some(snippet), snippets)
            | _ -> (lineNum, None, snippets)

        | Some(snippet) ->
            match line with
            | SnippetOpenLine args.SnippetTag id ->
                failwith (sprintf "Encountered a %s open line before the previous %s closed.  Details:\n%i:%s" args.SnippetTag args.SnippetTag lineNum file)
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

let private parsesnippets specifiedDirectories (args:SnippetParserArgs) =
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

let search tag searchPatterns path =
    let args = {
        SnippetTag = tag 
        FileNamePatterns = searchPatterns
        FileExceptionHandler = fun _ _ -> ()
        LineExceptionHandler = fun _ _ -> ()
        DirectoryExceptionHandler = fun _ _ -> ()
    }
    parsesnippets [path] args

let searchForSamples searchPatterns path = search "SAMPLE" searchPatterns path