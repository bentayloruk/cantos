module ServiceStackHttpServer

open System
open ServiceStack.ServiceHost
open ServiceStack.WebHost.Endpoints
 
type Hello = { mutable Name: string; }
type HelloResponse = { mutable Result: string; }
type HelloService() =
    interface IService<Hello> with
        member this.Execute (req:Hello) = { Result = "Hello, " + req.Name } :> Object
 
//Define the Web Services AppHost
type AppHost =
    inherit AppHostHttpListenerBase
    new() = { inherit AppHostHttpListenerBase("Hello F# Services", typeof<HelloService>.Assembly) }
    override this.Configure container =
        base.Routes
            .Add<Hello>("/about.html")
            .Add<Hello>("/hello/{Name}") |> ignore
 
//Run it!
open System.IO

let runPreviewServer path (port:int) = 
    let host = sprintf "http://localhost:%i/" port
    printfn "listening on %s ..." host
    let appHost = new AppHost()
    let vpp = ServiceStack.VirtualPath.FileSystemVirtualPathProvider(appHost :> IAppHost, DirectoryInfo(path))
    appHost.VirtualPathProvider <- vpp
    appHost.Init()
    appHost.Start host