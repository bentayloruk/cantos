module Cantos.StepDefinitions

open TickSpec
open System.IO.Abstractions.TestingHelpers
open System.IO.Abstractions
open System.Collections.Generic
open System.Text.RegularExpressions

open FileSystem
fs <- MockFileSystem(new Dictionary<string,MockFileData>()) :> IFileSystem

let sitePath = @"c:\blog\ben\"
let rootPath relative = Path.combine [| sitePath; relative; |] 
let mkdir path = fs.Directory.CreateDirectory(path) |> ignore
let writeFile path contents = fs.File.WriteAllText(path, contents)

let [<Given>] ``I have a (.*) directory`` (path:string) = mkdir path

let [<Given>] ``I have the following post:`` (postTable:Table) =
    ()

let [<Given>] ``I have the following posts:`` (postsTable:Table) =
    ()
    
let [<Given>] ``I have a (.*) layout that contains (.*)`` (layoutName:string) (layoutBody:string) =
    let layoutPath = Path.combine [| sitePath; "_layouts"; layoutName + ".html" |]
    writeFile layoutPath layoutBody

let [<Given>] ``I have a ordered layout that contains "(.*)"`` (text:string) =
    ()
    
let [<Given>] ``I have an "(.*)" file that contains "(.*)"`` (filePath:string) (text:string) =
    ()
    
let [<Given>] ``I have the following post in "(.*)":`` (category:string) (postTable:Table) =
    ()

let [<When>] ``I run cantos`` () =
    let sc, ic, pp = Jekyll.cantosConfig sitePath 
    Program.runCantos sc ic pp
    ()

let [<Then>] ``the (.*) directory should exist`` (path:string) =
    fs.Directory.Exists(rootPath path)

let [<Then>] ``I should see "(.*)" in "(.*)"`` (pattern:string) (path:string) =
    let fileContents = fs.File.ReadAllText(rootPath path)
    Regex.IsMatch(fileContents, pattern)

let [<Then>] ``the "(.*)" file should not exist`` (filePath:string) =
    ()

    