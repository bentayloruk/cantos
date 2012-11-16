namespace Cantos

/////Provides the template files.
//type Templates(templatePath:InPath, siteConfig:SiteConfig) =
//
//    interface IPlugin with member x.Name = "Default Templates Plugin" 
//
//    interface IGetStreams with
//        member x.GetStreams() =
//            failwith "NotImplemented"
////            Dir.getFiles(siteConfig.GetFullPathString(templatePath))
////            //|> Seq.filter isValidSiteFile
////            |> Seq.choose(fun templatePath ->
////                try
////                    use stream = File.Open(templatePath, FileMode.Open, FileAccess.Read)
////                    use reader = new StreamReader(stream)
////                    let fmArgs = argsFromFrontMatter reader
////                    let parent = getArgValueOpt "template" fmArgs
////                    let template = reader.ReadToEnd()
////                    let templateFile = Path.GetFileName(templatePath)
////                    let values = valueTuples fmArgs |> List.ofSeq
////                    Some((templateFile, {FileName=templateFile; Template=template; ParentFileName=parent; Vars = values}))
////                with
////                | :? IOException -> None 
////            )
//
//
//

(*
type RequiredPath private (path) =
    static member Create(path) =
        if Directory.Exists(path) then RequiredPath(path)
        else raise <| ArgumentException("Path does not exist. %s", path)
    override x.ToString() = path
*)


