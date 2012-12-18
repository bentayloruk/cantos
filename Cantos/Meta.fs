[<AutoOpen>]
module Cantos.Meta 

open System.Collections.Generic

//TODO should extend when MetaMap.  Look up constraints/declaration.
type Map<'a,'b> when 'a : comparison with
    member x.tryGetValue(key) = 
        if x.ContainsKey(key) then Some(x.[key]) else None
    member x.join (q:Map<'a,'b>) = 
        Map(Seq.concat [ (Map.toSeq x) ; (Map.toSeq q) ])

let metaToDic (metaMap:MetaMap) = 

    //TODO make this tail recursive.
    let rec inner x =
        match x with
        | Mapping(map) ->
            let dic = Dictionary<string,obj>()
            map |> Seq.iter (fun kvp -> dic.Add(kvp.Key, inner kvp.Value))
            dic :> obj
        | Object(o) -> o
        | DateTime(dt) -> dt :> obj
        | String(s) -> s :> obj
        | Int(i) -> i :> obj
        | List(l) -> l |> Seq.map (fun item -> inner item) |> List.ofSeq :> obj

    let o = inner (Mapping(metaMap))

    //Always a Dictionary<string,objj>
    o :?> Dictionary<string,obj>

let (|StringValue|_|) key (meta:MetaMap) = 
    let value = meta.tryGetValue key
    match value with
    | Some(MetaValue.String(x)) -> Some(x)
    | _ -> None

let (|ValueOrDefault|) key def (meta:MetaMap) = 
    let value = meta.tryGetValue key
    match value with
    | Some(MetaValue.String(x)) ->
        let x = x.ToLower()
        if x = "true" || x = "yes" then true
        else false//Should use convertor and throw if rubbish value.
    | _ -> def 

let (|LayoutName|_|) map =
    match map with 
    | StringValue "layout" v -> Some(v) 
    | _ -> None
