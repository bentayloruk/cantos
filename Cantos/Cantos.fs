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
type MetaValue = | String of string | Int of int
type MetaKey = string
type MetaMap = Map<MetaKey, MetaValue>
    
[<AutoOpen>]
[<RequireQualifiedAccess>]
module Meta = 
    let maybeGetValue (valueMap:MetaMap) key = 
        if valueMap.ContainsKey(key) then Some(valueMap.[key]) else None

type Port = int

[<System.Diagnostics.DebuggerDisplayAttribute("{Path.AbsolutePath}")>]
type TextOutputInfo =
    { Path:RootedPath;
      HadFrontMatter: bool;
      Meta:MetaMap;
      ReaderF:unit->TextReader;
      }

type BinaryOutputInfo =
    { Path:RootedPath
      Meta:MetaMap;
      StreamF:unit->Stream;
      }

//Leaning towards TextOutputInfo and BinaryOutputInfo being interfaces,
//but pushing ahead with records and DU to see what I learn.
type Output =
    //Maybe add "DirectCopy" as an output.
    | TextOutput of TextOutputInfo
    | BinaryOutput of BinaryOutputInfo

module Seq = 
    let mapTextOutput outputs = outputs |> Seq.choose

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<AutoOpen>]
module Output = 

    //let (|TextOutput|) output = function | TextOutput(toi) -> output | _ -> () 

    let mapTextOutput f x = 
        match x with 
        | TextOutput(toi) -> TextOutput(f toi)
        | _ -> x
    
type Generator = MetaMap -> MetaMap * seq<Output> 
type Transformer = Output -> Output


///Template types.
type TemplateName = string
type Template = string
type TemplateMap = Map<TemplateName, Template>

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

