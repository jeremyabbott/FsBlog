open System

type Gift =
| Wants of string
| Coal

type Person = { Name: string; SharedToys: bool; IsANastyWoman: bool; Wants: string }

let giftIf behavior person =
    let gift = if behavior then Wants person.Wants else Coal
    person.Name, gift
let sharedToys person = giftIf person.SharedToys person
let wasANastyWoman person = giftIf person.IsANastyWoman person

let nameNotNullOrEmpty person successF failF =
    if String.IsNullOrEmpty(person.Name) then failF person
    else successF person

let determineGift person =
    match sharedToys person with
    | n, Coal -> n, Coal // fail fast
    | _, _ ->
        match wasANastyWoman person with
        | n, Coal -> n, Coal // fail again
        | n, Wants s -> n, Wants s 

let naughtyOrNice person =
    nameNotNullOrEmpty
        person
        determineGift
        (fun p -> failwith "Person did not have a valid name")

naughtyOrNice { Name = "Jeremy"; IsANastyWoman = true; SharedToys = true; Wants = "For 2016 not to have happenend" }

naughtyOrNice { Name = "Donald"; IsANastyWoman = false; SharedToys = false; Wants = "To maintain systemic discrimination and privilege." }

type Person' = { Name: string; SharedToys: bool; IsANastyWoman: bool; Gift: Gift }

let giftIf' behavior (person: Person') =
    if behavior then person
    else { person with Gift = Coal }

let sharedToys' person = giftIf' person.SharedToys person

let wasANastyWoman' person = giftIf' person.IsANastyWoman person

let nameNotNullOrEmpty' (person: Person') successF failF =
    if String.IsNullOrEmpty(person.Name) then failF person
    else successF person

let determineGift' = sharedToys' >> wasANastyWoman'

let naughtyOrNice' person =
    
     nameNotNullOrEmpty'
        person
        determineGift'
        (fun p -> failwith "Person did not have a valid name")

naughtyOrNice' { Name = "Donald"; IsANastyWoman = false; SharedToys = false; Gift = Wants "To maintain systemic discrimination and privilege." }

type Result<'t> =
| Nice of 't
| Naughty of 't

let giftIf'' behavior (person: Person') =
    if behavior then Nice person
    else Naughty { person with Gift = Coal }

let sharedToys'' person = giftIf'' person.SharedToys person

let wasANastyWoman'' person = giftIf'' person.IsANastyWoman person

let determineGift'' person = 
    match sharedToys'' person with
    | Naughty p -> Naughty p
    | Nice p ->
        match wasANastyWoman'' person with
        | Naughty p -> Naughty p
        | Nice p -> Nice p

determineGift'' { Name = "Donald"; IsANastyWoman = false; SharedToys = false; Gift = Wants "To maintain systemic discrimination and privilege." }

// function that accepts a (person -> Result) and a Result
let failFast nOrFF nOrF =
    match nOrF with
    | Naughty p -> Naughty p
    | Nice p -> nOrFF p

let determineGiftRedux person =
    person
    |> sharedToys''
    |> failFast wasANastyWoman''
    
determineGiftRedux { Name = "Donald"; IsANastyWoman = false; SharedToys = false; Gift = Wants "To maintain systemic discrimination and privilege." }

let (>>=) nOrF nOrFF = failFast nOrFF nOrF

let determineGiftRedux' person =
    person
    |> sharedToys'' 
    >>= wasANastyWoman''

determineGiftRedux' { Name = "Donald"; IsANastyWoman = false; SharedToys = false; Gift = Wants "To maintain systemic discrimination and privilege." }

let map f r =
    match r with
    | Nice n -> Nice (f n) 
    | Naughty n -> Naughty n
let needsPokemon p =
    match p with
    | { Gift = Wants "For 2016 not to have happenend" } -> { p with Gift = Wants "Pokémon Sun/Moon" }
    | _ -> p
let mappedNeedsPokemon = map needsPokemon

needsPokemon { Name = "Jeremy"; IsANastyWoman = true; SharedToys = true; Gift = Wants "For 2016 not to have happenend" }
let determineGiftRedux'' person =
    person
    |> sharedToys'' 
    >>= wasANastyWoman''
    |> map needsPokemon

determineGiftRedux'' { Name = "Jeremy"; IsANastyWoman = true; SharedToys = true; Gift = Wants "For 2016 not to have happenend" }

let optionalString s =
    if System.String.IsNullOrEmpty(s) then None
    else Some s

let withCharacters s =
    if System.String.IsNullOrWhiteSpace(s) then None
    else Some s
let empty = ""
let notEmpty = "Hello fellow nasty woman"
let whitespace = " "
let oEmpty = optionalString empty
let oNotEmpty = optionalString notEmpty
let oWhiteSpace = withCharacters whitespace

let shouldBeNone =
    whitespace
    |> optionalString
    |> Option.bind withCharacters

let shouldBeSome =
    notEmpty
    |> optionalString
    |> Option.bind withCharacters

// let sharedToys child =
//     if child.SharedToys then Nice child
//     else Naughty child

// let isANastyWoman child =
//     if child.IsANastyWoman then Nice child
//     else Naughty child

// let naughtyOrNice child =
//     let rw = child |> isANastyWoman
//     let rst = child |> sharedToys
//     match rw, rst with
//     | Nice rw, Nice rst -> Nice child
//     | _, _ -> Naughty child

