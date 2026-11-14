namespace ShippedFSharpUnions

type Animal =
    | Cow of name: string * weight: int
    | Horse of name: string * speed: int
    | Dog of name: string * color: string
    | Cat

type Pet =
    | Cat of name: string
    | Dog of name: string

type Household = { Pet: Pet }
