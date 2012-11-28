namespace Cantos

open System
open System.IO
open System.Text
open YamlDotNet.RepresentationModel
open System.Collections.Generic

    
///
///Module with functions to deal with file front matter.
///
module FrontMatter = 

    type FrontMatterBlock = { Text:string; LineCount:int; }

    let FrontMatterFirstLine = "---"
    let FrontMatterLastLine = "---"
    let YamlDocumentEndLine = "..."

    ///Loads Yaml docs from some Yaml document text.
    let yamlDocs yamlDocText = 
        let yaml = YamlStream()
        yaml.Load(new StringReader(yamlDocText))
        yaml.Documents

    ///Converts a yaml doc string into front map.
    let metaValues frontMatterBlock : MetaMap =
        let yamlDoc = sprintf "---\n%s\n..." frontMatterBlock.Text
        let docs = yamlDocs yamlDoc
        if docs.Count <> 1 then raiseNotImpl "We only support one front matter yaml document."

        match docs.[0].RootNode with 

        | null -> Map.empty 

        | :? YamlMappingNode as mappingNode -> 

            [ for child in mappingNode.Children do

                //TODO make these conversions active patterns.
                //TODO properly type scalars - https://github.com/bentayloruk/cantos/issues/8 
                //MAYBE make this recursive for nested yaml?
                match child.Key, child.Value with

                | (:? YamlScalarNode as key), (:? YamlScalarNode as value) -> 
                    let t = value.Value.GetType()
                    yield (key.ToString().ToLower()), MetaValue.String(value.ToString())

                | (:? YamlScalarNode as key), (:? YamlSequenceNode as value) -> 
                    let values = value |> Seq.map (fun node -> node.ToString()) |> List.ofSeq//TODO this is a hack as may not all be Scalar nodes.
                    yield (key.ToString().ToLower()), MetaValue.List(values)

                | (_, _) ->
                    raiseNotImpl "We only have support for simple Yaml key value pairs (YamlScalarNode to YamlScalarNode) at the moment." 

            ] |> Map.ofSeq

        | _ -> Map.empty //Some as yet unsupported YamlNodeType.

    let maybeStringScalar (key:string) (metaValues:MetaMap) =
        let key = key.ToLower()
        if metaValues.ContainsKey(key) then
            match metaValues.[key] with
            | String(s) -> Some(s)
            | _ -> None
        else None
        

    ///Reads the front matter from a reader.  Does not assume Yaml.  Just pulls the text between ---.
    let readFrontMatterBlock (reader:#TextReader) =

        if reader.ReadLine() <> FrontMatterFirstLine then
            None
        else
            let sb = StringBuilder()
            let append (text:string) = sb.AppendLine(text) |> ignore

            let rec readUntilFrontMatterEnds linePos =

                match reader.ReadLine() with

                | line when line = FrontMatterLastLine ->
                    Some({ Text = sb.ToString(); LineCount = linePos + 1})

                | null -> None //The first line was FM but ran off end.  TODO report this? 

                | frontMatter -> 
                    append frontMatter 
                    readUntilFrontMatterEnds (linePos + 1)

            readUntilFrontMatterEnds 1 //as read the first line...

    let getFileFrontMatterMeta path =
        //TODO the return of this tuple is messy.
        use r = File.fileReader path
        match readFrontMatterBlock r with
        | Some(fmBlock) -> true, fmBlock.LineCount, metaValues fmBlock
        | None -> false, 0, Map.empty 
