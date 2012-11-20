module Cantos.Template

///Template types.
type TemplateName = string
type Template = { Name:TemplateName}
type TemplateList = list<Template>
type TemplateMap = Map<TemplateName, Template>
type TemplateProvider = unit -> TemplateList

open System.IO
open FrontMatter

(*
let dirTemplateProvider path filter =
    ()
    Dir.getFiles path
    |> Seq.filter filter 
    |> Seq.map (fun templatePath ->
        try
            let args = readFileFrontMatterArgs templatePath
            let parent = 
                match args with
                | Some(valMap) when valMap.Contains("template") ->
                | None -> None
            let parent = fmArgs.getArgValueOpt "template" fmArgs
            let template = reader.ReadToEnd()
            let templateFile = Path.GetFileName(templatePath)
            let values = valueTuples fmArgs |> List.ofSeq
            Some((templateFile, {FileName=templateFile; Template=template; ParentFileName=parent; Vars = values}))
        with
        | :? IOException -> None 
    )
    |> Seq.choose (fun x -> x)
    |> Map.ofSeq
*)
