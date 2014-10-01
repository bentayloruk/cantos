namespace Cantos

open System
open System.IO
open UnionArgParser
open Snippets

module Program =

    //Command line argument definitions.
    type CantosArg =
        | [<Mandatory>] InputPath of string
        | OutputPath of string
        | WebServerPort of int
        | SamplesPath of string//TODO can we make this a list?
    with
        interface IArgParserTemplate with
            member __.Usage = 
                match __ with
                | InputPath _ -> "specify a site input path"
                | OutputPath _ -> "specify a site output path"
                | WebServerPort _ -> "specify the web server preview port"
                | SamplesPath _ -> "specify a path to files to scan for samples" 

    //Site options.
    type SiteOptions = {
        SourcePath:string
        DestinationPath:string
        PreviewServerPort:int
        } 

    //Convert args to our SiteOptions.
    //Review: maybe straight to Site?
    let argsToOptions (args:ArgParseResults<CantosArg>) =

        let fixPath = Path.getFullPath >> ensureEndsWithDirSeparatorChar
        let inputPath = args.GetResult <@ InputPath @> |> fixPath
        let outputPath = args.GetResult (<@ OutputPath @>, Path.combine [| inputPath; "_site" |]) |> fixPath
        let port = args.GetResult (<@ WebServerPort @>, 8888)
       
        {
            SourcePath = inputPath
            DestinationPath = outputPath
            PreviewServerPort = port
        }

    //Bit of pre-amble...
    let logBuild site = 
        logInfo "[ Cantos - F#st and furious static website generator ]"
        logInfo <| sprintf "Input Path: %s" (site.InPath.ToString())
        logInfo <| sprintf "Output Path: %s" (site.OutPath.ToString())
        logInfo <| sprintf "DateTime: %s" (DateTime.Now.ToString()) 

    //The main site build function.
    let buildSite options = 
        (*
        For now we are running Cantos with some defaults.
        Plan this being done from an fsx script (FAKE style), command line or YAML config.  One to discuss.
        *)
        //TODO config/options is bit of mess.  Clean this up when trying fsx approach.
        let siteMeta = MetaValue.Mapping(["time", MetaValue.DateTime(DateTime.Now)] |> Map.ofList)

        let site = {
            InPath = Uri(options.SourcePath)
            OutPath = Uri(options.DestinationPath)
            Meta = [ "site", siteMeta; ] |> Map.ofList
            RegisterTemplateType = initSafeType
            }
        
        logBuild site 

        Dir.cleanDir (fun di -> di.Name = ".git") options.DestinationPath 

        //Run generators.
        let outputs =
            let generate g = g site
            [ generateBlog; generateBooks; generateBasicSite; ]
            |> Seq.map generate 
            |> Seq.concat

        //Set up the DotLiquid transform (our default and main templating engine).
        let liquidTransform = 
            //REVIEW tidy this up - Set up our "global" liquid environment (the includes and functions available).
            let includesPath = site.InPath.CombineWithParts(["_includes"])
            let rpf = renderParameters (IncludeFileSystem.Create(includesPath)) [typeof<JekyllFunctions>]
            liquidContentTransformer rpf 

        //Let people enhance the site meta.  Review: We do this due to streaming.  Too much?
        //Blog needs to content transform, so provide one.
        let contentTransform = markdownTransformer >> liquidTransform site.Meta
        let site = 
            let meta = 
                Seq.fold (fun siteMeta metaMaker ->
                    let meta = metaMaker site siteMeta
                    meta
                    ) site.Meta [ (blogMeta contentTransform); ]
            { site with Meta = meta }

        //Transform and write content (new contentTransform with new Meta).
        let contentTransform = markdownTransformer >> liquidTransform site.Meta
        let siteTransform = layoutTransformer liquidTransform site

        outputs
        |> Seq.choose (|OKContent|_|)
        |> Seq.map (contentTransform >> siteTransform)
        |> Seq.iter writeContent

        logSuccess (sprintf "Success.  Output written to:\n%s.\n" options.DestinationPath)

    //Watch the input and build if changes.
    let buildOnChange site = 
        let exec = fun args -> buildSite site
        let watchPath = site.SourcePath
        let filterPaths = [site.DestinationPath]
        Dir.execOnFileChange watchPath filterPaths exec

    //Spin up a preview web server.
    let previewSite site = 
        FireflyServer.runPreviewServer site.DestinationPath site.PreviewServerPort
        logInfo ("Hosting site at http://localhost:" + site.PreviewServerPort.ToString())

    //Searches for and writes SAMPLE snippets into the _includes dir.
    let writeSampleIncludes (argResults:ArgParseResults<CantosArg>) =
        let samplesPath = argResults.TryGetResult(<@ SamplesPath @>)
        let siteInputPath = argResults.GetResult(<@ InputPath @>)
        match (samplesPath, siteInputPath) with
        | (None,_) -> logInfo "No samples specified."; ()
        | (Some(samplesPath), inputPath) ->
            let writeSnippet snippet = 
                let path = Path.combine [| inputPath; "_includes"; snippet.Id |]
                let lines =
                    seq { for line in snippet.Lines do
                            if line.Text.Length <= snippet.LeadingSpaces then yield ""
                            else yield line.Text.Substring(snippet.LeadingSpaces)
                    }
                File.WriteAllLines(path, lines)
            Snippets.searchForSamples ["*.cs"; "*.fs"; "*.js"; ] samplesPath //TODO add file filters to command line.
            |> Seq.iter writeSnippet

    [<EntryPoint>]
    let main argv = 
        try
            let argResults = UnionArgParser<CantosArg>().Parse argv
            writeSampleIncludes argResults
            let siteOptions = argsToOptions argResults
            buildSite siteOptions
            buildOnChange siteOptions
            previewSite siteOptions
            let _ = Console.ReadLine()
            0
        with
        | ex ->
            logError ex.Message 
            logError ex.StackTrace
            1


