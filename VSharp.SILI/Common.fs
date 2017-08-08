﻿namespace VSharp

open VSharp.Terms

module internal Common =

    let internal simplifyPairwiseCombinations = Propositional.simplifyPairwiseCombinations

    let rec internal simplifyGenericUnary name state x matched concrete unmatched =
        match x with
        | Error _ -> matched (x, state)
        | Concrete(x, typeofX) -> concrete x typeofX state |> matched
        | GuardedValues(guards, values) ->
            Cps.List.mapFoldk (fun state term matched -> simplifyGenericUnary name state term matched concrete unmatched) state values (fun (values', state) ->
                (Merging.merge (List.zip guards values'), state) |> matched)
        | _ -> unmatched x state matched

    let rec internal simplifyGenericBinary name state x y matched concrete unmatched repeat =
        match x, y with
        | Error _, _ -> matched (x, state)
        | _, Error _ -> matched (y, state)
        | Concrete(x, typeOfX), Concrete(y, typeOfY) -> concrete x y typeOfX typeOfY state |> matched
        | Union(gvsx), Union(gvsy) ->
            let compose (gx, vx) state (gy, vy) matched = repeat vx vy state (fun (xy, state) -> ((gx &&& gy, xy), state) |> matched) in
                let join state (gx, vx) k = Cps.List.mapFoldk (compose (gx, vx)) state gvsy k in
                    Cps.List.mapFoldk join state gvsx (fun (gvss, state) -> (Merging.merge (List.concat gvss), state) |> matched)
        | GuardedValues(guardsX, valuesX), _ ->
            Cps.List.mapFoldk (fun state x matched -> repeat x y state matched) state valuesX (fun (values', state) ->
            (Merging.merge (List.zip guardsX values'), state) |> matched)
        | _, GuardedValues(guardsY, valuesY) ->
            Cps.List.mapFoldk (fun state y matched -> repeat x y state matched) state valuesY (fun (values', state) ->
            (Merging.merge (List.zip guardsY values'), state) |> matched)
        | _ -> unmatched x y state matched

    and is (leftType : TermType) (rightType : TermType) =
        let makeBoolConst name termType = Terms.FreshConstant name (SymbolicConstantType termType) typedefof<bool>
        in
        let concreteIs (dotNetType : System.Type) =
            let b = makeBoolConst dotNetType.FullName (ClassType dotNetType) in
            function
            | Types.ReferenceType t
            | Types.StructureType t -> Terms.MakeBool (t = dotNetType)
            | SubType(t, name) as termType when t.IsAssignableFrom(dotNetType) ->
                makeBoolConst name termType ==> b
            | SubType(t, _) when not <| t.IsAssignableFrom(dotNetType) -> Terms.MakeFalse
            | ArrayType _ -> Terms.MakeBool <| dotNetType.IsAssignableFrom(typedefof<obj>)
            | VSharp.Null -> Terms.MakeFalse
            | Object name as termType -> makeBoolConst name termType ==> b
            | _ -> __notImplemented__()
        in
        let subTypeIs (dotNetType: System.Type, rightName) =
            let b = makeBoolConst rightName (SubType(dotNetType, rightName)) in
            function
            | Types.ReferenceType t -> Terms.MakeBool <| dotNetType.IsAssignableFrom(t)
            | Types.StructureType t -> Terms.MakeBool (dotNetType = typedefof<obj> || dotNetType = typedefof<System.ValueType>)
            | SubType(t, name) when dotNetType.IsAssignableFrom(t) -> Terms.MakeTrue
            | SubType(t, name) as termType when t.IsAssignableFrom(dotNetType) ->
                makeBoolConst name termType ==> b
            | ArrayType _ -> Terms.MakeBool <| dotNetType.IsAssignableFrom(typedefof<obj>)
            | VSharp.Null -> Terms.MakeFalse
            | Object name as termType -> makeBoolConst name termType ==> b
            | _ -> __notImplemented__()
        in
        match leftType, rightType with
        | Void, _   | _, Void
        | Bottom, _ | _, Bottom -> Terms.MakeFalse
        | Func _, Func _ -> Terms.MakeTrue
        | ArrayType(t1, c1), ArrayType(Object "Array", 0) -> Terms.MakeTrue
        | ArrayType(t1, c1), ArrayType(t2, c2) -> Terms.MakeBool <| ((t1 = t2) && (c1 = c2))
        | leftType, Types.StructureType t when leftType <> Null -> concreteIs t leftType
        | leftType, Types.ReferenceType t -> concreteIs t leftType
        | leftType, SubType(t, name) -> subTypeIs (t, name) leftType
        | leftType, Object name -> subTypeIs (typedefof<obj>, name) leftType
        | _ -> __notImplemented__()
