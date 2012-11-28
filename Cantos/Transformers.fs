namespace Cantos

[<AutoOpen>]
module TemplateEngines =

    open DotLiquid
    open System.IO
        
    let liquidTransform (reader:TextReader) =
        let template = Template.Parse(reader.ReadToEnd())
        new StringReader(template.Render())

///Markdown conversion.
[<AutoOpen>]
module MarkdownTransformer =

    open System.IO
    open MarkdownDeep
    open System

    ///Turns a markdown stream into and html stream.  Returns a new Uri with .html extension.
    let markdownTransformer (output:Output) = 
        
        if output.HasExtension(["md";"markdown";]) then

            let markdownTransform (reader:TextReader) =
                let md = Markdown()
                md.ExtraMode <- false
                md.SafeMode <- false
                let html = md.Transform(reader.ReadToEnd())
                new StringReader(html)

            let output = output.ChangeExtension("html")
            output.DecorateTextOutputReader(markdownTransform)

        else output
        
[<AutoOpen>]
module ContentTransformer = 
    
    ///Transforms the content out output using the Liquid templating engine.
    ///Does not render templates (this allows content post processing with other transformers).
    let liquidContentTransformer (output:Output) = output.DecorateTextOutputReader(liquidTransform)

[<AutoOpen>]
module LayoutTransformer = 

    open System.IO
    open FrontMatter
    open System.Collections.Generic
    open System

    ///Transforms outputs that have "layout" meta.
    let layoutTransformer layoutPath (output:Output) =

        ///Create a map of layouts below sourcePath.
        let templateMap =
            getDescendantFileInfos appDirExclusions tempFileExclusions layoutPath
            |> Seq.map (fun fileInfo ->
                let path = fileInfo.FullName
                use fileReader = fileReader path 
                Path.GetFileName(path).ToLower(), fileReader.ReadToEnd())
            |> Map.ofSeq

        let toDictionary pairs =
            let dic = Dictionary<string,obj>()
            for (k,v) in pairs do dic.Add(k, v)
            dic


        match output with

        | TextOutput(x) ->
            
            match maybeStringScalar "template" x.Meta with

            | Some(value) -> 

                let value = value.ToLower() + ".liq"//TODO hacked in Liquid for now.
                output
                    (*
                if templateMap.ContainsKey(value) then
                    let content = fun () -> new StringReader(liquidTransform templateMap.[value]) :> TextReader
                    //Render the leaf document.
                    let d = toDictionary output. 
                    let initialRender = Render.StringToString(content, d)

                    //Recurse up the parent templates providing the "content".
                    let rec inner content' templateName' vars' =
                        match templatesMap.TryFind templateName' with
                        | Some(template) -> 
                            //Join template args with current args
                            let varAcc = 
                                seq {yield! vars'; yield! template.Vars } 
                                //HACK!!! take out "template" vars as we will get dupes.
                                |> Seq.filter (fun (name,_) -> name <> "template")
                                |> List.ofSeq
                            let dic = toDictionary varAcc
                            dic.Add("content", content')
                            let output = Render.StringToString(template.Template, dic)
                            match template.ParentFileName with
                            | Some(parent) ->  inner output parent varAcc 
                            | None -> output
                        | None -> printfn "Missing template %s" templateName'; content' //failwith <| sprintf "No template named %s." templateName'
                    inner initialRender templateName vars 
                    TextOutput({ x with ReaderF = content })

                else output 
                *)
                    
            | None -> output 

        | BinaryOutput(_) -> output 


