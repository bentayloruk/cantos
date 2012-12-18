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
        descendantFilePaths site.InPath
        |> Seq.map (getContent >> outUri site)
        |> Seq.choose (|PublishedContent|_|) 

    ///Generates blog post output.
    let generateBlog (site:Site) = 
        
        let blogDir = site.InPath.CombineWithParts(["_posts"])

        if Directory.Exists(blogDir.LocalPathUnescaped) then
            childFilePathsEx blogDir
            |> Seq.choose (getContent >> hasFrontMatter)
            |> Seq.choose (|PublishedContent|_|)
            //Review - should I choose AND map here?
            |> Seq.choose textContent
            |> Seq.choose (fun content ->
                match (getUri content) with
                | DateSlugFormat (date, slug) ->
                    //TODO support he Jekyll style path outputs.
                    let path = site.OutPath.CombineWithParts(["blog"; slug + ".html"])
                    Some(withUriOut content path)
                | uri ->
                    logError(sprintf "Bad post file name %s." <| uri.ToString()); None
            )

        else
            logInfo <| sprintf "Blog directory does not exist.  Skipping.  Looked in %s" blogDir.AbsolutePath
            Seq.empty

///Used to create one or more books in the site.
[<AutoOpen>]
module BookGenerator =
    open System

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
    type Toc = { Id:string; Chapters:list<Chapter>; }
    and Chapter = { Title:string; Headings:list<Heading> }
    and Heading = { Href:string; Title:string; EnableLink: bool; }

    ///Creates an AHead for from a file.
    let maybeHeadingFromRootedPath sitePath = 
        Some( { Heading.Href = "TBD"; Title = "TBD"; EnableLink = true; } )
        
    let tryGetHeading (rootUri:Uri) output =
        match output with 
        | TextContent(x) ->
            let shouldLink = (|ValueOrDefault|) "toc-link" true x.Meta
            match x.Meta with
            | StringValue "toc-title" t
            | StringValue "title" t ->
                let href = "/" + rootUri.MakeRelativeUri(x.UriOut.Value).ToString()
                Some({ Href = href; Title = t; EnableLink = shouldLink })
            | _ -> None
            
        | BinaryContent(_) -> None

    ///Creates a TOC for files in the given site path.
    let toBookOutputs (path:string) (site:Site) = 

        let bookOutPath =
            DirectoryInfo(path).Name.Split('.')
            |> List.ofArray
            |> site.OutPath.CombineWithParts 

        let chapterDirs =
            Dir.getDirs path
            |> List.ofArray

        let chapters, chapterContents = 
            chapterDirs
            |> Seq.fold (fun (chapters, contentItems) dir ->
                let chapterDir = deNumberWang (DirectoryInfo(dir).Name)
                let filesInChapterDir = getFileInfos dir

                let outputs, headings =
                    filesInChapterDir
                    |> Seq.fold (fun (contents, headings) fi ->
                        
                        //This needs to be a LOT neater.
                        let content =
                            let output = getContent fi.FullName
                            let outPath = bookOutPath.CombineWithParts([chapterDir; deNumberWang fi.Name])
                            let outPath = 
                                match output with 
                                | TextContent(x) -> outPath.WithExtension(htmlExtension)
                                | BinaryContent(x) -> outPath
                            withUriOut output outPath

                        match content with
                        | PublishedContent(x) ->
                            let heading = tryGetHeading site.OutPath x
                            let headings = if heading.IsSome then heading.Value::headings else headings
                            //REVIEW outputs includes non-page and binary files.  Does this break anything.
                            (x::contents, headings)

                        | _ -> (contents, headings)
                            
                        ) ([], [])

                let headings = headings |> List.rev
                let outputs = outputs |> List.rev

                if headings.Length >= 2 then
                    let chapter = { Title = (headings.Head).Title; Headings = headings |> List.tail }
                    (chapter::chapters, outputs::contentItems)
                else
                    //Don't add the chapter, but write the content items.
                    (chapters, outputs::contentItems)

            ) ([] , [])

        //Use the folder name as the toc ID as will be unique.
        let tocMeta =
            { Id = DirectoryInfo(path).Name.Replace(".","-") + "-toc";
            Toc.Chapters = chapters |> List.rev }
            :> obj
            |> MetaValue.Object

        let contentItems =
            chapterContents 
            |> List.rev 
            |> List.concat

        contentItems |> Seq.map (withMeta "toc" tocMeta)

    ///Generates blog post output.
    let generateBooks (site:Site) = 
        let path = site.InPath.CombineWithParts(["_books"])

        if Dir.exists path.LocalPathUnescaped then

            //Register types we wish to template (required by DotLiquid).
            [typeof<Toc>; typeof<Chapter>; typeof<Heading>;]
            |> Seq.iter site.RegisterTemplateType

            let bookDirs = Dir.getDirs path.LocalPathUnescaped
            seq { for path in bookDirs do yield! toBookOutputs path site } 
        else
            logInfo(sprintf "Books directory not found.  Skipping.  Looked in: %s" path.AbsolutePath)
            Seq.empty


