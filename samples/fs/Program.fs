namespace FsExample

open Nerdbank.MessagePack
open PolyType.ReflectionProvider

module Say =
    type Animal =
        | Cow of name: string * weight: int
        | Horse of name: string * speed: int
        | Dog of name: string * color: string

    type Farm = { Animals: Animal list }

    let farm_union_sample =
        let mutable farm = { Animals = [
            Cow("Bessie", 1500)
            Horse("Spirit", 45)
            Dog("Rex", "Brown")
        ] }

        let serializer = MessagePackSerializer()
        let msgpack = serializer.Serialize(&farm, ReflectionTypeShapeProvider.Default)

        MessagePackSerializer.ConvertToJson(msgpack) |> printfn "Farm as JSON: %s"

        let newFarm = serializer.Deserialize<Farm>(msgpack, ReflectionTypeShapeProvider.Default)

        printfn "Farm animals:"
        newFarm.Animals |> Seq.iter (function
            | Cow(name, weight) -> printfn "Cow: %s, Weight: %d" name weight
            | Horse(name, speed) -> printfn "Horse: %s, Speed: %d" name speed
            | Dog(name, color) -> printfn "Dog: %s, Color: %s" name color)
