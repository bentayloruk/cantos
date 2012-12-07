[<AutoOpen>]
module Uri
open System
open System.IO
open System.Text.RegularExpressions

let dateSlugRegex = """^(\d+-\d+-\d+)-(.*)(\.[^.]+)$"""

///Extensions to the Uri class.
type Uri with

    ///Case insensitive check for extension.
    member x.HasFileExtension(extensions) =
        if x.IsFile && Path.HasExtension(x.LocalPath) then 
            let actual = Path.GetExtension(x.LocalPath)
            extensions |> Seq.exists (fun expected -> String.Compare(actual, expected, StringComparison.InvariantCultureIgnoreCase) = 0)
        else false

    //TODO using this LocalPathunescaped is all a bit hacky.  Review. 
    member x.CombineWithParts(parts) = Uri(Path.Combine(x.LocalPathUnescaped :: parts |> Array.ofList))

    member x.CombineWithRelativeUri(uri:Uri) = Uri(Path.Combine(x.LocalPathUnescaped, uri.OriginalString))

    ///Get the safe unescaped filename (as LocalPath has in %20).
    member x.LocalPathUnescaped = Uri.UnescapeDataString(x.LocalPath)

    member x.FileName = Path.GetFileName(x.LocalPath)

    member x.FileNameWithoutExtension = Path.GetFileNameWithoutExtension(x.LocalPath)

    member x.WithExtension(extension) = Uri(Path.ChangeExtension(x.LocalPath, extension))


///Matches Uri with 20-12-2013-this-is-slug.md filenames.
let (|DateSlugFormat|_|) (uri:Uri) = 
    let matches = Regex.Match(uri.FileName, dateSlugRegex)
    if matches.Success then Some(DateTime.Parse(matches.Groups.[1].Value), matches.Groups.[2].Value)
    else None

