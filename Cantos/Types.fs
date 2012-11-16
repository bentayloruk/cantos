namespace Cantos

open System
open System.IO


///Type responsible for wrangling absolute and relative file and URLs for files in the site.
//TODO run tests for this on MONO!
type SitePath private (siteRootPath, siteRootRelativePath) =

    let absolutePath = Path.Combine(siteRootPath, siteRootRelativePath)

    ///Creates a new SitePath.
    static member Create(siteRootDir, siteRootRelativePath) = 
        //TODO path rooted does not mean we'll get an absolute path.  
        if Path.IsPathRooted(siteRootDir) <> true then
            raiseArgEx "Not an absolute path." "siteRootPath"
        if Path.endsWithDirSeparatorChar siteRootDir <> true then
            raiseArgEx "Does not end with a directory separator character." "siteRootPath"
        if Path.IsPathRooted(siteRootRelativePath) = true then
            raiseArgEx "Not a relative path." "siteRootRelativePath"
        SitePath(siteRootDir, siteRootRelativePath)

    member x.AbsolutePath = absolutePath

    member x.RelativePath = siteRootRelativePath

    member x.IsSameRelativePathOrParent(sitePath:SitePath) =
        if siteRootRelativePath = sitePath.RelativePath then true
        else sitePath.RelativePath.StartsWith(siteRootRelativePath)
        
    ///Creates a SitePath from path.  path must be a descendant of this SitePath instance.
    member x.RelativeSitePath(path) =
        if Path.IsPathRooted(path) then
            let expectedParentPath = path.Substring(0, absolutePath.Length)
            if absolutePath <> expectedParentPath then
                let msg = sprintf "%s is not a child of %s (compared %s)." path absolutePath expectedParentPath
                raise <| InvalidOperationException(msg)
            SitePath(siteRootPath, path.Substring(siteRootPath.Length))
        else
            SitePath(siteRootPath, Path.Combine(siteRootRelativePath, path)) 
            
    ///Converts the SitePath into a rooted url for use in HTML (e.g. /folder-in-site-root/page.html)
    member x.RootUrl =
        let relativeUrl = siteRootRelativePath.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/')
        "/" + relativeUrl //rooted url

    member x.SwitchRoot(newRootPath) =
        if Path.IsPathRooted(newRootPath) then
            SitePath(newRootPath, siteRootRelativePath)
        else
            raiseArgEx "Provide a rooted path." "path"

    member x.SwitchRelative(newRelativePath) =
        SitePath(siteRootPath, newRelativePath)

    member x.ChangeExtension(extension) =
        if Path.endsWithDirSeparatorChar siteRootRelativePath then raiseInvalidOp "Path ends with directory separator char.  Probably not a file."
        let newRelative = Path.changeExtension extension siteRootRelativePath
        SitePath.Create(siteRootPath, newRelative)
       
        
type ITracer =
    abstract member Error : string -> unit
    abstract member Info : string -> unit
    abstract member Warning : string -> unit

type ConsoleTracer() =
    let write (msg:string) = System.Console.WriteLine(msg)
    interface ITracer with
        member x.Error(msg) = write msg
        member x.Info(msg) = write msg
        member x.Warning(msg) = write msg

///Contains configuration infomation for this site.
type SiteConfig = 
    { SiteInPath:SitePath;
      SiteOutPath:SitePath;
      Tracer:ITracer; }
    member x.InSitePath(relativePath) = SitePath.Create(x.SiteInPath.AbsolutePath, relativePath)

//
//Extension types.
//
type Exclusion<'a> = 'a -> bool
type SitePathProcessor = SitePath -> SitePath
type DirectoryInfoExclusion = Exclusion<DirectoryInfo> 
type FileInfoExclusion = Exclusion<FileInfo> 

