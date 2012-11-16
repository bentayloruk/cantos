namespace Cantos

///
///Path functions.
///
module Path =
    open System.IO

    let dirSeparatorChars = [|  Path.DirectorySeparatorChar; Path.AltDirectorySeparatorChar|]

    let endsWithDirSeparatorChar (path:string) =
        dirSeparatorChars
        |> Seq.exists (fun char -> path.EndsWith(char.ToString()))

    let changeExtension extension path = Path.ChangeExtension(path, extension)

    let combine parts = Path.Combine(parts)

module FileSystem = 
    open System.IO.Abstractions
    let mutable fs = FileSystem() :> IFileSystem

///
///File functions.
///
[<RequireQualifiedAccess>]
module File =
    open FileSystem 

    ///Read all text from file at path.  Uses File.ReadAllText.
    let readAllText path = fs.File.ReadAllText(path)

    let writeAllText path contents = fs.File.WriteAllText(path, contents)


///
///Directory functions.
///
[<RequireQualifiedAccess>]
module Dir =

    open FileSystem

    ///Get seq of descendant dir paths excluding those that match the filter.
    let rec descendantDirs path exclude =

        if fs.Directory.Exists(path) <> true then raiseArgEx (sprintf "Path %s does not exist" path) "path"

        seq { for dir in fs.Directory.GetDirectories(path) do
                let dirInfo = new System.IO.DirectoryInfo(dir)
                if not (exclude dirInfo) then
                    yield! descendantDirs dir exclude 
                    yield dir 
            }

    ///Ensures that directories in the provided path are created.
    let ensureDir path = 
        let dirName = fs.Path.GetDirectoryName(path)
        if fs.Directory.Exists(dirName) <> true then
            fs.Directory.CreateDirectory(dirName) |> ignore

    ///Cleans files and directories below a dir.
    let cleanDir skipDir path =
        if fs.Directory.Exists(path) then 
            for dir in fs.Directory.GetDirectories(path) do
                let dirInfo = System.IO.DirectoryInfo(dir)
                if skipDir dirInfo <> true then 
                    fs.Directory.Delete(dir, true)
            for file in fs.Directory.GetFiles(path) do
                fs.File.Delete(file)
        else ()

    ///Returns all the files in path.
    let getFiles path = fs.Directory.GetFiles(path)

    ///Gets all the files in directory path and all child directories of path (excluding filtered dirs).
    let descendantFilePaths path filter = 
        seq { yield path; yield! descendantDirs path filter }
        |> Seq.map getFiles
        |> Seq.concat

