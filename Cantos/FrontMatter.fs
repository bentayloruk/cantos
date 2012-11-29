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
        if docs.Count <> 1 then raiseNotImpl "We only support a single yaml document at the moment."

        //The root node MUST be a YamlMappingNode.
        match docs.[0].RootNode with 

        | null -> Map.empty 

        | :? YamlMappingNode as mappingNode -> 

            let rec mapToMeta (node:YamlNode) =
                match node with 
                | :? YamlMappingNode as n ->

                    let map =
                        [ for child in mappingNode.Children do

                            //TODO make these conversions active patterns.
                            //TODO properly type scalars - https://github.com/bentayloruk/cantos/issues/8 
                            match child.Key with
                            | (:? YamlScalarNode as key) -> yield (key.ToString().ToLower(), mapToMeta child.Value)
                            | (_) -> raiseNotImpl "We only support Yaml scalar types in Yaml maps." 
                        ]
                        |> Map.ofSeq
                    MetaValue.Mapping(map)
                | :? YamlScalarNode as n -> MetaValue.String(n.Value)
                | :? YamlSequenceNode as n ->
                    let metaValues = n |> Seq.map mapToMeta |> List.ofSeq
                    MetaValue.List(metaValues)
                | x -> raiseNotImpl <| sprintf "Not implemented mapping for YamlNode type %s." (x.GetType().ToString())

            match mapToMeta mappingNode with
            | MetaValue.Mapping(map) -> map 
            | _ -> raiseInvalidOp "Should be a map."

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
