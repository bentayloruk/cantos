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
    let yamlArgs frontMatterBlock : MetaValueMap =
        
        let yamlDoc = sprintf "---\n%s\n..." frontMatterBlock.Text
        let docs = yamlDocs yamlDoc
        if docs.Count <> 1 then raiseNotImpl "We only support one front matter yaml document."

        match docs.[0].RootNode with 
        | null -> Map.empty 
        | :? YamlMappingNode as mappingNode -> 
            [ for child in mappingNode.Children do
                match child.Key, child.Value with
                | (:? YamlScalarNode as key), (:? YamlScalarNode as value) -> 
                    yield (key.ToString()), MetaValue.String(value.ToString())
                | (_, _) -> raiseNotImpl "We only have support for simple Yaml key value pairs (YamlScalarNode to YamlScalarNode) at the moment." 
            ] |> Map.ofSeq
        | _ -> Map.empty //Some other type of YamlNode... 

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

    let maybeReadFrontMatterArgs reader =
        match readFrontMatterBlock reader with
        | Some(fmBlock) -> Some(yamlArgs fmBlock)
        | None -> None