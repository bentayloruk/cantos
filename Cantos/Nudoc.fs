module Nudoc
open System
open System.Reflection
open ClariusLabs.NuDoc 

let print x = printfn "%s" x

type private Debug() =
    inherit Visitor()
    override __.VisitMember(m) =
        print (m.ToText())

let docAssembly path = 
    let members =
        let assembly = Assembly.LoadFrom(path)
        Reader.Read(assembly)
    let visitor = new Debug()
    visitor.VisitAssembly(members)

    

