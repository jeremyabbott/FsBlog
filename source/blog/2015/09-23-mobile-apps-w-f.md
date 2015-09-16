@{
    Layout = "post";
    Title = "Mobile Apps w/ F#";
    Date = "2015-09-23T09:25:51";
    Tags = "F#,mobile,xamarin,type providers";
    Description = "Quick run down on my experience writing a mobile app with Xamarin and F#.";
}

## Background 
I'm in the process of writing an iOS app for the [North Louisiana Gay & Lesbian Film Festival presented by PACE](http://nlglff.org). It *should* already be shipped and "done", but that's a story for another blog post.

I knew I was going to write the app using Xamarin. At first I considered using Xamarin.Forms, but since I don't have an Android device to test with I decided to use Xamarin.iOS so that I could write the app in F#. This isn't to say that you can't write a Xamarin.Forms app using F#, but the tooling isn't quite there yet so I decided to go with what Xamarin officially supports. 

I chose Xamarin and .NET over Objective-C because my experience with Objective-C is limited to an 8 hour course I took on it before Automatic Reference Counting was a thing. Basically I didn't want to deal with the overhead of an unfamiliar framework, *and* an unfamiliar language, *and* never having written a native mobile app before in general.

What follows are some notes on F# features that really helped me be productive throughout this project. 

## Type Providers
One of the key productivity features of F# is [Type Providers](https://msdn.microsoft.com/en-us/library/hh156509.aspx). Type providers can take a data source and generate code based on that data. In my case the data I cared about was JSON returned from a Web API.

To get up and running with the JSON Provider you need to add the [Fsharp.Data](http://fsharp.github.io/FSharp.Data/) NuGet package to your project. In Visual Studio you'll get a warning after the package is successfully installed. The warning serves to make sure you understand that a type provider is a code generation tool and that it is going to execute and generate code based on a data source that it has no control over. As scary as that sounds, it's really okay, so click the Enable button.


![Visual Studio Type Provider Security Warning](~/images/typeprovidersecurity.png "Visual Studio Type Provider Security Warning")  


Now, using the GitHub API as an example, I can write something like the following...

    open FSharp.Data
        
    type Gist = JsonProvider<"https://api.github.com/gists", SampleIsList=true>
    
    [<EntryPoint>]
    let main argv = 
        let gists = Gist.GetSamples()
        printfn "%A" (gists |> Array.map (fun g -> g.Id) |> Array.reduce(fun a e -> sprintf "%s\n%s" a e))
        0 // return an integer exit code

Thanks to the type provider I can now code against a strongly typed object that I didn't have to write. The JsonProvider uses the structure of the JSON response to create the type. Not only does the type provider give me static types, but I can also write queries against any end point whose structure matches the sample passed to the provider. This means I get code generation and simple HTTP operations!

One caveat to using Type Providers against Web APIs is that they do not support OAuth (yet).

## Custom Operators and Auto Layout

F# gives you the ability to create custom operators and/or overload existing operators. This can be used in very interesting ways. Frank A. Krueger ([@@praeclarum](https://twitter.com/praeclarum)), [has an awesome gist on GitHub](https://gist.github.com/praeclarum/c9e2d1a0f1089cb4025a) that allows you to write [auto layout constraints](https://developer.apple.com/library/prerelease/ios/documentation/UserExperience/Conceptual/AutolayoutPG/AnatomyofaConstraint.html#//apple_ref/doc/uid/TP40010853-CH9-SW1) in a very terse yet easy to understand fashion. For reference an auto layout constraint is a linear equation of the form y = mx + b where y is the attribte of one view, m is the attribute of another view, x is a multiplier, and b is a constant.

A really common use case (at least for me) was needing to center a subview inside its superview. In Auto Layout terms this amounts to 2 equations:

1. subview.CenterX = superview.CenterX * 1 + 0
  - y = subview.CenterX
  - m = superview.CenterX
  - x = 1
  - b = 0
2. subview.CenterY - superview.CenterY * 1 + 0 
  - y = subview.CenterY
  - m = superview.CenterY
  - x = 1
  - b = 0
  
The two equations state that we want the subview's center X and Y coordinates to be equal to the superview's center X and Y coordinates.

Without using the custom operators provided by @@praeclarum, the code is pretty verbose:

    superview.AddConstraints [|NSLayoutConstraint.Create(subview, NSLayoutAttribute.CenterX, NSLayoutRelation.Equal, superview, NSLayoutAttribute.Equal, nfloat 1., nfloat 0.0)
                               NSLayoutConstraint.Create(subview, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, superview, NSLayoutAttribute.Equal, nfloat 1., nfloat 0.0)|]
    
Using @@praeclarum's Auto Layout code:

    superview.AddConstraints [|subview.LayoutCenterX == superview.LayoutCenterX
                               subview.LayoutCenterY == superview.LayoutCenterY|]
                               
Each version has the same number of lines of code, but in the second version its MUCH easier to reason about the relationship between the subview and superview.

## Immutability and Memoization

[From Wikipedia](https://en.wikipedia.org/wiki/Memoization): In computing, memoization is an optimization technique used primarily to speed up computer programs by storing the results of expensive function calls and returning the cached result when the same inputs occur again.

Said another way memoization is a form of  caching. We store the result of a function call so that if the function is called again we can return the result without doing the calculation. Immutability is important to this because if your function happens to depend on the state of a value outside the scope of the function the memoized result becomes invalid if/when that external value changes. Fortunately for us F# values are immutable by default so we can be confident that if we memoize something, the memoized result won't become stale. *Obviously if the calculation the function is performing is derived from an external source, like a Web API call, the "freshness" of the memoized result is more subjective.*

    let memoize f =
        let cache = ref Map.empty
        fun x ->
            match (!cache).TryFind(x) with
            | Some res -> res
            | None ->
                let res = f x
                cache := (!cache).Add(x,res)
                res

And usage might look something like this:

    let getGists =
        memoize (fun () -> Gist.Load("https://api.github.com/gists"))
        
Memoize is of the type ('a -> 'b) -> ('a -> 'b). Said another way: memoize accepts a function and returns a new version of that function that memoizes its inputs and outputs to optimize subsequent calls. In our case we're passing it a function that accepts unit and returns a list of Gists. It's worth nothing that if you're memoizing a function that accepts unit, then the key for the Map cache value is null (TIL that null is a valid key for a Map).

**[Source for memoize function.](http://blogs.msdn.com/b/dsyme/archive/2007/05/31/a-sample-of-the-memoization-pattern-in-f.aspx)**

## Type Augmentations

F# type augmentations are similar to extension methods, but they can extend to properties, events, and static members. The Auto Layout code sample referenced above adds several read-only properties to the Xamarin.iOS UIView type along with a couple of properties for setting the priority for vertical and horizontal hugging. We can now get and set those properties anytime we're working with an instance of UIView. We didn't have to create a subclass that inherited from UIView to get this functionality.

## Summary
One thing all of the examples above really capture is how terse yet expressive F# can be. More than anything else I have been able to be productive on this project because of how little code I have to write. So far the hardest part of this little iOS project has been the iOS part. Xamarin and F# have been huge time savers.