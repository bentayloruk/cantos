module Cantos.SitePathTests

open Xunit
open Swensen.Unquote
open System
open System.IO

[<Fact>]
let ``siteRootPath ctor arg must end with dir slash`` () =
    raises<ArgumentException> <@ SitePath.Create(@"c:\site", @"docs\ben\") @>

[<Fact>]
let ``siteRootPath ctor arg must be path rooted`` () =
    raises<ArgumentException> <@ SitePath.Create(@"site", @"docs\ben\") @>

[<Fact>]
let ``RootUrl correct when relative path is dir`` () =
    test <@ SitePath.Create(@"c:\site\", @"docs\ben\").RootUrl = @"/docs/ben/" @>

[<Fact>]
let ``RelativeSitePath works with absolute path`` () =
    test
        <@
            let sitePath = SitePath.Create(@"c:\site\", @"docs\ben")
            let fileSitePath = sitePath.RelativeSitePath(@"c:\site\docs\ben\chap1\file.html")
            fileSitePath.RootUrl = "/docs/ben/chap1/file.html"
        @>

[<Fact>]
let ``RelativeSitePath works with relative path`` () =
    test
        <@
            let sitePath = SitePath.Create(@"c:\site\", @"docs\ben")
            let fileSitePath = sitePath.RelativeSitePath(@"chap1\file.html")
            fileSitePath.RootUrl = "/docs/ben/chap1/file.html"
        @>

[<Fact>]
let ``ChangeExtension should work when source is file`` () =
    test
        <@
        let sp = SitePath.Create(@"c:\ben\", @"test.md")
        let sp = sp.ChangeExtension(".html")
        Path.GetExtension(sp.AbsolutePath) = ".html"
        @>


[<Fact>]
let ``SameRelativePathOrChild spec`` () =
    let spa = SitePath.Create(@"c:\in\", @"child1\child2\")
    let spb = SitePath.Create(@"c:\out\", @"child1\child2\child3\test.md")
    Assert.True(spa.IsSameRelativePathOrParent(spb))

[<Fact>]
let ``ChangeExtension should throw invalidop when source is Dir`` () =
    let sp = SitePath.Create(@"c:\ben\", @"sub-dir\")
    Assert.Throws<InvalidOperationException>(fun () -> sp.ChangeExtension(".html") |> ignore)

[<Fact>]
let ``RootUrl returns file URL even when no file extension`` () =
    test <@ SitePath.Create(@"c:\site\", @"docs\ben").RootUrl = @"/docs/ben" @>

[<Fact>]
let ``RootUrl correct when relative path is file`` () =
    test <@ SitePath.Create(@"c:\site\", @"docs\ben\test.html").RootUrl = @"/docs/ben/test.html" @>

    
