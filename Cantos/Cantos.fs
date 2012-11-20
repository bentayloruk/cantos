namespace Cantos

(*
Home of the main Cantos types
*)


///Site configuration information.
type SiteConfig = 
    { SiteInPath:SitePath;
      SiteOutPath:SitePath;
      Tracer:ITracer; }
    member x.InSitePath(relativePath) = SitePath.Create(x.SiteInPath.AbsolutePath, relativePath)

open System.IO

//Types used to control Cantos behaviour.

//File and directory exclusions.
type Exclusion<'a> = 'a -> bool
type DirectoryInfoExclusion = Exclusion<DirectoryInfo> 
type FileInfoExclusion = Exclusion<FileInfo> 

//Path processors.  Used to manipulate output SitePaths.
type SitePathProcessor = SitePath -> SitePath

type Port = int
type PreviewHttpServer = SitePath -> Port -> unit


