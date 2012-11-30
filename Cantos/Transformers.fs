namespace Cantos

[<AutoOpen>]
module TemplateTransformers =

    open DotLiquid
    open System.IO
        
    let liquidTransform (meta:MetaMap) (templateReader:TextReader) =

        //Review:  Recursively convert the meta values to a hash that dotliquid likes.
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
        let template = Template.Parse(templateReader.ReadToEnd())
        new StringReader(template.Render(hash))

///Markdown conversion.
[<AutoOpen>]
module MarkdownTransformer =

    open System.IO
    open MarkdownDeep
    open System

    ///Turns a markdown stream into and html stream.  Returns a new Uri with .html extension.
    let markdownTransformer (site:Site) (output:Output) = 
        
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
    let liquidContentTransformer (site:Site) (output:Output) =
        //TODO create the hash.
        let f = liquidTransform Map.empty
        output.DecorateTextOutputReader(f)

[<AutoOpen>]
module LayoutTransformer = 

    open System.IO
    open FrontMatter
    open System.Collections.Generic
    open System

    ///Transforms outputs that have "layout" meta.
    let layoutTransformer layoutDir (site:Site) (output:Output) =

        let layoutPath = site.InPath.CreateFeaturePath(layoutDir)
        ///Create a map of layouts below sourcePath.
        //TODO make it easier to get files with front matter.  This is too much mess.
        let templateMap =
            getFileInfos layoutPath.AbsolutePath
            |> Seq.map (fun fi -> getOutput (layoutPath.CreateRelative(fi.Name)) fi)
            |> Seq.choose (fun output ->
                match output with
                | TextOutput(x) ->
                    let name = Path.GetFileNameWithoutExtension(x.Path.AbsolutePath)
                    use r = x.ReaderF()
                    Some(name, { FileName = name ; Meta = x.Meta; Template = r.ReadToEnd()} )
                | BinaryOutput(_) -> None )
            |> Map.ofSeq

        //TODO this code is not super readable.  Fix it up.
        //Look for "layout" in meta.  Transform, then recurse looking for "layout" in the layout!
        let rec recurseLayouts (meta:MetaMap) (output:TextOutputInfo) =
            match meta.tryGetValue("layout") with

            | Some(String(layoutName)) ->
                match templateMap.tryGetValue(layoutName) with

                | Some(templateInfo) ->
                    let mergedMeta = templateInfo.Meta.join(meta.Remove("layout"))
                    let o = 
                        output.DecorateReader(fun tr ->
                            let meta = mergedMeta.Add("content", MetaValue.String(tr.ReadToEnd()))
                            use templateReader = new StringReader(templateInfo.Template)
                            liquidTransform meta templateReader)

                    recurseLayouts mergedMeta o 

                | None -> output//Log!!

            | None | Some(_) -> output 


        match output with
        | TextOutput(toi) -> TextOutput(recurseLayouts toi.Meta toi) 
        | _ -> output
