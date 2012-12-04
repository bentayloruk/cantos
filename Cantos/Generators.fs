namespace Cantos

[<AutoOpen>]
///Module containing the "standard" generator functions.
module Generators = 

    open System
    open System.IO
    open FrontMatter
    open System.Text.RegularExpressions

    ///Copies all files from the site in path and transforms text files where it can.
    let generateBasicSite (site:Site) = 
        let transform = (markdownTransformer site >> liquidContentTransformer Map.empty site)
        descendantFilePaths site.InPath
        |> Seq.map (getContent >> transform >> (outUriContent site))

    ///Generates blog post output.
    let generateBlog (site:Site)  = 
        
        let blogDir = site.InPath.CombineWithParts(["_posts"])

        childFilePathsEx blogDir
        |> Seq.choose (getContent >> hasFrontMatter)
        //Review - should I choose AND map here?
        |> Seq.choose textContent
        |> Seq.choose (fun textContent ->

            let content = textContent |> (markdownTransformer site >> liquidContentTransformer Map.empty site)

            match (getUri content) with
            | DateSlugFormat (date, slug) -> Some(site.OutPath.CombineWithParts([slug + ".html"]), content)
            | uri -> site.Tracer.Error(sprintf "Bad post file name %s." <| uri.ToString()); None
        )

///Used to create one or more books in the site.
[<AutoOpen>]
module BookGenerator =

    ///Removes leading numbers and -.  Example:  1010-myname -> myname.  
    let deNumberWang (name:string) =
        let wangIndex = name.IndexOf('-')
        if wangIndex = -1 then name else
            let maybeNumber = name.Substring(0, wangIndex)
            let (parsed, number) = System.Int32.TryParse(maybeNumber)
            if parsed = true then name.Substring(wangIndex+1) else name 

    open System.IO
    open FrontMatter

    ///Represents a Table of Contents.
    type Toc = { Chapters:list<Chapter>; }
    and Chapter = { Headings:list<Heading> }
    and Heading = { Href:string; Title:string; EnableLink: bool; }

    ///Creates an AHead for from a file.
    let maybeHeadingFromRootedPath sitePath = 
        Some( { Heading.Href = "TBD"; Title = "TBD"; EnableLink = true; } )

    ///Get dir structure from dotted directory name.
    let dirPathConvention path = DirectoryInfo(path).Name.Split('.') |> Path.combine
        
    let tryGetHeading output =
        match output with 
        | TextContent(x) ->
            let shouldLink = (|BoolValueD|) "toc-link" true x.Meta
            match x.Meta with
            | StringValue "toc-title" t
            | StringValue "title" t ->
                Some({ Href = ""(*TODO*); Title = t; EnableLink = shouldLink })
            | _ -> None
            
        | BinaryContent(_) -> None

    (*
    ///Creates a TOC for files in the given site path.
    let toBookOutputs (path:string) (site:Site) = 
        
        let bookRootPath = site.OutPath.CreateRelative(dirPathConvention path)
        let chapterDirs = Dir.getDirs path |> List.ofArray

        let makeBook dirs = 
            let chapterOutputs = 
                let rec inner dirs acc = 
                    match dirs with

                    | dir::t ->
                            let chapterDir = deNumberWang (DirectoryInfo(dir).Name)
                            let filesInChapterDir = getFileInfos dir

                            let outputs, headings =
                                filesInChapterDir
                                |> Seq.fold (fun (outputs, headings) fi ->
                                    let outPath = Path.Combine(chapterDir, deNumberWang fi.Name) 
                                    let outPath = bookRootPath.CreateRelative(outPath)
                                    let output = getOutput fi.FullName outPath
                                    //TODO heading URL is wrong (as still has MD extension).  Should we hack or add after?
                                    let heading = tryGetHeading output
                                    let headings = if heading.IsSome then heading.Value::headings else headings
                                    //TODO don't add chapters with no headings?
                                    //TODO outputs includes non-page and binary files.  Does this break anything.
                                    (output::outputs, headings)
                                    ) ([], [])

                            let chapter = { Headings = headings |> List.rev }
                            let chapterOuputs = outputs |> List.rev
                            //chapterOuputs |> Seq.mapTextOutput (fun x -> { x with Meta = x.Meta.Add("chapter", MetaValue.Object(chapter)) }) 
                            inner t ((chapter, chapterOuputs)::acc)

                    | [] -> acc
                    
                inner dirs []

            let chapterOutputs = chapterOutputs |> List.rev
            let toc = { Chapters = chapterOutputs |> List.map (fun (chapter, _) -> chapter) |> List.ofSeq }
            let allBookOutputs = seq { for chap, outs in chapterOutputs do yield! outs }
            Seq.empty
            //allBookOutputs |> Seq.mapTextOutput (fun o -> { o with Meta = o.Meta.Add("toc", MetaValue.Object(toc)) })

            //TODO add chapter to it's outputs.
            //TODO add Toc to all outputs.

        makeBook chapterDirs

    ///Generates blog post output.
    let generateBooks (site:Site) = 
        let path = site.InPath.CreateFeaturePath("_books")
        if Dir.exists path.AbsolutePath then
            let outputs = seq {
                for path in Dir.getDirs path.AbsolutePath do
                    yield! toBookOutputs path site } 
            //site.Meta, outputs 
            ()
        else
            site.Tracer.Error(sprintf "Books generator is configured but path does not exist.  Looking in: %s" path.AbsolutePath)
            ()

            *)
