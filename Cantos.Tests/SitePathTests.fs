module Cantos.RootedPathTests

open Xunit
open Swensen.Unquote
open System
open System.IO

[<Fact>]
let ``siteRootPath ctor arg must end with dir slash`` () =
    raises<ArgumentException> <@ RootedPath.Create(@"c:\site", @"docs\ben\") @>

[<Fact>]
let ``siteRootPath ctor arg must be path rooted`` () =
    raises<ArgumentException> <@ RootedPath.Create(@"site", @"docs\ben\") @>

[<Fact>]
let ``RootUrl correct when relative path is dir`` () =
    test <@ RootedPath.Create(@"c:\site\", @"docs\ben\").RootUrl = @"/docs/ben/" @>

[<Fact>]
let ``RelativeRootedPath works with absolute path`` () =
    test
        <@
            let sitePath = RootedPath.Create(@"c:\site\", @"docs\ben")
            let fileRootedPath = sitePath.CreateRelative(@"c:\site\docs\ben\chap1\file.html")
            fileRootedPath.RootUrl = "/docs/ben/chap1/file.html"
        @>

[<Fact>]
let ``RelativeRootedPath works with relative path`` () =
    test
        <@
            let sitePath = RootedPath.Create(@"c:\site\", @"docs\ben")
            let fileRootedPath = sitePath.CreateRelative(@"chap1\file.html")
            fileRootedPath.RootUrl = "/docs/ben/chap1/file.html"
        @>

[<Fact>]
let ``ChangeExtension should work when source is file`` () =
    test
        <@
        let sp = RootedPath.Create(@"c:\ben\", @"test.md")
        let sp = sp.ChangeExtension(FileExtension.Create("html"))
        Path.GetExtension(sp.AbsolutePath) = ".html"
        @>


[<Fact>]
let ``SameRelativePathOrChild spec`` () =
    let spa = RootedPath.Create(@"c:\in\", @"child1\child2\")
    let spb = RootedPath.Create(@"c:\out\", @"child1\child2\child3\test.md")
    Assert.True(spa.IsSameRelativePathOrParent(spb))

[<Fact>]
let ``ChangeExtension should throw invalidop when source is Dir`` () =
    let sp = RootedPath.Create(@"c:\ben\", @"sub-dir\")
    Assert.Throws<InvalidOperationException>(fun () -> sp.ChangeExtension(FileExtension.Create("html")) |> ignore)

[<Fact>]
let ``RootUrl returns file URL even when no file extension`` () =
    test <@ RootedPath.Create(@"c:\site\", @"docs\ben").RootUrl = @"/docs/ben" @>

[<Fact>]
let ``RootUrl correct when relative path is file`` () =
    test <@ RootedPath.Create(@"c:\site\", @"docs\ben\test.html").RootUrl = @"/docs/ben/test.html" @>

    
