@{
    Layout = "post";
    Title = "Hosting Suave as a Sub App in IIS & Asking for Help";
    Date = "2016-02-13T08:14:38";
    Tags = "F#,fsharp,suave";
    Description = "A quick explanation on how to host a Suave as an IIS sub-application, with a gentle reminder that it's okay to ask for help.";
}

While experimenting with [Suave](https://suave.io/) for a side project I wanted to deploy it locally to IIS to see how [HttpPlatformHandler](http://www.iis.net/downloads/microsoft/httpplatformhandler) ([more info](http://azure.microsoft.com/blog/2015/02/04/announcing-the-release-of-the-httpplatformhandler-module-for-iis-8/)) worked. Easy enough, right?! Actually yes, but it took me a long time (several evenings) to get it right. 

First let's take a look at how to host a Suave app as a sub app in IIS. The requirements are pretty straightforward. In fact, you can follow [Scott Hanselman's post about deploying Suave to Azure](http://www.hanselman.com/blog/RunningSuaveioAndFWithFAKEInAzureWebAppsWithGitAndTheDeployButton.aspx) and get most of the way there. Before you do anything else [download](http://www.iis.net/downloads/microsoft/httpplatformhandler) and install version 1.2 of the HttpPlatformHandler IIS module.

After you've installed the HttpPlatformHandler module you can verify that it's installed in IIS by checking in IIS: 

![IIS Home Screen](~/images/iishome.png "IIS Home")

Double click on the "Modules" icon in the "IIS" section.

![IIS Modules Screen](~/images/iismodules.png "IIS Modules")

<br />
<br />

Next let's set up a simple F# script that will run a Suave application. A simple way to do this is to create an F# tutorial project in Visual Studio. From the "New Project" dialogue select "Visual F#" and then "Tutorial".

![Visual Studio New Project Dialogue](~/images/vsFsharpTutorialDialogue.png "Visual Studio New Project Dialogue")

<br />
<br />

The tutorial template simplifies setup because it gives you an F# script file by default, but also makes it really easy to add the NuGet packages we're going to need. Go ahead and add packages for `Suave` and `FAKE`.

When you're done your project should look similar to this in Visual Studio's solution explorer:

![Visual Studio Solution Explorer](~/images/suavesolutionexplorer.png "Visual Studio Solution Explorer")

<br />
<br />

Aside: if we wanted to be more idiomatically F# we'd use [Paket](https://fsprojects.github.io/Paket/) instead of NuGet, but let's not add too many new ideas at once.

Now that we have our project setup let's write some code. Replace the code in `Tutorial.fsx` with the following:

    #I "./packages/Suave.1.1.1/lib/net40"
    #I "./packages/FAKE.4.20.0/tools"
    #r "Suave.dll"
    #r "FakeLib.dll"

    open System
    open System.Net
    open Fake
    open Suave
    open Suave.Filters
    open Suave.Operators
    open Suave.Successful

    Environment.CurrentDirectory <- __SOURCE_DIRECTORY__

    let app =
        choose [
                path "/hello" >=> OK "Hello!"
                path "/goodbye" >=> OK "Good bye!"]

    let port = getBuildParamOrDefault "port" "8083" |> uint16

    let config =
        { defaultConfig with
            bindings = [HttpBinding.mk HTTP IPAddress.Loopback port]
        }

    startWebServer config app
    
Here's a quick explanation about the script-specific bits of code. Lines 1 & 2 tell FSI to include the directories where `Suave` and `FAKE` live when a DLL is referenced. Lines 3 & 4 can be written that way (as opposed to including a relative path) because of the code at lines 1& 2. Once the assemblies are referenced we still need to open the specific namespaces that we're going to use.

This script will start a Suave web app using either the default port # (8083) or one given to it from a caller. At this point you can run the app in FSI and navigate to http://localhost:8083 in your browser.

We need to one more thing before we can host this app in IIS (actually two, but I'm getting ahead of myself). Regardless of the technology (ASP.NET MVC, Suave, Java, Rails, etc.), a site hosted in IIS needs a web.config. Let's add one now:

![Add an Application Configuration File named "web.config"](~/images/addwebconfig.png "Add Web.Config")

<br />
<br />

And here's what should go in the `web.config`:

    <?xml version="1.0" encoding="UTF-8"?>
    <configuration>
    <system.webServer>
        <handlers>
        <remove name="httpplatformhandler" />
        <add name="httpplatformhandler" path="*" verb="*" modules="httpPlatformHandler" resourceType="Unspecified" />
        </handlers>
        <httpPlatform
                stdoutLogEnabled="true" startupTimeLimit="20"
                processPath="./packages/FAKE.4.20.0/tools/FAKE.exe"
                arguments="./Tutorial.fsx port=%HTTP_PLATFORM_PORT%" >
        </httpPlatform>
    </system.webServer>
    </configuration>
    
The web.config is pretty straightforward. When IIS sees the `httpPlatform` section it will run the `FAKE` executable defined in `processPath` passing it `./Tutorial.fsx port=%HTTP_PLATFORM_PORT%` as the argument replacing `%HTTP_PLATFORM_PORT%` with a port that's not in use.

Now we can create a sub-application in IIS. Create a directory that will host the application. I used `C:/sites/SuaveSubApp`. After you've created the directory copy the `web.config` and `Tutorial.fsx` files, and the `packages` folder into that directory. Now open up IIS and right-click on the Default Web Site and click "Add Application". 

![IIS Default Web Site](~/images/iisdefaultwebsite.png "IIS Default Web Site")


The application's alias should be the name of the folder that contains it. So if the folder's name is "SuaveSubApp" that's what the alias should be. The application's physical path is the path to that folder.


![IIS Add Application](~/images/iisaddapplication.png "IIS Add Application")


Awesome! Now if you go to `http://localhost/SuaveSubApp/hello` you'll see your "Hello!" message... Except that doesn't happen at all. ¯\_(ツ)_/¯


![HTTP Error 502.3 - Bad Gateway](~/images/suavebadgateway.png "HTTP Error 502.3 - Bad Gateway")


##What Am I Doing Wrong?
*When we added the web.config I mentioned that there was actually one other thing we would need to do. When I was trying to set this up the first time I forgot this critical step.*

What's going on? If you look in the sub-application folder HttpPlatformHandler has generated some nice log messages for you. Maybe those will help? Unfortanately they don't in this case, but it's good to know for future reference. The site is running as it should so you won't find a friendly error message to point you in the right direction.

This is where the "asking for help" bit of this post comes in. I run web applications as sub-applications every day at work. What was I doing wrong this time? I thrashed on this problem for a while (several evenings in fact). And because I knew it had to be something "really simple" I really didn't want to ask for help. "I don't want to waste anyone else's time", I told myself. That's actually only half true. The other half was that I didn't want someone to find out I was stupid which is what I had already decided was the problem. In hindsight my time is way more valuable than my ego, and I should have asked for help sooner. Also, asking for help gives me the opportunity to help someone else who has the same or similar problem in the future. 

So finally I asked for help. [I asked this question on Stack Overflow](http://stackoverflow.com/questions/34868221/suave-app-hosted-on-iis-with-httpplatformhandler-closes-connection), and then I shared the question on Twitter. The super awesome [David Haney](https://twitter.com/haneycodes) ended up answering my question on Twitter first. He took time out from being in paradise (Hawaii) to help me out which is doubly awesome. Over on Stack Overflow, [Ademar](https://twitter.com/ad3mar/), who happens to be one of the core contributors to Suave, also answered my question. Thanks again to both of you!

Here's the answer: Ultimately when you host an application as a sub-application in IIS it really is part of the main application for the purpose of routing/URLs. In our case we're making a request to a directory under Default Web Site (Default Web Site/SuaveSubApp/). This means "/SuaveSubApp/hello" is getting passed to the sub-application instead of "/hello" like I was expecting. When I'm running the application from FSI it **is** the application. So when I make a request to "http://localhost:8083/hello" while the app is running from FSI "/hello" gets matched because the application is the root.

This ended up being a humbling learning experience. When I'm playing with a new technology, trying out a new recipe, or really doing anything I haven't done before, I let the newness (and fear of failure) distract me from the actual problem I'm trying to solve. This is where a decent mindfulness practice and remembering to be in the moment is helpful. In my case this meant knowing that I'd done this before, and that I knew what steps to take, instead of letting the newness of the framework intimidate me. It also means that when all of that fails (and it will) it really is okay to ask for help.

##Examining Requests in Suave

After figuring out what the problem was I obsessed over how I could have figured it out sooner.

If you look at the incoming request to the application you'll find that the request path getting passed to the `choose` function doesn't match any of the defined routes. One easy way to find this information is to examine the request itself. Let's add a function that handles the case where the request doesn't match any of our routes:

    let notFound =
        warbler(fun r ->
                    OK <| sprintf "No route matching %A" r.request.url.AbsolutePath)

    let app =
        choose [
                path "/hello" >=> OK "Hello!"
                path "/goodbye" >=> OK "Good bye!"
                notFound ]

We create a function, `notFound`, that gets called when `choose` can't find a matching route. The `notFound` function uses Suave's `warbler` function to give us a chance to look at the httpContext's request before returning a WebPart. Maybe this can give us some insight into what's happening? Copy the updated Tutorial.fsx into the sub-application directory, recycle the application pool used by the by the sub-application, and try to hit the app again.

You can recycle the application pool in IIS by going to Application Pools -> DefaultAppPool and then clicking the "Recycle" button in the "Actions" pane on the right-hand side of the window.

![IIS - Recycle App Pool](~/images/iisrecycleapppool.png "IIS - Recycle App Pool")

<br />
<br />

After refreshing the page we get the following: "No route matching "/SuaveSubApp/hello". This tells us two things: 1) our `/hello` and `/goodbye` routes are not being hit, and 2) that the path getting passed to the application is "/SuaveSubApp/hello". So if we change our routes to "/{SubAppName}/path" they'll work, right? Let's find out. Update the routes to the following:

    let app =
        choose [
                path "/SuaveSubApp/hello" >=> OK "Hello!"
                path "/SuaveSubApp/goodbye" >=> OK "Good bye!"
                notFound ]
                
Recycle the app pool again and then refresh the page. Now the request's absolute path matches the path defined in our route and we get back "hello."
 
We don't really want to hardcode the sub-application path into our routes, especially since we don't always have one so let's clean this up. We'll add the sub-application name as an argument to FAKE when the application starts. Upate the `arguments` attribute of `httpPlatform` in the `web.config` like this:
 
    arguments="./Tutorial.fsx port=%HTTP_PLATFORM_PORT% subPath=&quot;/SuaveSubApp&quot;" >

Then update `Tutorial.fsx` with the following:

    let subPath = getBuildParamOrDefault "subPath" ""

    let mapSubPath p = path <| sprintf "%s%s" subPath p

Finally update your routes:

    let app =
        choose [
                mapSubPath "/hello" >=> OK "hello!"
                mapSubPath "/goodbye" >=> OK "Good bye!"
                notFound ]
                
If we have a subPath argument we'll append it to the route we pass to the `path` function. If there isn't a subPath argument then `subPath` is an empty string and nothing really changes. Doing it this way we can run our site from FSI or IIS without having to hardcode the routes based on where the application is running.

##Wrap Up

Here's the final F# script with all the necessary changes:

    #I "./packages/Suave.1.1.1/lib/net40"
    #I "./packages/FAKE.4.20.0/tools"
    #r "Suave.dll"
    #r "FakeLib.dll"

    open System
    open System.Net
    open Fake
    open Suave
    open Suave.Filters
    open Suave.Operators
    open Suave.Successful

    Environment.CurrentDirectory <- __SOURCE_DIRECTORY__

    let subPath = getBuildParamOrDefault "subPath" ""

    let mapSubPath p = path <| sprintf "%s%s" subPath p

    let notFound =
        warbler(fun r ->
                    OK <| sprintf "No route matching %A" r.request.url.AbsolutePath)

    let app =
        choose [
                mapSubPath "/hello" >=> OK "hello!"
                mapSubPath "/goodbye" >=> OK "Good bye!"
                notFound ]

    let port = getBuildParamOrDefault "port" "8083" |> uint16

    let config =
        { defaultConfig with
            bindings = [HttpBinding.mk HTTP IPAddress.Loopback port]
        }

    startWebServer config app

And here's the finished web.config:

    <?xml version="1.0" encoding="UTF-8"?>
    <configuration>
    <system.webServer>
        <handlers>
        <remove name="httpplatformhandler" />
        <add name="httpplatformhandler" path="*" verb="*" modules="httpPlatformHandler" resourceType="Unspecified" />
        </handlers>
        <httpPlatform
                stdoutLogEnabled="true" startupTimeLimit="20"
                processPath="./packages/FAKE.4.20.0/tools/FAKE.exe"
                arguments="./Tutorial.fsx port=%HTTP_PLATFORM_PORT% subPath=&quot;/SuaveSubApp&quot;" >
        </httpPlatform>
    </system.webServer>
    </configuration>

Hopefully you're comfortable with how HttpPlatformHandler works, and with how you can examine an incoming request to a Suave application. I also hope that my reluctance to ask for help resonates with others so that they won't be as hesitant as I was.