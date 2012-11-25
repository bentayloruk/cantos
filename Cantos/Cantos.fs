namespace Cantos

open System
open System.IO

(*
Home of the main Cantos types
*)

//Types used to control Cantos behaviour.

//Exclusions.
type Exclusion<'a> = 'a -> bool
type UriExclusion = Exclusion<Uri> 
type DirectoryExclusion = Exclusion<DirectoryInfo> 
type FileExclusion = Exclusion<FileInfo> 

//Output meta data types.
type MetaValue = | String of string
type MetaValueKey = string
type MetaValueMap = Map<MetaValueKey, MetaValue>

type Port = int

type TextOutputInfo =
    { Path:RootedPath;
      HadFrontMatter: bool;
      Meta:MetaValueMap;
      ReaderF:unit->TextReader;
      }

type BinaryOutputInfo =
    { Path:RootedPath
      Meta:MetaValueMap;
      StreamF:unit->Stream;
      }

type Output =
    //TODO rename record or DU as will be confusing...
    | TextOutput of TextOutputInfo
    | BinaryOutput of BinaryOutputInfo

type Generator = unit -> seq<Output> 
type Processor = Output -> Output

module Meta = 
    let maybeGetValue (valueMap:MetaValueMap) key = 
        if valueMap.ContainsKey(key) then Some(valueMap.[key]) else None

///Template types.
type TemplateName = string
type Template = { Name:TemplateName}
type TemplateMap = Map<TemplateName, Template>
type TemplateFrontMatter = { Name:string }

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

