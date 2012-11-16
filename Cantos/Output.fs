namespace Cantos


module Output =
    open System.IO

    ///Removes leading numbers and -.  Example:  1010-myname -> myname.  
    let deNumberWang (name:string) =
        let wangIndex = name.IndexOf('-')
        if wangIndex = -1 then name else
            let maybeNumber = name.Substring(0, wangIndex)
            let (parsed, number) = System.Int32.TryParse(maybeNumber)
            if parsed = true then name.Substring(wangIndex+1) else name 

    ///Removes leading numbers from file and dir paths (e.g. /2222-dirname/1234-file.html -> /dirname/file.html).
    let deNumberWangPath (path:string) =
        //Example:
        //This -> "developer\0100-introduction\0075-enticify-connector-for-commerce-server.md"
        //Becomes this -> "developer\introduction\enticify-connector-for-commerce-server.md"
        path.Split(Path.dirSeparatorChars)
        |> Seq.map deNumberWang 
        |> Array.ofSeq
        |> Path.combine
