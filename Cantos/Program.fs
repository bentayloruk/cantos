namespace Cantos

open System
open System.IO

module Program =

    //Preview server function.
    let runPreviewServer path (port: Port) = FireflyServer.runPreviewServer path port

    let sitePathFromPath path = 
        //Ensure directory arg is separator terminated (as SitePath demands it!).
        //TODO review this path stuff.  Hacked in for sample site relative path.
        let path = path
        let path = if Path.endsWithDirSeparatorChar path then path else path + Path.DirectorySeparatorChar.ToString()
        let path = Path.GetFullPath(path)
        SitePath.Create(path, "")

    let write (msg:string) = Console.WriteLine(msg)

    //Entry point.
    [<EntryPoint>]
    let main argv = 

        (*
        For now we are running the Cantos like Jekyll.
        However, plan this being done from an fsx script (FAKE style), command line or YAML config.  One to discuss.
        *)

        //For now, if we have one arg, it is the path to the site source.
        if argv.Length = 1 then

            //Get in and out path.
            let siteInPath = ensureEndsWithDirSeparatorChar (Path.GetFullPath(argv.[0]))
            let path parts = Path.Combine( siteInPath :: parts |> Seq.map ensureEndsWithDirSeparatorChar |> Array.ofSeq)//Todo fix this so can't get wrong.
            let siteOutPath = path [ "_site" ]

            //Partially apply functions with defaults.
            let tracer = ConsoleTracer() :> ITracer
            let tempFileExclusions:FileExclusion = fun fi -> fi.Name.EndsWith("~") || fi.Name.EndsWith(".swp")
            let appDirExclusions:DirectoryExclusion = fun di -> di.Name.StartsWith("_")
            let fileStreamInfos = fileStreamInfosFiltered tracer appDirExclusions tempFileExclusions
                
            //Generate streams.
            let outputStreams = [

                //Basic site files.
                yield! fileStreamInfos siteInPath siteOutPath

                //Blog posts.
                //TODO map according to post properties.
                yield! fileStreamInfos (path [ "_posts" ]) siteOutPath
            ]

            //Process streams.
            let (processors:list<Processor>) = [ markdownProcessor ]
            let applyProcessors streamInfo = 
                processors |> List.fold (fun acc proc -> proc acc) streamInfo

            let processedStreams = outputStreams |> Seq.map applyProcessors 

            //Clean output directory.
            Dir.cleanDir (fun di -> di.Name = ".git") siteOutPath 

            //Write
            let writer output =
                match output with
                | TextOutput(t) ->
                    use tr = t.ReaderF()
                    File.WriteAllText(t.Path.AbsolutePath, tr.ReadToEnd())//Change to stream write.
                | BinaryOutput(b) ->
                    use fs = File.Create(b.Path.AbsolutePath)
                    use s = b.StreamF()
                    s.CopyTo(fs)

            processedStreams |> Seq.iter writer 

            //Preview it!
            write (sprintf "Cantos success.  Wrote site to %s." siteOutPath)
            runPreviewServer siteOutPath 8888
            let _ = Console.ReadLine()

            0
        else
            write "Cantos fail.  You must provide the site source path as the single argument."
            1
