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

    type FrontMatterValue = | KeyValue of string * string
    type FrontMatterValueList = list<FrontMatterValue>

    let FrontMatterFirstLine= "---"
    let FrontMatterLastLine= "---"
    let YamlDocumentEndLine = "..."
    

    ///Loads Yaml docs from some Yaml document text.
    let yamlDocs yamlDocText = 
        let yaml = YamlStream()
        yaml.Load(new StringReader(yamlDocText))
        yaml.Documents

    ///Converts a yaml doc string into front matter args.
    let yamlArgs (yamlDoc:string) =

        let docs = yamlDocs yamlDoc
        
        if docs.Count > 1 then raiseNotImpl "We only support one front matter yaml document."

        if docs.Count = 0 then
            None
        else
            match docs.[0].RootNode with 
            | null -> None
            | :? YamlMappingNode as mappingNode -> 
                let vars = [
                    for child in mappingNode.Children do
                        match child.Key, child.Value with
                        | (:? YamlScalarNode as key), (:? YamlScalarNode as value) -> 
                            yield FrontMatterValue.KeyValue(key.ToString(), value.ToString())
                        | (_, _) -> raiseNotImpl "We only have support for simple Yaml key value pairs (YamlScalarNode to YamlScalarNode) at the moment." 
                        ]
                Some(vars)
            | _ -> None //Some other type of YamlNode... 

    ///Reads the front matter from a reader.
    let readFrontMatterFromReader (reader:#TextReader) =

        if reader.ReadLine() <> FrontMatterFirstLine then
            None
        else
            let fmText = 

                let sb = StringBuilder()
                let append (text:string) = sb.AppendLine(text) |> ignore

                append FrontMatterFirstLine

                let rec readUntilFrontMatterEnds () =
                    match reader.ReadLine() with
                    | line when line = FrontMatterLastLine ->
                        append YamlDocumentEndLine//Append end of Yaml doc rather than our end of front matter --- 
                    | null -> ()
                    | frontMatter -> 
                        append frontMatter 
                        readUntilFrontMatterEnds ()
                readUntilFrontMatterEnds ()

                sb.ToString()

            if String.IsNullOrEmpty(fmText) then
                None 
            else yamlArgs fmText

    ///Reads front matter from a string.
    let frontMatterArgs(text:string) =
        use reader = new StringReader(text)
        readFrontMatterFromReader reader
        
    ///Reads front matter from a file.  Returns None if no front matter and list of 0-* args if any.
    let readFileFrontMatterArgs (filePath:string) =
        if not <| File.Exists(filePath) then raiseArgEx "File does not exist." filePath
        use stream = File.Open(filePath, FileMode.Open, FileAccess.Read)
        use reader = new StreamReader(stream)
        readFrontMatterFromReader reader


