@{
    Layout = "post";
    Title = "F# Events, Reactive Programming and Async Workflows";
    Date = "2015-12-03T00:00:30";
    Tags = "fsharp,F#,Rx,Reactive,Async";
    Description = "Working with async events in F#";
}

This is my contribution to the [2015 F# Advent Calendar](https://sergeytihon.wordpress.com/2015/10/25/f-advent-calendar-in-english-2015/). I hope you like it!

The majority of my code-slinging career has focused on full stack web development which hasn't necessitated a lot of complex event-driven programming, unless you include the unholy event model in Web Forms. Even in the cases where I do need to work with events, it's usually abstracted behind a library or framework (shout-out to Knockout). Working with events has become much more relevant now that I'm doing more mobile development. Fortunately F# has some awesome language features that make working with events a lot of fun. Before we jump into events though we're going to look briefly at async programming in F#. Then we'll look at the F# programming model for events. Finally, we'll bring it all together in a comprehensive, if a little contrived, example.

# F# Async Workflows
For whatever reason, F# async workflows have been one of the hardest things for me to grok. The concepts started to click when I read [this](http://dotnet.readthedocs.org/en/latest/async/async-fsharp.html) excellent write-up on the subject.

The goal of asynchronous programming is to start expensive and/or time consuming processes on a separate thread and continue doing work on the main thread until we have to wait for the expensive task to finish. It's kind of like multitasking except that asynchronous programming actually is more efficient. ;)

The following code sample shows a couple of expensive tasks running in sequence. We can't do anything on the main thread until they complete.

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
 
While the above code is contrived, it's not hard to imagine each timer representing an HTTP request to a web API to get data that our app needs before it can be useful. As it stands we have to wait 5 seconds for these tasks to finish before the app startup completes.

Now here's the same code using F# async workflows:

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
    
Thanks to async workflows, we've cut the amount of time it takes to regain control of the main thread nearly in half. But what if we could fire off each request independently and let the async workflow notify us when it's done? That's exactly what the following example does:

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
    
    // Output:
    // "Some other task that isn't blocked"
    // timer output...
    // "Timer 2 finished"
    // "Timer 1 finished"

In the example above, the list of async workflows is broken into two Async.StartWithContinuations calls. Each runs independently somewhere in the background and is given a function to execute when the task is done. Each async call is non-blocking, which means work can continue on the main thread until the expensive task completes.

# F# Events

F# events are straightforward to work with. When working with existing events, you can add a handler to the event using the `Event.Add` method. Referring back to the timer instances from the previous example, we were able to add handlers for the `Elapsed` event with lambdas (anonymous functions) like this:  `timer.Elapsed.Add (fun x -> printfn "%A" x)`. 

Adding a custom event to a type (class) is also simple. Let's create a type that determines whether or not a number is prime and fires an event when called. I chose prime numbers because they come up a lot in programming exercises, and it's interesting see the different ways to determine if a number is prime efficiently.

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

`PrimeFinder` is a type that exposes a single method, `IsPrime`, and an event, `PrimeFound`, that we can add handlers to. `primeFoundEvent` is a local value of type `Event<_>`. Notice that the type passed to `Event` is not defined explicitly. By using the wildcard "`_`" syntax we're telling the compiler to figure out the event type based on its usage. `PrimeFinder` exposes `primeFoundEvent` as `PrimeFound` via the `Publish` method, which returns an object of type `IEvent<PrimeFinder * obj>`. Finally, `IsPrime` calls the `Trigger` method on `primeFoundEvent`. The arguments passed to `Trigger` are the self-identifier for the type, `x`, and a tuple of type `int64 * bool`. Said another way, when we call the `Trigger` method we're passing it the sender and eventArgs. 

Note that when `IsPrime` calls `primeFoundEvent.Trigger` with arguments of type `int64 * bool`, the F# compiler infers `PrimeFound`'s type to be `IEvent<PrimeFinder *(int64 * bool)>`.

One of the neat things about F# events is that you can treat them as event streams (like with Rx programming). An event stream is essentially a collection of events that you can apply function calls like `map`, `reduce`, and `filter` to.

So maybe we only want the event to call our handler when a number is prime. We could easily update our handler to print only when a number is prime, but what if we could separate out the responsibility of handling the event from determining whether or not we *should* handle the event?

The following sample creates an event stream from `PrimeFound` and returns only events whose arguments are prime. The previous handler is then added to the filtered stream. The logic for determining *when* to handle an event is now separate from the logic describing *how* to handle an event.

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

`findPrimesAsync` takes a list of numbers and a function that determines the primality of a number and then applies that function to each number in an async workflow. There are a lot of ways to iterate over the collection and apply the `primeTester` function to each item, but I went with an F# list to see what it looked like being solved recursively. Note that the first time you call the inner recurisve function `findPrimes` you can't use the `do!` binding because you're outside the async workflow. (This is probably obvious to most people but it wasn't to me. Fortunately the compiler will tell you.)

At this point we could declare a mutable binding (boo), fire off Async.Start on `findPrimesAsync` and use `PrimeFinder` to set the mutable binding whenever a prime is found. We could then access the mutable binding later. However, the best way to get comfortable with F# is to write it idiomatically so let's see how we can tackle this functionally, and without mutable state.

We already know we can wire a handler that will get called whenever a prime is found. It would be nice if there were an event whose argument was the most recent prime number to be processed... 
    
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

The value `pFound` only includes events where the `PrimeFound` returned true. The event argument is then mapped to the record type `PrimeTime`. This record type provides extra context about the event, such as when the event fired, where the event originated from, and what the event arguments were.

For the sake of this exercise, we'll check the status of the list processing with a button click. When the button is clicked, we want to see the most recent prime number found in the collection. The following code takes the click event from a button and maps it onto an `Event` of type `PrimeTime`, just like `pFound`!

    let checkStatusButton = new Button(Text = "Check Status")
    let csClicked =
        checkStatusButton.Click 
        |> Event.map (fun _ ->
            {Time = DateTime.Now; Prime = None; ID = "c"})

`pFound` and `csClicked` are both of type `IEvent<PrimeTime>`. This is ultimately the type needed for the final event stream. Getting there is a little bit interesting though.

`csClicked` will **never** have a value for `Prime`. However, `pFound` will always have a value for `Prime`. What we need is a stream containing the latest event emitted by both `pFound` and `csClicked`.

The following snippet uses `Event.merge` to combine the streams `pFound` and `csClicked`. `Event.scan` then reduces the merged stream into a stream of type `IEvent<PrimeTime option * PrimeTime option>`. The first `PrimeTime option` in the tuple is either `Some` or `None` from the `pFound` stream. The second `PrimeTime option` in the tuple is either `Some` or `None` from the `csClicked` stream. Each time either `pFound` or `csClicked` emits an event, `Event.scan` passes the tuple and newest event through an aggregation function. When a `pFound` event passes through the aggregate function a tuple of `(Some(e), None)` is returned. When a `csClicked` event passes through the aggregate function, a tuple of `(p, Some(e))` is returned. Using this pattern, the aggregate function will always return the latest `PrimeFound` event (if any) when `e` is a `Click` event, but will **not** return a `Click` event with when `e` is `PrimeFound` event. Doing this guarantees that when a tuple with a `Click` event is returned, it will have the most recent prime number found if there is one.

    let reduced =
        pFound
        |> Event.merge csClicked
        |> Event.scan (fun (p, c) e ->
            match e.ID with
            |"p" -> (Some(e), None)
            |"c" -> (p, Some(e))
            |_ -> (p, c)) (None, None)

With `reduced` in hand, we can now get the most recent prime number found when the "Check Status" button is clicked. This new event stream should only emit events when the "Check Status" button is clicked. This means we only want events from the `reduced` stream where the second value in the tuple is `Some`. After the stream is filtered to make sure that a "Check Status" click occurred, we map the event stream back into an `IEvent<PrimeTime>` stream. The objective of this map operation is updating the `None` value on the button click event with the `Some` value from the `pFound` event if there is one. If the `pFound` stream hasn't emitted an event we return the unchanged `csClicked` event. If a `pFound` event has occurred, the prime number found from that event is swapped in for the `None` emitted by the `csClicked` event. 

Below is the full code for the filtered, mapped, and refiltered `statusCheck` stream.  

    let statusCheck =
        reduced
        |> Event.filter (fun (p, c) -> c.IsSome)
        |> Event.map (fun (p, c) ->
            match p with
            | Some f -> { c.Value with Prime = f.Prime }
            | None -> c.Value)

With an async workflow defined for our background process and an event stream we can listen to to get the last prime found we're ready to bring it all together. Below is the full code for this example. Try running it in F# interactive. Clicking the "Check Status" button will write either the latest prime found or 0 (zero) to FSI. Clicking "Cancel" will kill the async workflow.

    open System
    open System.Windows.Forms
    
    let isPrime x =
        let rec check i =
            double i > sqrt (double x) || (x % i <> 0L && check (i + 1L))
        match x with
        | y when y < 2L -> false
        | _ -> check 2L
    
    type PrimeFinder() =
        let primeFoundEvent = Event<_>()
        
        member x.PrimeFound = primeFoundEvent.Publish
        member x.IsPrime n =
            primeFoundEvent.Trigger(x, (n, isPrime n))
    
    type PrimeTime =
        {Time:DateTime; Prime:int64 option; ID:string}
        override x.ToString() =
            sprintf "[%s] %d - %s"
                x.ID
                (match x.Prime with | Some(e) -> e | None -> 0L)
                (x.Time.ToLongTimeString())
    
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
        findPrimes numbers
    
    let runForm () =
        let panel = new FlowLayoutPanel()
        panel.Dock <- DockStyle.Fill
        panel.WrapContents <- false
        
        let form = new Form(Width = 400, Height = 300, Visible = true, Text = "Test")
        form.Controls.Add panel
    
        let startButton = new Button(Text = "Start")
        let cancelButton = new Button(Text = "Cancel")
        let checkStatusButton = new Button(Text = "CheckStatus")
    
        let primeFinder = PrimeFinder()
    
        // Transform the event args 
        // from the button.Click
        // and PrimeFound events
        // into uniform structures
        // that can be merged
        let csClicked = 
            checkStatusButton.Click 
            |> Event.map (fun _ -> {Time = DateTime.Now; Prime = None; ID = "c"})
    
        let pFound = 
            primeFinder.PrimeFound
            |> Event.filter (fun (_, e) -> snd e) // only where the number was prime
            |> Event.map (fun (_, e) -> 
                {Time = DateTime.Now; Prime = Some(fst e); ID = "p"})
    
        let reduced =
                pFound
                |> Event.merge csClicked
                |> Event.scan (fun (p, c) e ->
                    match e.ID with
                    |"p" -> (Some(e), None)
                    |"c" -> (p, Some(e))
                    |_ -> (p, c)) (None, None)
    
        let statusCheck =
            reduced
            |> Event.filter (fun (p, c) -> c.IsSome)
            |> Event.map (fun (p, c) ->
                match p with
                | Some f -> { c.Value with Prime = f.Prime }
                | None -> c.Value)
        
        statusCheck.Add (fun x -> printfn "Last prime: %s" <| x.ToString())
        
        let numbers = [1L..100000L]
        startButton.Click.Add (fun _ ->
            printfn "Started"
            Async.StartWithContinuations(
                findPrimesAsync numbers primeFinder.IsPrime,
                (fun _ -> printfn "Done"),
                (fun x -> printfn "Exception occurred: %A" x),
                (fun _ -> printfn "Stopped")
            )
        )
    
        cancelButton.Click.Add (fun _ -> Async.CancelDefaultToken())
    
        panel.Controls.AddRange [|startButton;cancelButton;checkStatusButton|]
            
        Application.Run(form)
    
    runForm()
    
#Conclusion
 
The basics of F# events are simple but powerful. F# events are of type `Event` and implement `IEvent` which in turn implements `IObservable` and `IDelegateEvent`. Because of this, `IEvent` can be mapped, filtered, and more, which allows consumers to add handlers for highly specialized workflows. We can start expensive tasks asynchronously and use events to check on the status of the work.

#Resources
1. http://fsharpforfunandprofit.com/posts/concurrency-async-and-parallel/
2. http://dotnet.readthedocs.org/en/latest/async/async-fsharp.html
3. http://fsharpforfunandprofit.com/posts/concurrency-reactive/