namespace Cantos

open System
open System.IO

(*
Home of the main Cantos types
*)

//Meta types.
type MetaKey = string
and MetaMap = Map<MetaKey, MetaValue>
and MetaValue = | String of string | Int of int | List of list<MetaValue> | Mapping of MetaMap | Object of obj

[<System.Diagnostics.DebuggerDisplayAttribute("{Uri.LocalPath}")>]
type TextContent =
    { Meta:MetaMap
      HadFrontMatter:bool
      ReaderF:unit->TextReader
      Uri:Uri }

type BinaryContent = 
    { Meta:MetaMap
      StreamF:unit->Stream
      Uri:Uri }

type Content =
    //Maybe add "DirectCopy" as an output.
    | TextContent of TextContent 
    | BinaryContent of BinaryContent

type Site = 
    { InPath:Uri
      OutPath:Uri
      Meta:MetaMap }

type Generator = Site -> unit 
type Transformer = Site -> Content -> Content

//Exclusions.
type Exclusion<'a> = 'a -> bool
type UriExclusion = Exclusion<Uri> 
type DirectoryExclusion = Exclusion<DirectoryInfo> 
type FileExclusion = Exclusion<FileInfo> 
    
type Port = int

///Template types.
type TemplateName = string
type TemplateInfo = { FileName:string; Template:string; Meta:MetaMap } 
type TemplateMap = Map<TemplateName, TemplateInfo>

