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
        args.Count = 2
        @>

[<Fact>]
let ``Should report number of lines of front matter in the file`` () =
    let sr = new StringReader("""---
markdown: rdiscount
pygments: true
---
this is
content and
stuff""")

    test
        <@
        match readFrontMatterFromReader sr with
        | None -> failwith "Should be some"
        | Some(fm, fmLines) -> fmLines = 4
        @>