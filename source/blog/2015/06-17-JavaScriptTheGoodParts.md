@{
    Layout = "post";
    Title = "JavaScript: The Good Parts: The Good Parts";
    Date = "2015-06-17T07:34:00";
    Tags = "JavaScript, Beginner";
    Description = "Takeaways from finally reading Douglas Crockford's excellent <em>JavaScript: The Good Parts</em>";
}

I've been meaning to read _JavaScript: The Good Parts_ by Douglas Crockford for over a year. It's been sitting on my stack of shame (stack of programming books I have every intention of reading) collecting dust for MONTHS. It's an understatement to say that I'd been doing myself a disservice to not have read it sooner. Crockford's book provides a concise yet complete assessment of all of JavaScript's frustrating nuances.

 JavaScript is a hastily constructed language that was shipped before it had time to be polished. The web runs on a half-baked language! However, by using a subset of JavaScript, and following a few rules, we can make writing and maintaining JavaScript manageable. The book is a quick and enjoyable read in no small part  because Crockford is not afraid to point out the terrible features of the language. In a general sense JavaScript can cause a lot of heartache because its familiar syntax coaxes you into believing it behaves like the languages that syntax came from. This leads to code being written based on incorrect assumptions.

What follows isn't so much a review of the book, but a brief overview of a JavaScript nuances the book describes really well. Specifically, these are items that I see developers new to the language really struggle with. The range of topics covered by the book is extensive, and every chapter is worth reading.

## Variable Hoisting
While the syntax of the language implies otherwise, variable scope does not change when code is wrapped in a block with curly braces. Instead a new scope is created when a function is defined. Variables defined within a function are only visible within that function. More importantly though is that variables defined within a function are visible everywhere within the function. This is called _variable hoisting_. 

Basic variable hoisting:

    function hoistVariable() {
        console.log(foo); // undefined
        var foo = "foo";
        console.log(foo); // "foo"
        console.log(bar); // throws an exception because bar is never declared.
    }

    hoistVariable();

Hoisted variable within a block within a function:

    function hoistVariable() {
        console.log(foo); // undefined

        if (true) { 
            var foo = "foo";
            console.log(foo); // "foo"
        }
        // The if block did not create a new scope. Foo was still hoisted to the beginning of the function
    }

    hoistVariable();

Variable declared twice within a function. Once at the beginning of the function, and again within an if statement:

    function hoistVariable() {
        var foo = "foo";
        console.log(foo); "foo"

        if (true) { 
            var foo = "also foo"; // this is really bad.
            console.log(foo); // "also foo"
        }
        console.log(foo); // "also foo"
    }

    hoistVariable();

Because of variable hoisting Crockford recommends declaring all variables at the beginning of the function. Doing this helps prevent using variables before they're assigned. Variable hoisting is a great example of how JavaScript's familiar syntax can lead to programmatic errors that are either easier to catch or impossible to make in other languages.

## The _this_ Keyword
The _this_ keyword is a source of great pain in JavaScript. The value of _this_ changes significantly based on how or when it is used.

_JavaScript: The Good Parts_ describes the _this_ keyword as an additional parameter that gets passed in with every function.  The value of _this_ depends on how a function is invoked. A JavaScript "bad part" is that the value of this is not necessarily bound to the same scope as the function it's used in.

Changing a globally declared variable within a function: 

    var foo = "foo";
    console.log(foo); // foo is bound to the global object.

    function doSomething() {
        this.foo = "foo again";
        console.log(this.foo);
    }

    if (this.doSomething !== null){
        this.doSomething(); // "foo again"
        console.log(foo) // "foo again"
    }

The example above demonstrates that a function defined in the global scope is bound to the global object. The value of _this_ did not change when used inside of a function. _this_ was still bound to the global object scope.

So when does the context of _this_ change? There are two cases:
1. When _this_ is passed in as an argument to the "apply" function, and 
2. When a new object is created using the _new_ operator.

In JavaScript functions are objects and just like any other object they can have methods. One of those methods is "apply."

###Controlling the context of _this_ with apply

Using apply we can call a function and tell it what _this_ is:


    function printDescription() {
        console.log(this.description);
    }

    var thing1 = { description: "thing 1"};

    // when null is passed in _this_ is bound to the global object
    printDescription.apply(null); // prints undefined because description is not a property on the global object.

    // pass in thing1 as the value for _this_
    printDescription.apply(thing1); // prints "thing 1" because _this_ has a description property

The "apply" method allows us to invoke a function, explicitly define what _this_ is bound to within that function, and pass in a list of arguments. 

###Controlling the context of _this_ with the _new_ operator

As mentioned above, the context of _this_ does not necessarily change when variable scope changes. The value of _this_ within a function can be, and often is, the same as _this_ in the global scope.

Using the _new_ operator when invoking a function does change the context of _this_. Functions that are invoked with _new_ are called constructor functions and by convention start with an upper-case letter (PascalCase). **Invoking a constructor function without the _new_ operator can cause all sorts of nasty things to happen!**

Example of using constructor functions with and without _new_:

    var foo = "global foo variable";
    console.log(foo); // "global foo variable"

    // PascalCase indicating constructor function
    function Thing() {
        this.foo = "thing's foo variable";
    }

    // create a new Thing object using the _new_ operator
    var someThing = new Thing();
    console.log(someThing.foo); // "thing's foo variable"

    // invoked without new operator
    var someOtherThing = Thing();
    console.log(foo); // "thing's foo variable"
    console.log(someOtherThing.foo); // throws exception because someOtherThing is undefined. 

In the preceding example, the value of a global variable was accidentally changed when a constructor function was invoked without the _new_ operator.

The _new_ operator changes the behavior of the _return_ keyword too. All functions return something. When that something is not explicitly stated, a function returns undefined. When paired with _new_, _return_ returns _this_. That is why, in the previous example, "someOtherThing" was undefined.

###One Other _this_ "Bad Part"

The value of _this_ takes on the context of the global object when it is not the property of an object. This also has consequences:

    var foo = "global foo";

    function Thing() {
        this.foo = "foo property of Thing";
        this.printDescription = function() {
            // printDescription is a method on Thing
            // _this_ is bound to the Thing's context.

            function helperFunction() {
                // helperFunction is not a method on Thing
                // _this_ is bound to the global object's context.

                console.log("value of this.foo from helperFunction: " + this.foo); // "global foo"
            }
            helperFunction();
            console.log("value of this.foo from within printDescription: " + this.foo); // "foo property of Thing"
        }
    }

    var someThing = new Thing();
    someThing.printDescription();

The most common way of maintaining access to an object's context within that object is to assign _this_ to a variable:

    var foo = "global foo";

    function Thing() {
        var self = this;
        self.foo = "foo property of Thing";
        
        self.printDescription = function() {
            // printDescription is a method on Thing
            // _this_ is bound to the context of Thing.

            function helperFunction() {
                // helperFunction is not a method on Thing
                // _this_ is bound to the global object's context.

                console.log("value of this.foo from helperFunction: " + this.foo); // "global foo"
                console.log("value of this.foo from helperFunction (accessed using \"self\" variable); " + self.foo); // "foo property of Thing"
            }
            
            helperFunction();

            // note that self and _this_ are the same at this point!
            console.log("value of this.foo from within printDescription: " + this.foo); // "foo property of Thing"
        }
    }

    var someThing = new Thing();
    someThing.printDescription();

##In Summary
1. JavaScript's familiar syntax can cause us to make incorrect assumptions about language behavior.
2. A variable defined anywhere in a function is visible everywhere within that function.
3. All variables used within a function should be declared at the beginning of a function to make sure they're not accidentally accessed before they're assigned a value.
4. The _this_ parameter's value is generally scoped to the global object.
    * Using the "apply" method allows you to explicitly set the value of _this_.
    * Using the _new_ operator binds _this_ to the function the operator is applied to (constructor function).
5. Functions defined within methods have their _this_ parameter bound to the global scope.
    * Methods are functions that are properties of an object.
6. The behavior of _return_ is also changed when the new operator is applied to it.
    * Functions return undefined when _return_ is not explicitly used.
    * Functions return _this_ when the _new_ operator is applied to it returning an object with all the properties/methods of _this_.
7. Using a constuctor function without _new_ can cause global variables to be overwritten with the values of variables assigned within the constructor function.

Everyone writing JavaScript should read this book. It has a wealth of information for developers new to the language, and it provides some interesting background and historal context that JavaScript veterans will appreciate. [Go buy it](http://www.amazon.com/JavaScript-Good-Parts-Douglas-Crockford/dp/0596517742)!

*This is the first post I've ever written and shared on software development. I actually wrote it in February, but I never shared it outside of a couple trusted friends.*