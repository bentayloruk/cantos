module Nancy 

open Nancy.Hosting.Self
open Nancy.Conventions
open Nancy
open System

let mutable maybeRootPath = None


type RootPathProvider() =

    let path = 
        match maybeRootPath with
        | None -> raiseInvalidOp "path is not set.  Which is an ugly hack anyway due to Nancy design or my lack of Nancy knowledge."
        | Some(path) -> path

    interface IRootPathProvider with
        member x.GetRootPath() = path
        member x.Equals(obj) = 
            match obj with
            | null -> false
            | :? IRootPathProvider as rpp -> rpp.GetRootPath() = path
            | _ -> false
        member x.GetHashCode() = x.GetHashCode()
        member x.GetType() = x.GetType()
        member x.ToString() = path

type StaticSiteBootstrapper() =
    inherit DefaultNancyBootstrapper() with
        override x.ConfigureConventions(conventions) =
            let convention = StaticContentConventionBuilder.AddDirectory("/","")
            System.Diagnostics.Debugger.Break()
            let _ = conventions.StaticContentsConventions.Add(convention)
            base.ConfigureConventions(conventions)
            ()
        override x.DiagnosticsConfiguration = Nancy.Diagnostics.DiagnosticsConfiguration(Password = @"goldfish")
        override x.RootPathProvider = typeof<RootPathProvider>

let runPreviewServer path (port:int) = 
    //TODO get rid of this mutation.
    maybeRootPath <- Some(path)

    let nancyHost = 
        let uri = Uri(sprintf "http://localhost:%i" port)
        NancyHost(uri, StaticSiteBootstrapper())

    nancyHost.Start()
