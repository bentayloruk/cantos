namespace Cantos

[<AutoOpen>]
///Module containing the "standard" generator functions.
module Generators = 
    open System

    type Post = 
        { Title:string
          Url:string
          Id:string
          Content:string
          Date:DateTime}

    open System
    open System.IO
    open FrontMatter
    open System.Text.RegularExpressions

    ///Copies all files from the site in path and transforms text files where it can.
    let generateBasicSite (site:Site) = 
        descendantFilePaths site.InPath
        |> Seq.map (getContent >> outUri site)
        |> Seq.choose (|PublishedContent|_|) 

    let makePostContents (site:Site) = 
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
                    //TODO support the Jekyll style path outputs.
                    let path = site.OutPath.CombineWithParts(["blog"; slug + ".html"])
                    Some(withUriOut content path)
                | uri ->
                    logError(sprintf "Bad post file name %s." <| uri.ToString()); None
            )

        else
            logInfo <| sprintf "Blog directory does not exist.  Skipping.  Looked in %s" blogDir.AbsolutePath
            Seq.empty

    ///Generates blog post output.
    let generateBlog (site:Site) = makePostContents site
        
    let (|BlogPostData|_|) (outUriRoot:Uri) (content:Content)  =
        match content with 
        | TextContent(x) ->
            match x.Meta with 
            | MetaString "title" title when x.UriOut.IsSome ->
                let dt = 
                    match x.Meta with
                    | MetaString("date") dt when tryParseGood (DateTime.TryParse(dt)) -> DateTime.Parse(dt)
                    | _ -> DateTime.Now 
                use r = x.ReaderF()
                let url = outUriRoot.MakeRelativeUri(x.UriOut.Value).ToString()
                let post = { Title=title; Url=url; Id=url; Content=r.ReadToEnd(); Date=dt }
                Some(post)
            | _ -> logDebug (sprintf "No title in potential blog post %s." <| x.Uri.ToString()); None
        | _ -> None

    ///Generates blog post output.
    let blogMeta (transform:Content->Content) (site:Site) (meta:MetaMap) =

        site.RegisterTemplateType(typeof<Post>)//Ugly, maybe use attributes instead.

        let posts = 
            makePostContents site
            |> Seq.map transform
            |> Seq.choose (|PublishedContent|_|)
            |> Seq.choose ((|BlogPostData|_|) site.OutPath)
            |> List.ofSeq
            |> List.sortBy (fun post -> post.Date)

        let boxedPosts = posts |> List.map (box >> MetaValue.Object)

        let siteMeta = 
            match meta.Item("site") with
            | Mapping(x) ->
                let x = x.Add("posts", MetaValue.List(boxedPosts))
                let dt = 
                    match posts with
                    | [] -> DateTime.MinValue
                    | h::t -> h.Date
                let x = x.Add("latestpostdatetime", MetaValue.DateTime(dt))
                Mapping(x)
            | _ -> failwith "site must exit."

        meta.Add("site", siteMeta)

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

    let tryGetHeading (rootUri:Uri) output =
        match output with 
        | TextContent(x) ->
            let shouldLink = (|ValueOrDefault|) "toc-link" true x.Meta
            match x.Meta with
            | MetaString "toc-title" t//Use this if found, otherwise title...
            | MetaString "title" t ->
                let href = "/" + rootUri.MakeRelativeUri(x.UriOut.Value).ToString()
                Some({ Href = href; Title = t; EnableLink = shouldLink })
            | _ -> None
            
        | BinaryContent(_) | ErrorContent(_) -> None

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
                                | ErrorContent(_) -> outPath
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

            seq {
                let bookDirs =
                    Dir.getDirs path.LocalPathUnescaped
                    |> Array.filter (fun path -> DirectoryInfo(path).Name.StartsWith("_") <> true)
                for path in bookDirs do yield! toBookOutputs path site
                } 
        else
            logInfo(sprintf "Books directory not found.  Skipping.  Looked in: %s" path.AbsolutePath)
            Seq.empty


