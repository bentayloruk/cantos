namespace Cantos

[<AutoOpen>]
///
///Liquid template engine transformer bits.
///
module LiquidTransformer = 
    
    open DotLiquid
    open System.IO
    open System.Collections.Generic

    ///Reads template from reader and transforms with DotLiquid and the provided hash data.
    let liquidTransform (hash:Hash) (reader:TextReader) =
        let template = Template.Parse(reader.ReadToEnd())
        new StringReader(template.Render(hash)) :> TextReader

    ///Transforms the content out content using the Liquid templating engine.
    ///Does not recurse layouts (this allows content post processing with other transformers).
    let liquidContentTransformer namedMetas (content:Content) =

        match content with

        | Meta meta & Text x ->

            //Combine page meta with any provided "global" metas (e.g. site).
            let metas = ("page", Mapping(meta))::namedMetas |> Map.ofSeq
            let hash = Hash.FromDictionary(toDictionary metas)
            textTransform (liquidTransform hash) x

        | _ -> content

///
///Markdown conversion.
///
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
    let markdownTransformer (content:Content) = 
        matchTextTransform (|Markdown|_|) toMarkdown content

///
///Handles content layouts.
///
[<AutoOpen>]
module LayoutTransformer = 

    open System.IO
    open FrontMatter
    open System.Collections.Generic
    open System

    let buildLayoutMap layoutPath = 
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

    ///Transforms contents that have "layout" meta.
    let layoutTransformer (site:Site) =

        let layoutMap = buildLayoutMap (site.InPath.CombineWithParts(["_layouts"]))
        
        //Return rest of computation, so we don't keep parsing template.
        fun (content:Content) ->
            //TODO this code is not super readable.  Fix it up.
            //Look for "layout" in meta.  Transform, then recurse looking for "layout" in the layout!
            let rec recurseLayouts = function

                | TextContent(x) ->

                    match x.Meta with

                    | LayoutName(name) ->

                        match layoutMap.tryGetValue(name) with

                        | Some(templateInfo) ->

                            //Add content and site meta...
                            use tr = x.ReaderF()
                            let globalMetas = ["site", Mapping(site.Meta); "content", MetaValue.String(tr.ReadToEnd())]

                            //Create "new" content containing the layout template and new meta. 
                            let layoutContent = 
                                { x with
                                    Meta = templateInfo.Meta.join(x.Meta.Remove("layout"));
                                    ReaderF = fun () -> new StringReader(templateInfo.Template) :> TextReader }

                            //Apply the liquid transform to the layout...
                            let layoutContent  = liquidContentTransformer globalMetas (Content.TextContent(layoutContent))

                            recurseLayouts layoutContent 

                        | None -> 

                            logError (sprintf "Layout named %s missing.  Referenced by content:\n%s" name (x.Uri.ToString()))
                            Content.TextContent(x)

                    | _ -> Content.TextContent(x)

                | x -> x 

            recurseLayouts content
