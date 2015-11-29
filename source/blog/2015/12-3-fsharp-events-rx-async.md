@{
    Layout = "post";
    Title = "F# Events, Reactive Programming and Async Workflows";
    Date = "2015-12-03T00:00:30";
    Tags = "fsharp,F#,Rx,Reactive,Async";
    Description = "Working with async events in F#";
}

This is my contribution to the [2015 F# Advent Calendar](https://sergeytihon.wordpress.com/2015/10/25/f-advent-calendar-in-english-2015/). I hope you like it!

The majority of my code-slinging career has focused on full stack web development which hasn't necessitated a lot of complex event-driven programming, unless you include the unholy event model in Web Forms. Even in the cases where I do need to work with events, it's usually abstracted behind a library or framework (shout out to Knockout). Since I've been doing more mobile development working with events has become more relevant which inspired me to dig into them and write this post. First we'll look very briefly at async programming in F# and then look at the F# programming model for events. Then we'll bring it all together in an applicable, if a little contrived, example.

# F# Async Workflows
For whatever reason F# async workflows have been one of the hardest things for me to grok. The concepts started to click when I read [this](http://dotnet.readthedocs.org/en/latest/async/async-fsharp.html) excellent write-up on the subject.

The goal of asynchronous programming is to start expensive and/or time consuming processes on a separate thread and continue doing work on the main thread until we have to wait for the expensive task to finish. It's kind of like multitasking except that asynchronous programming actually is more efficient. ;)

The following code sample shows a couple of "expensive" tasks running in sequence. We can't do anything on the main thread until they complete.

    open System
    
    let timer1 = new Timers.Timer(500.)
    let timer2 = new Timers.Timer(250.)
    
    timer1.Elapsed.Add
        (fun x ->
            printfn "Timer 1: %d:%d" x.SignalTime.Second x.SignalTime.Millisecond)
    timer2.Elapsed.Add
        (fun x ->
            printfn "Timer 2: %d:%d" x.SignalTime.Second x.SignalTime.Millisecond)
    
    let runTimer (t : System.Timers.Timer) (s : int) =
        t.Start()
        System.Threading.Thread.Sleep s
        t.Stop()
    
    //Run timers sequentially
    runTimer timer1 2500
    runTimer timer2 2500
 
While the above code is contrived it's not hard to imagine each timer representing an HTTP request to a web API to get data that our app needs before it can be useful. As it stands we have to wait 5 seconds for these tasks to finish before the app startup completes.

Now here's the same code using F# Async Workflows:

    open System
    
    let timer1 = new Timers.Timer(500.)
    let timer2 = new Timers.Timer(250.)
    
    timer1.Elapsed.Add
        (fun x ->
            printfn "Timer 1: %d:%d" x.SignalTime.Second x.SignalTime.Millisecond)
    timer2.Elapsed.Add
        (fun x ->
            printfn "Timer 2: %d:%d" x.SignalTime.Second x.SignalTime.Millisecond)
    
    // new stuff starts here
    let runTimerAsync (t : System.Timers.Timer) s =
        async {
            t.Start()
            // Async workflow needs to do something (basically block here)
            do! Async.Sleep s
            t.Stop()        
        }
    
    // Run timers in parallel
    Async.Parallel [(runTimerAsync timer1 5000); (runTimerAsync timer2 5000)]
    |> Async.RunSynchronously
    
Thanks to Async Workflows we've cut the amount of time it takes to regain control of the main thread nearly in half. But what if we could fire off each "request" independently and let the async workflow notify us when it's done? That's exactly what the following example does:

    Async.StartWithContinuations(
        runTimerAsync timer1 2500,
        (fun _ -> printfn "Timer 1 finished"),
        (fun _ -> printfn "Timer 1 threw an exception"),
        (fun _ -> printfn "Cancelled Timer 1"))

    Async.StartWithContinuations(
        runTimerAsync timer2 1500,
        (fun _ -> printfn "Timer 2 finished"),
        (fun _ -> printfn "Timer 2 threw an exception"),
        (fun _ -> printfn "Cancelled Timer 2"))

    printfn "Some other task that isn't blocked"
    // the output will be something like:
    // "Some other task that isn't blocked"
    // timer output...
    // "Timer 2 finished"
    // "Timer 1 finished"

In the example above the list of async workflows is broken into two Async.StartWithContinuations calls. Each runs indepdently somewhere in the background and is given a function to execute when the task is done. Each async call is non-blocking which means work can continue on the main thread until the expensive task completes.

# F# Events

F# events are straightforward to work with. When working with existing events you can add a handler to the event using the `Event.Add` method. Referring back to the timer instances from the previous example, we were ble to add handlers for the Elapsed event with lambdas (anonymous functions) like this:  `timer.Elapsed.Add (fun x -> printfn "%A" x)`. 

Adding a custom event to a type (class) is also simple. Let's create a type that can determine whether or not a number is prime, and fires an event when called. *I chose prime numbers because they come up a lot in programming exercises, and it's interesting see the different ways to efficiently determine if a number is prime.*

    // Type for testing primality of numbers
    // Include custom event that triggers when helper is called.
    type PrimeFinder() =
        let primeFoundEvent = Event<_>()
    
        member x.PrimeFound = primeFoundEvent.Publish
        member x.IsPrime n =
            primeFoundEvent.Trigger(x, (n, isPrime n))
    
    let primeFinder = PrimeFinder()
    primeFinder.PrimeFound.Add (fun (_, e) ->
        match e with
        | n, true -> printfn "%d is prime" n
        | n, false -> printfn "%d is not prime" n)
    
    primeFinder.IsPrime 3L // prints "3 is prime"
    // PrimeFound event should not fire
    primeFinder.IsPrime 4L // prints "4 is not prime"
    
Inside of the `PrimeFinder` type the event `primeFoundEvent` is defined. It's correpsonding type isn't defined explicitly, notice the wildcard "`_`" passed as the type to `Event<_>`, which means the compiler will figure out the event's type based on how it's used. The member `PrimeFound` is of type `IEvent<PrimeFinder * obj>`, and is what callers will wire handlers to.

The member `IsPrime` is responsible for triggering the `primeFoundEvent`. The handler wired up to `PrimeFound` is called with a tuple, `Sender * Args`, passed in.

Note that `IEvent<PrimeFinder * obj>` changes to `IEvent<PrimeFinder * (int64 * bool)>` after `primeFoundEvent.Trigger` is called with an event argument of type `int64 * bool`. Once `Trigger` is called with an argument the F# compiler can infer the type of `primeFoundEvent`.

One of the neat things about F# events though is that you can treat them as event streams (like with Rx programming). An event stream is essentially a collection of events that you can apply function calls like `map`, `reduce`, and `filter` to.

So maybe we only want the event to call our handler when a number is prime? We could easily update our handler to only print when a number is prime, but what if we could separate out the responsibility of handling the event from determining whether or not we *should* handle the event?

The following sample take the PrimeFound event stream and returns only events whose argument is prime. The same handler used previously is then added to the filtered stream. The logic for determining *when* to handle an event is now separate from the logic describing *how* to handle an event.

    let primeFinder = PrimeFinder()
    
    primeFinder.PrimeFound
        |> Event.filter (fun (_, e) -> snd e) // only where the number was prime
        |> Event.add (fun (_, e) ->
            match e with
            | n, true -> printfn "%d is prime" n
            | n, false -> printfn "%d is not prime" n)
    
    // PrimeFound handler should fire.
    primeFinder.IsPrime 3L
    // PrimeFound handler should not fire
    primeFinder.IsPrime 4L
    
What's really nice about this approach is that we can change our filtering logic at any time without touching the event handler. `PrimeFound` can be treated as a stream by virtue of being of type IEvent thanks to `primeFoundEvent.Publish`.

# More Events
Now let's say we want to start a long running process in the background and we want to be able to check on the status of it as we go. The code for the background process might look something like:

    let findPrimesAsync numbers primeTester = 
        let rec findPrimes n = async {
            match n with
            | h::t ->
                do! Async.SwitchToNewThread()
                primeTester h
                do! Async.SwitchToThreadPool()
                do! findPrimes t
            | [] -> ()
            
        }
        findPrimes number

`findPrimesAsync` takes an list of numbers and a function that determines the "primality" of a number and then applies that function to each number in an async workflow. There are a lot of ways to iterate over the collection and apply the `primeTester` function to each item, but I went with an F# list to see what it looked like being solved recursively. Note that the first time you call the inner recurisve function `findPrimes` you can't use the `do!` binding because you're outside the async workflow (this is probably obvious to most people but it wan't to me... fortunately the compiler will tell you).

At this point we could declare a mutable binding (boo), fire off Async.Start on `findPrimesAsync` and use `PrimeFinder` to set the mutable binding whenever a prime is find. We could then access the mutable binding later. However, the best way to get comfortable with F# is to write it idomatically so let's see how we can tackle this functionally, and without mutable state.

We already know we can wire a handler that will get called whenever a prime is found. It would be nice if there was an event whose argument was the most recent prime number to be processed... 
    
    // Record type give our event args some structure
    type PrimeTime =
        {Time:DateTime; Prime:int64 option; ID:string}
        override x.ToString() =
            sprintf "%d - %s" 
                (match x.Prime with | Some(e) -> e | None -> 0L)
                (x.Time.ToLongTimeString())
                
    let primeFinder = PrimeFinder()
    let pFound = 
        primeFinder.PrimeFound
        |> Event.filter (fun (_, e) -> snd e) // only where the number was prime
        |> Event.map (fun (_, e) -> 
            {Time = DateTime.Now; Prime = Some(fst e); ID = "p"})

    let reduced = 
        pFound
        |> Event.scan (fun l c -> c :: l) [] 
        |> Event.map (fun x -> x.Head)

The interesting bit here is the reduced value which is derived via `Event.scan` to build a list of events. `Events.scan` is like reduce in that it takes as an argument a function that accepts an accumulator and the current item in the collection (or stream in this case). Each time `PrimeFound` fires `Event.scan` creates a new list with the new event as the head and all the previous events as the tail. We also created a record type `PrimeTime` to give the event arguments some structure.

For the sake of this exercise we'll check the status of the list processing with a button click. When the button is clicked we want to see the most recent prime number found in the collection. The following code takes the click event from a button and maps it into an `Event` of type `PrimeTime` just like `reduced`!

    let checkStatusButton = new Button(Text = "CheckStatus")
    let checkStatusClicked =
        checkStatusButton.Click 
        |> Event.map (fun _ ->
            {Time = DateTime.Now; Prime = None; ID = "c"})

`reduced` and `checkStatusClicked` are both of type `IEvent<PrimeTime>`. We can now combine these event streams together. Events will be added to this stream whenever the "Check Status" button is clicked, or a new prime number is found. After the two streams are merged it would be nice if events in the stream could be grouped together so that when the "Check status" click event fires it could be paired with the latest `PrimeFound` event. For each pair of events that contains both a `Click` and a `PrimeFound` we should extract the revelant data, filter out what we don't need and return a stream that will contain the latest `PrimeTime` event when the "Check Status" button is clicked. The following code does exactly that:

    let statusCheck =
        reduced
        |> Event.merge csClicked
        |> Event.pairwise
        |> Event.map (fun (f, s) -> { s with Prime = f.Prime})
        |> Event.filter (fun pt -> pt.ID = "c")
        |> Event.add (fun x -> printfn "Largest prime: %s" <| x.ToString())

The `statusCheck` event stream is created by first merging the `reduced` stream with the `checkStatusClicked` stream. Events in this merged stream will be of type `PrimeTime` and will either have an `ID` of "c" or "p" depending on from where the event originated.

After the two streams are combined a stream of pairs can be created by passing the merged stream to `Event.pairwise`. The "pairwised" stream contains IEvents of the type `PrimeTime * PrimeTime`. Each `PrimeTime` instance is from either the `PrimeFound` event or the `Click` event of the merged stream. 

`Event.map` maps `IEvent<PrimeTime * PrimeTime>` to `IEvent<PrimeTime>` by copying the 2nd `PrimeTime` instance and replacing it's `Prime` member with the value from the 1st `PrimeTime` instance. This step is important because when we mapped the original `Click` event to an event stream of `IEvent<PrimeTime>` the value for `Prime` was set to `None`. While it's entirely possible that all of the numbers being processed are composite (e.g. not prime), we already know that the values coming from the `ClickEvent` are never prime so it's safe to replace them with what's coming back from the `reduced` version of the `PrimeFound` stream. 

Finally the Events generated by the `PrimeFound` event are filtered out. Because of the mapping done in the previous step the `Click` events in the stream have a value for `PrimeTime.Prime` that we can pass to the UI (in this case the console).

#Conclusion
 
The basics of F# events are simple, but also extremely powerful. F# events are of type Event and implement IEvent which implements IObservable and IDelegateEvent and because of this can be mapped, filtered, and reduced allowing callers to add handlers to customized scenarios as needed. We can start expensive tasks asynchronsously and use events to check on the status of the work.

#Resources
1. http://fsharpforfunandprofit.com/posts/concurrency-async-and-parallel/
2. http://dotnet.readthedocs.org/en/latest/async/async-fsharp.html
3. http://fsharpforfunandprofit.com/posts/concurrency-reactive/