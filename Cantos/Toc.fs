[<RequireQualifiedAccessAttribute>]
module Cantos.Toc 

open System.IO
open FrontMatter

///Represents a Table of Contents.
type Toc = { Name:string; Chapters:list<Chapter>; SitePath:SitePath; }
and Chapter = { Headings:list<Heading> }
and Heading = { Href:string; Title:string; EnableLink: bool; }

///Creates an AHead for from a file.
let maybeHeadingFromSitePath sitePath = 
    Some( { Heading.Href = "TBD"; Title = "TBD"; EnableLink = true; } )

///Creates a TOC for files in the given site path.
let forPath (sitePath:SitePath) name = 

    let tocSectionDirs = Directory.GetDirectories(sitePath.AbsolutePath)

    let chaptersWithAtLeastOneHeading = 
        [
            for tocSectionDir in tocSectionDirs do

                let headings = 
                    Dir.getFiles tocSectionDir
                    |> Seq.map (fun filePath -> sitePath.RelativeSitePath(filePath)) 
                    |> Seq.choose maybeHeadingFromSitePath 
                    |> List.ofSeq

                if headings.Length > 0 then
                    yield { Chapter.Headings = headings }
        ]

    { 
        SitePath = sitePath;
        Toc.Name = name;
        Chapters = chaptersWithAtLeastOneHeading;
    }
        
//        //Do TOC processing for each scope.
//        let scopeTocs = 
//            scopes
//            |> Seq.map (fun scope ->
//                //Get the top level dirs in scope.
//                let dirs = 
//                    let path = Path.Combine(args.DocsInPath, scope.Path)
//                    Directory.GetDirectories(path)
//                //Get the relative root path.
//                let getHref path = 
//                    let (_, relativePath) = getOutPath args.DocsInPath args.DocsOutPath path
//                    let path = relativePath.Replace(Path.DirectorySeparatorChar, '/')
//                    "/" + path.Replace(Path.AltDirectorySeparatorChar, '/')
//                let toc = 
//                    [ for dir in dirs do 
//                        //Get files, except those that are binary mime types.
//                        let filePaths = 
//                            getFiles dir 
//                            |> Seq.filter (fun path ->
//                                match mimeTypeForPath path with | None -> false | Some(mt) -> not(isBinaryMimeType mt)
//                            )
//                            |> List.ofSeq 
//                        //Only yield a toc section if we have any files!
//                        if filePaths.Length > 0 then
//                            yield [
//                                for path in filePaths do
//                                    //Do IO and swallow fails.  We will be eventually consistent (maybe :)!
//                                    //TODO Only swallow the fails if in local server mode.
//                                    let readArgs p = 
//                                        use stream = File.Open(p, FileMode.Open, FileAccess.Read)
//                                        use reader = new StreamReader(stream)
//                                        argsFromFrontMatter reader
//                                    let (ioSuccess, fmArgs) = (protect "TOC IO Problem" [] readArgs) path
//                                    //If IO was success add to TOC.
//                                    if ioSuccess && argNotPresentOrSetTo "published" fmArgs "true" then
//                                        let tocTitle = 
//                                            seq { 
//                                                yield (getArgValueOpt "toc-title" fmArgs);
//                                                yield (getArgValueOpt "title" fmArgs);
//                                                yield Some(emptyTocTitleText);
//                                                }
//                                            |> Seq.pick (fun s -> s)
//                                        //Check for TOC flag
//                                        let incInToc = 
//                                            let tocArg = getArgValueOpt "toc" fmArgs
//                                            if tocArg = None then true else Convert.ToBoolean(tocArg.Value)
//                                        let hyperlink = 
//                                            let tocArg = getArgValueOpt "toc-link" fmArgs
//                                            if tocArg = None then true else Convert.ToBoolean(tocArg.Value)
//                                        if incInToc then
//                                            yield {Text=tocTitle; Link = getHref path; HyperLink = hyperlink}
//                        ]
//                    ]
//                let templateScopeName = 
//                    scope.Replace('\\','-') + "-toc"
//                    |> (fun s -> s.Replace('/','-')) //Just in case mono.  This code sucks!
//                (templateScopeName,toc)
//                )
//            |> List.ofSeq
//        
//        //Create the TOC html fragment.
//        [
//            let formatTocEntry tocEntry = 
//                let liContent = 
//                    if tocEntry.HyperLink then 
//                        String.Format("<a href='{0}'>{1}</a>", tocEntry.Link, tocEntry.Text)
//                    else tocEntry.Text
//                String.Format("<li>{0}</li>", liContent)
//
//            for (scope, tocSections) in scopeTocs do
//                //Build the scope TOC
//                //TODO make this a template (with default hardcoded)
//                let sb = StringBuilder()
//                sb.AppendFormat("<ul class='nav nav-list toc {0}'>", scope) |> ignore
//                for tocSection in tocSections do
//                    if tocSection.Length > 1 then
//                        let te = tocSection.Head//We assume head is the seciton header.
//                        sb.Append(formatTocEntry te) |> ignore
//                        sb.Append("<li><ul>") |> ignore
//                        for te in tocSection.Tail do
//                            sb.Append(formatTocEntry te) |> ignore
//                        sb.Append("</ul></li>") |> ignore
//                sb.Append("</ul>") |> ignore
//                yield (scope, sb.ToString())
//        ] 
//
//
