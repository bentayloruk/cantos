namespace Cantos

open System
open System.IO

///Represents a rooted path.  Used to manipulate the relative part of the path, but not the root.
type RootedPath private (rootPart, relativePart) =

    let absolutePath = Path.Combine(rootPart, relativePart)
    let uri = Uri(absolutePath)
    do
        if uri.IsFile <> true then raiseArgEx "RelativePart is not a file." "relativePart"

    ///Creates a new RootedPath.
    static member Create(rootPart, relativePart) = 
        if Path.IsPathRooted(rootPart) <> true then raiseArgEx "Not an absolute path." "rootPart"
        if Path.IsPathRooted(relativePart) = true then raiseArgEx "Not a relative path." "relativePart"
        RootedPath(rootPart, relativePart)

    member x.AbsolutePath = absolutePath

    member x.HasExtension extensions = 
        //Probably should only take one extension and leave the Seq.exists to elsewhere.
        if Path.HasExtension(absolutePath) then
            let extension = FileExtension.Create(Path.GetExtension(absolutePath).Substring(1))//Remove leading .
            extensions |> Seq.exists (fun ex -> extension.Equals(ex))
        else false

    member x.RelativePath = relativePart

    member x.FileName = Path.GetFileName(absolutePath)

    member x.IsSameRelativePathOrParent(rootedPath:RootedPath) =
        if relativePart = rootedPath.RelativePath then true
        else rootedPath.RelativePath.StartsWith(relativePart)
        
    ///Creates a RootedPath from path.  path must be a descendant of this RootedPath instance.
    member x.RelativeRootedPath(path) =
        if Path.IsPathRooted(path) then
            let expectedParentPath = path.Substring(0, absolutePath.Length)
            if absolutePath <> expectedParentPath then
                let msg = sprintf "%s is not a child of %s (compared %s)." path absolutePath expectedParentPath
                raise <| InvalidOperationException(msg)
            RootedPath(rootPart, path.Substring(rootPart.Length))
        else
            RootedPath(rootPart, Path.Combine(relativePart, path)) 
            
    ///Converts the RootedPath into a rooted url for use in HTML (e.g. /folder-in-site-root/page.html)
    member x.RootUrl =
        let relativeUrl = relativePart.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/')
        "/" + relativeUrl //rooted url

    member x.SwitchRoot(newRootPath) =
        if Path.IsPathRooted(newRootPath) then
            RootedPath(newRootPath, relativePart)
        else
            raiseArgEx "Provide a rooted path." "path"

    member x.SwitchRelative(newRelativePath) =
        RootedPath(rootPart, newRelativePath)

    member x.ChangeExtension(extension:FileExtension) =
        if Path.endsWithDirSeparatorChar relativePart then raiseInvalidOp "Path ends with directory separator char.  Probably not a file."
        let newRelative = Path.changeExtension (extension.ToString()) relativePart
        RootedPath.Create(rootPart, newRelative)

