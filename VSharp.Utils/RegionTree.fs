namespace VSharp

/// Implementation of region tree. Region tree is a tree indexed by IRegion's and having the following invariants:
/// - all regions of one node are disjoint;
/// - the parent region includes all child regions.

type IRegionTreeKey<'a> =
    abstract Hides : 'a -> bool

type regionTree<'key, 'reg when 'reg :> IRegion<'reg> and 'key : equality and 'reg : equality and 'key :> IRegionTreeKey<'key>> =
    | Node of pdict<'reg, 'key * regionTree<'key, 'reg>>

module RegionTree =
    let empty<'key, 'reg when 'reg :> IRegion<'reg> and 'key : equality and 'reg : equality and 'key :> IRegionTreeKey<'key>> : regionTree<'key, 'reg> = Node PersistentDict.empty

    let isEmpty (Node d) = PersistentDict.isEmpty d

    type private filterResult = FilteredOut | Preserved of RegionComparisonResult

    // Returns subtree of d completely covered by reg
    let rec private splitNode filterOut (reg : 'a when 'a :> IRegion<'a>) (Node d) =
        if PersistentDict.isEmpty d then PersistentDict.empty, PersistentDict.empty
        else
            match PersistentDict.tryFind d reg with
            | Some x -> (if filterOut (fst x) then PersistentDict.empty else PersistentDict.add reg x PersistentDict.empty), PersistentDict.remove reg d
            | None ->
                let groups = PersistentDict.groupBy (fun (reg', key) -> if filterOut (fst key) then FilteredOut else reg.CompareTo reg' |> Preserved) d
                let empty() = Seq.empty
                let included = Option.defaultWith empty (PersistentDict.tryFind groups (Preserved Includes)) |> PersistentDict.ofSeq
                let disjoint = Option.defaultWith empty (PersistentDict.tryFind groups (Preserved Disjoint)) |> PersistentDict.ofSeq
                let intersected = Option.defaultWith empty (PersistentDict.tryFind groups (Preserved Intersects))
                let splitChild (included, disjoint) (reg' : 'a, (k, child)) =
                    let splittedIncluded, splittedDisjoint = splitNode filterOut reg child
                    (PersistentDict.add (reg'.Intersect reg) (k, Node splittedIncluded) included), (PersistentDict.add (reg'.Subtract reg) (k, Node splittedDisjoint) disjoint)
                Seq.fold splitChild (included, disjoint) intersected

    let localize reg tree = splitNode (always false) reg tree |> fst

    // NOTE: [ATTENTION] must be used only if 'reg' were not added earlier and keys are disjoint
    // NOTE: used for fast initialization of new array
    let memset regionsAndKeys (Node tree) =
        regionsAndKeys |> Seq.fold (fun acc (reg, k) -> PersistentDict.add reg (k, Node PersistentDict.empty) acc) tree |> Node

    let write reg key tree =
        let included, disjoint = splitNode (fun key' -> (key :> IRegionTreeKey<_>).Hides key') reg tree
        Node(PersistentDict.add reg (key, Node included) disjoint)

    let rec foldl folder acc (Node d) =
        PersistentDict.fold (fun acc reg (k, t) -> let acc = folder acc reg k in foldl folder acc t) acc d

    let rec foldr folder acc (Node d) =
        PersistentDict.fold (fun acc reg (k, t) -> let acc = foldr folder acc t in folder reg k acc) acc d

    let rec private filterRec reg predicate ((Node d) as tree) =
        let mutable modified = false
        let mutable result = d
        for (reg', (k, t)) in PersistentDict.toSeq d do
            if reg'.CompareTo reg <> Disjoint then
                if predicate k then
                    match filterRec reg predicate t with
                    | true, t' ->
                        modified <- true
                        result <- PersistentDict.add reg' (k, t') result
                    | _ -> ()
                else result <- PersistentDict.remove reg' result
        modified, (if modified then Node result else tree)

    let filter reg predicate tree =
        filterRec reg predicate tree |> snd

    let rec map (mapper : 'a -> 'key -> 'a * 'a * 'key when 'a :> IRegion<'a>) tree =
        let folder reg k acc =
            let reg, reg', k' = mapper reg k
            if reg'.CompareTo reg = Disjoint then acc
            else write (reg.Intersect reg') k' acc
        foldr folder empty tree

    let rec append baseTree appendix =
        foldr write baseTree appendix

    let rec flatten tree =
        foldr (fun reg' k acc -> (reg', k)::acc) [] tree

    let rec private checkInvariantRec (parentReg : IRegion<'a> option) (Node d) =
        match parentReg with
        | Some parentReg -> PersistentDict.forall (fun (reg, _) -> parentReg.CompareTo reg = Includes) d
        | None -> true
        &&
        d |> PersistentDict.forall (fun (reg, (_, subtree)) -> checkInvariantRec (Some (reg :> IRegion<_>)) subtree && d |> PersistentDict.forall (fun (reg', _) -> reg = reg' || reg.CompareTo reg' = Disjoint))

    let checkInvariant tree =
        if not <| checkInvariantRec None tree then
            internalfailf "The invariant of region tree is violated! Tree: {0}" tree
