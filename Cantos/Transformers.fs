﻿namespace Cantos

open System.IO
open System.Collections.Generic
open System

[<AutoOpen>]
module NustacheTransformer = 
    open Nustache.Core

    let nustacheTransform dic (reader:TextReader) =
        let content = Render.StringToString(reader.ReadToEnd(), dic :> obj)
        new StringReader(content)

    let nustacheContentTransformer (globalMeta:MetaMap) (content:Content) =
        match content with
        | Meta m & Text x ->
            //Combine page meta with any provided "global" metas (e.g. site).
            let meta = globalMeta.Add("page", Mapping(m))
            let dic = metaToDic meta
            textTransform (nustacheTransform dic) x
        | _ -> content

[<AutoOpen>]
///
///Liquid template engine transformer bits.
///
module LiquidTransformer = 
    
    open DotLiquid
    open DotLiquid.FileSystems

    module List =
        let toLiquidHash list =
            let hash = Hash()
            list |> List.iter (fun (k,v) -> hash.Add(k, v))
            hash

    ///Hacked in start for providing Liquid includes from a Cantos folder.
    type IncludeFileSystem private (includesMap) =

        interface FileSystems.IFileSystem with
            member __.ReadTemplateFile(context, templateName) =
                let x = includesMap.tryGetValue (templateName.ToLower())
                if x.IsSome then x.Value
                else raiseArgEx (sprintf "Template %s not found.  We only support toc right now!  Fix me!" templateName) "templateName"

        static member Create(path:Uri) = 
            if Directory.Exists(path.LocalPathUnescaped) then
                let includesMap = 
                    childFilePathsEx path 
                    |> Seq.map (fun path -> Path.GetFileNameWithoutExtension(path).ToLower(), File.ReadAllText(path))
                    |> Map.ofSeq
                IncludeFileSystem(includesMap) :> IFileSystem
            else
                { new IFileSystem with member __.ReadTemplateFile(c,t) = raiseInvalidOp "No includes are defined." }
            
    ///Compatibility filters for Jekyll.  DotLiquid requires them to be static members on a class.
    type JekyllFunctions() =
        static member date_to_xmlschema (dt:DateTime) =
            //http://stackoverflow.com/questions/6314154/generate-datetime-format-for-xml
            dt.ToUniversalTime().ToString("o")
            //TODO// static member xml_escape (x:string) =

    let initSafeType (t:Type) = 
        let allowed =
            t.GetMembers(Reflection.BindingFlags.Public ||| Reflection.BindingFlags.Instance)
            |> Seq.map (fun m -> m.Name)
            |> Array.ofSeq
        Template.RegisterSafeType(t, allowed)
        
    let renderParameters (fileSystem:IFileSystem) functionSrcTypes hash = 
        let registers =
            ["file_system", fileSystem] 
            |> List.toLiquidHash
        RenderParameters(Filters = functionSrcTypes, LocalVariables = hash, Registers = registers)
        
    ///Transforms the content out content using the Liquid templating engine.
    ///Does not recurse layouts (this allows content post processing with other transformers).
    let liquidContentTransformer (renderParamsF:Hash->RenderParameters) (meta:MetaMap) (content:Content) =
        match content with

        | Meta m & Text x ->
            let renderParams =
                meta.Add("page", Mapping(m))//Add content meta to "global" meta as "page" member.
                |> metaToDic
                |> Hash.FromDictionary//Hash is DotLiquid structure
                |> renderParamsF

            //Review: DotLiquid takes a Stream.  Maybe we should use stream instead of TextReader.
            let liquidify (tr:TextReader) = 
                let template = Template.Parse(tr.ReadToEnd())
                Template.FileSystem <- IncludeFileSystem.Create(Uri(@"C:\Users\Ben Taylor\Projects\new.enticify.com\site\_includes"))
                new StringReader(template.Render(renderParams)) :> TextReader

            textTransform liquidify x

        | _ -> content

///
///Markdown conversion.
///
[<AutoOpen>]
module MarkdownTransformer =

    open System.IO
    open MarkdownDeep
    open System

    let markdownDeep (reader:TextReader) = 
        let md = Markdown()
        md.SafeMode <- false
        md.ExtraMode <- true
        //md.AutoHeadingIDs <- true 
        md.MarkdownInHtml <- true
        let text = reader.ReadToEnd()
        md.Transform(text)

    let pandocPath = Pandoc.findPandoc() 

    let pandoc (reader:TextReader) = 
        let exitCode, _, html = Pandoc.toHtml pandocPath reader
        if exitCode <> 0 then failwith "Failed to exec Pandoc"
        html

    ///Reads text content and converts it to Markdown.
    let toMarkdown (reader:TextReader) =
        //let html = markdownDeep reader
        let html = pandoc reader
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
    let layoutTransformer liquidTransform (site:Site) =

        let layouts = buildLayoutMap (site.InPath.CombineWithParts(["_layouts"]))
        
        //Return rest of computation, so we don't keep parsing template.
        fun (content:Content) ->
            //TODO this code is not super readable.  Fix it up.
            //Look for "layout" in meta.  Transform, then recurse looking for "layout" in the layout!
            let rec recurseLayouts = function

                | TextContent(x) ->

                    match x.Meta with

                    | LayoutName(name) ->

                        match layouts.tryGetValue(name) with

                        | Some(templateInfo) ->

                            //Add content and site meta...
                            use tr = x.ReaderF()
                            let meta = site.Meta.Add("content", MetaValue.String(tr.ReadToEnd()))
                            let meta = meta.join x.Meta

                            //Create "new" content containing the layout template and new meta. 
                            let layoutContent = 
                                { x with
                                    Meta = templateInfo.Meta.join(x.Meta.Remove("layout"));
                                    ReaderF = fun () -> new StringReader(templateInfo.Template) :> TextReader }

                            //Apply the liquid transform to the layout...
                            let layoutContent  = liquidTransform meta (Content.TextContent(layoutContent))

                            recurseLayouts layoutContent 

                        | None -> 

                            logError (sprintf "Layout named %s missing.  Referenced by content:\n%s" name (x.Uri.ToString()))
                            Content.TextContent(x)

                    | _ -> Content.TextContent(x)

                | x -> x 

            recurseLayouts content
