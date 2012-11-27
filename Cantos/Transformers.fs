namespace Cantos


///Markdown conversion.
[<AutoOpen>]
module MarkdownTransformer =

    open System.IO
    open MarkdownDeep
    open System

    ///Turns a markdown stream into and html stream.  Returns a new Uri with .html extension.
    let markdownTransformer (output:Output) = 

        match output with

        | TextOutput(x) ->

            let mdExtensions = [ "md"; "markdown" ] |> Seq.map FileExtension.Create

            if x.Path.HasExtension(mdExtensions) then
                let transfromReader = 
                    let md = Markdown()
                    md.ExtraMode <- false
                    md.SafeMode <- false
                    use tr = x.ReaderF()
                    let html = md.Transform(tr.ReadToEnd())
                    fun () -> new StringReader(html) :> TextReader

                let path = x.Path.ChangeExtension(FileExtension.Create("html"))
                TextOutput({ x with ReaderF = transfromReader; Path = path })

            else TextOutput(x) 

        | other -> other 
        
[<AutoOpen>]
module LayoutTransformer = 

    open System.IO
    open FrontMatter
    open DotLiquid
    open System.Collections.Generic
    open System

    let liquidTransform template output =
        //Should not be parsing everytime.  Rejig.
        let template = Template.Parse(template)
        template.Render()

    ///Creates a map of layouts below sourcePath.
    let getLayouts sourcePath = 
        getDescendantFileInfos appDirExclusions tempFileExclusions sourcePath
        |> Seq.map (fun fileInfo ->
            let path = fileInfo.FullName
            use fileReader = fileReader path 
            Path.GetFileName(path).ToLower(), fileReader.ReadToEnd())
        |> Map.ofSeq
         
    ///Creates an output processor that transforms output with templates.
    let layoutTransformer layoutPath (output:Output) =
        
        let templateMap = getLayouts layoutPath 

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


///Used to create one or more books in the site.
module Books =

    ///Removes leading numbers and -.  Example:  1010-myname -> myname.  
    let deNumberWang (name:string) =
        let wangIndex = name.IndexOf('-')
        if wangIndex = -1 then name else
            let maybeNumber = name.Substring(0, wangIndex)
            let (parsed, number) = System.Int32.TryParse(maybeNumber)
            if parsed = true then name.Substring(wangIndex+1) else name 

    ///Removes leading numbers from file and dir paths (e.g. /2222-dirname/1234-file.html -> /dirname/file.html).
    let deNumberWangPath (path:string) =
        //Example:
        //This -> "developer\0100-introduction\0075-enticify-connector-for-commerce-server.md"
        //Becomes this -> "developer\introduction\enticify-connector-for-commerce-server.md"
        path.Split(Path.dirSeparatorChars)
        |> Seq.map deNumberWang 
        |> Array.ofSeq
        |> Path.combine


    [<RequireQualifiedAccessAttribute>]
    module Toc =

        open System.IO
        open FrontMatter

        ///Represents a Table of Contents.
        type Toc = { Name:string; Chapters:list<Chapter>; Path:RootedPath; }
        and Chapter = { Headings:list<Heading> }
        and Heading = { Href:string; Title:string; EnableLink: bool; }

        ///Creates an AHead for from a file.
        let maybeHeadingFromRootedPath sitePath = 
            Some( { Heading.Href = "TBD"; Title = "TBD"; EnableLink = true; } )

        ///Creates a TOC for files in the given site path.
        let forPath (path:RootedPath) name = 

            let tocSectionDirs = Directory.GetDirectories(path.AbsolutePath)

            let chaptersWithAtLeastOneHeading = 
                [
                    for tocSectionDir in tocSectionDirs do

                        let headings = 
                            Dir.getFiles tocSectionDir
                            |> Seq.map (fun filePath -> path.RelativeRootedPath(filePath)) 
                            |> Seq.choose maybeHeadingFromRootedPath 
                            |> List.ofSeq

                        if headings.Length > 0 then
                            yield { Chapter.Headings = headings }
                ]

            { 
                Path = path;
                Toc.Name = name;
                Chapters = chaptersWithAtLeastOneHeading;
            }
