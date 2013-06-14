namespace Cantos

open System
open System.IO
open UnionArgParser

module Program =

    //Command line argument definitions.
    type CantosArg =
        | [<Mandatory>] InputPath of string
        | OutputPath of string
        | WebServerPort of int
    with
        interface IArgParserTemplate with
            member __.Usage = 
                match __ with
                | InputPath _ -> "specify a site input path"
                | OutputPath _ -> "specify a site output path"
                | WebServerPort _ -> "specify the web server preview port"

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
        let inputPath =
            args.GetResult <@ InputPath @>
            |> fixPath
        let outputPath =
            args.GetResult (<@ OutputPath @>, Path.combine [| inputPath; "_site" |])
            |> fixPath
        let port = args.GetResult (<@ WebServerPort @>, 8888)
        {
            SourcePath = inputPath
            DestinationPath = outputPath
            PreviewServerPort = port
        }

    //Bit of pre-amble...
    let logStart site = 
        let msg =
            sprintf """[ Cantos - F#st and furious static website generator ]
Input Path: %s
Output Path: %s
DateTime: %s""" (site.InPath.ToString()) (site.OutPath.ToString()) (DateTime.Now.ToString()) 
        logInfo msg 

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
            Meta = [ "site", siteMeta ] |> Map.ofList
            RegisterTemplateType = initSafeType
            }
        
        logStart site 

        //Clean output directory.
        Dir.cleanDir (fun di -> di.Name = ".git") options.DestinationPath 

        //Run generators.
        let outputs =
            [ generateBlog; generateBooks; generateBasicSite; ]
            |> Seq.map (fun g -> g site)
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

    [<EntryPoint>]
    //Entry point for Cantos.
    let main argv = 
        try
            //Gets valid options or throws.
            let siteOptions = 
                let argParser = UnionArgParser<CantosArg>()
                argParser.Parse argv |> argsToOptions
            //Let's get our build on.
            buildSite siteOptions
            Dir.execOnFileChange siteOptions.SourcePath [siteOptions.DestinationPath] (fun args -> buildSite siteOptions)
            FireflyServer.runPreviewServer siteOptions.DestinationPath siteOptions.PreviewServerPort
            logInfo ("Hosting site at http://localhost:" + siteOptions.PreviewServerPort.ToString())
            let _ = Console.ReadLine()
            0
        with
        | ex ->
            logError ex.Message 
            1


