module Nancy 

open Nancy.Hosting.Self
open Nancy.Conventions
open Nancy
open System

type StaticSiteBootstrapper(path) =

    inherit DefaultNancyBootstrapper()
    with override x.ConfigureConventions(conventions) =
            base.ConfigureConventions(conventions)
            let convention = StaticContentConventionBuilder.AddDirectory("/", path)
            let _ = conventions.StaticContentsConventions.Add(convention)
            ()

let runPreviewServer path (port:int) = 

    let nancyHost = 
        let uri = Uri(sprintf "http://localhost:%i" port)
        NancyHost(uri, StaticSiteBootstrapper(path))

    nancyHost.Start()
