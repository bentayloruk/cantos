module Cantos.Tests.FrontMatterTests

open Cantos
open Xunit
open Swensen.Unquote
open System
open FrontMatter 
open System.IO


let textWithFrontMatter = """markdown: rdiscount
pygments: true"""
    
[<Fact>]
let ``Parse Yaml OK`` () =
    test
        <@
        let args = yamlArgs textWithFrontMatter 
        args.IsSome && args.Value.Length = 2
        @>
   
