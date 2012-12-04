[<AutoOpen>]
module Cantos.Meta 

//TODO should extend when MetaMap.  Look up constraints/declaration.
type Map<'a,'b> when 'a : comparison with
    member x.tryGetValue(key) = 
        if x.ContainsKey(key) then Some(x.[key]) else None
    member x.join (q:Map<'a,'b>) = 
        Map(Seq.concat [ (Map.toSeq x) ; (Map.toSeq q) ])

let (|StringValue|_|) key (meta:MetaMap) = 
    let value = meta.tryGetValue key
    match value with
    | Some(MetaValue.String(x)) -> Some(x)
    | _ -> None

let (|BoolValueD|) key def (meta:MetaMap) = 
    let value = meta.tryGetValue key
    match value with
    | Some(MetaValue.String(x)) ->
        let x = x.ToLower()
        if x = "true" || x = "yes" then true
        else false//Should use convertor and throw if rubbish value.
    | _ -> def 

