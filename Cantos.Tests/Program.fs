﻿module Program

open System.Reflection
open TickSpec

let ass = Assembly.GetExecutingAssembly()
let definitions = new StepDefinitions(ass)

[<TickFact>]
let Feature1 () =
   let source = @"PostDataFeature.txt"
   let s = ass.GetManifestResourceStream(source)   
   definitions.GenerateScenarios(source,s)   


