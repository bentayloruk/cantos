namespace Cantos

[<AutoOpen>]
module LiquidTransformer = 
    
    open DotLiquid
    open System.IO
        
    let liquidTransform meta (reader:TextReader) =

        //TODO join site meta to meta.
        //TODO naive recusion.  Fix up.  Recursively convert the meta values to a hash that dotliquid likes.
        let rec toHash value : obj = 
            match value with
            | Mapping(map) ->
                let hash = Hash()
                map
                |> Seq.iter (fun kvp -> hash.Add(kvp.Key, toHash kvp.Value))
                hash :> obj
            | Object(o) -> o
            | String(s) -> s :> obj
            | Int(i) -> i :> obj
            | List(l) -> 
                l
                |> Seq.map (fun item -> toHash item)
                |> List.ofSeq
                :> obj

        let hash = (toHash (MetaValue.Mapping(meta))) :?> Hash
        let template = Template.Parse(reader.ReadToEnd())
        new StringReader(template.Render(hash)) :> TextReader

    ///Transforms the content out content using the Liquid templating engine.
    ///Does not render templates (this allows content post processing with other transformers).
    let liquidContentTransformer layouts (site:Site) (content:Content) =
        match content with
        | Meta meta & Text tc -> textTransform (liquidTransform meta) tc
        | _ -> content

///Markdown conversion.
[<AutoOpen>]
module MarkdownTransformer =

    open System.IO
    open MarkdownDeep
    open System

    ///Reads text content and converts it to Markdown.
    let toMarkdown (reader:TextReader) =
        let md = Markdown()
        md.ExtraMode <- false
        md.SafeMode <- false
        let html = md.Transform(reader.ReadToEnd())
        new StringReader(html)

    ///Turns .md or .markdown files into html.
    let markdownTransformer (site:Site) (content:Content) = 
        matchTextTransform (|Markdown|_|) toMarkdown content
        

[<AutoOpen>]
module LayoutTransformer = 

    open System.IO
    open FrontMatter
    open System.Collections.Generic
    open System

    let buildTemplateMap layoutPath = 
        ///Create a map of layouts below sourcePath.
        //TODO make it easier to get files with front matter.  This is too much mess.
        childFilePathsEx layoutPath
        |> Seq.map getContent
        |> Seq.choose (function
            | TextContent(x) ->
                let name = x.Uri.FileNameWithoutExtension
                use r = x.ReaderF()
                Some(name, { FileName = name ; Meta = x.Meta; Template = r.ReadToEnd()} )
            | BinaryContent(_) -> None )
        |> Map.ofSeq

        (*
    ///Transforms contents that have "layout" meta.
    let layoutTransformer layoutDir (site:Site) =

        let layoutMap = buildTemplateMap (site.InPath.CombineWithParts(layoutDir))
        
        //Return rest of computation, so we don't keep parsing template.
        fun content ->
            //TODO this code is not super readable.  Fix it up.
            //Look for "layout" in meta.  Transform, then recurse looking for "layout" in the layout!
            let rec recurseLayouts (meta:MetaMap) (content:TextContent) =

                match meta with

                | LayoutName(name) ->

                    match layoutMap.tryGetValue(name) with

                    | Some(templateInfo) ->

                        let mergedMeta = templateInfo.Meta.join(meta.Remove("layout"))
                        let o = 
                            textTransform
                            content.DecorateReader(fun tr ->
                                let meta = mergedMeta.Add("content", MetaValue.String(tr.ReadToEnd()))
                                use templateReader = new StringReader(templateInfo.Template)
                                liquidTransform meta templateReader)

                        recurseLayouts mergedMeta o 

                    | None -> content//Log!!

                | None | Some(_) -> content 


            match content with
            | TextContent(toi) -> TextContent(recurseLayouts toi.Meta toi) 
            | _ -> content
            *)
