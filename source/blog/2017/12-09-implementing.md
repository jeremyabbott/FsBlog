@{
    Layout = "post";
    Title = "Implementing the A in SAFE with the Azure CLI";
    Date = "2017-12-09T09:44:56";
    Tags = "";
    Description = "";
}

I've been working with the [SAFE Stack](https://safe-stack.github.io/) in my free time, and it's a thing of beauty. We just had our annual 3-day hackathon, and the team I was on used the SAFE Stack to great effect. If you're not familiar with SAFE, it stands for [Suave](https://suave.io), [Fable](https://favle.io), [Azure](https://azure.com/), and [Elmish](https://fable-elmish.github.io/elmish/). It's a full stack application model leveraging .NET Core and F#. The back-end runs on .NET Core and Suave. The front-end compiles F# into JavaScript via Fable. Fable allows you to leverage the awesomeness that is F#, in addition to the vastness of the JavaScript ecosystem. In the SAFE Stack, Fable uses Elmish, an application architecture for Fable based on [Elm](https://www.elm-tutorial.org/en/02-elm-arch/01-introduction.html).

The SAFE Stack allows you to share code between the back and front ends, provides front-end type safety, the ability to leverage .NET types in your front-end code, and a rapid development experience thanks to .NET Core's `dotnet watch` and webpack dev server. This means that you can edit your code and see the changes in real time without having to recompile and run tests.

The [SAFE Bookstore](https://github.com/SAFE-Stack/SAFE-BookStore) repo provides an excellent introduction to how the stack works.

The SAFE Bookstore repo details how to publish the application to Azure's Web App for Containers via Azure Portal. However, I had the privilege of seeing Scott Hanselman demo a lot of cool things with the [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/get-started-with-azure-cli?view=azure-cli-latest), and wanted to see how hard it would be to do this with something leveraging the SAFE Stack.

As it turns out, [the steps are fairly simple](https://docs.microsoft.com/en-us/azure/app-service/containers/tutorial-custom-docker-image), and you can do it directly from VS Code! The first time I tried to do this, it didn't work, but I'm fairly certain the problem was between the user and the keyboard, as it's worked every time since then.

## The Azure Account extension for VS Code
These steps assume you have [VS Code](https://code.visualstudio.com/) installed, so if you don't have it, go get it. It's the best editor out there, and its especially great for F# thanks to [Ionide](http://ionide.io/).

Next, get the [Azure Account](https://marketplace.visualstudio.com/items?itemName=ms-vscode.azure-account) extension. You can install it directly from the link, or look it up in the VS Code Extensions tool directly in VS Code.

### Sign In to Azure Account

With the extension installed, open the VS Code Command Palette with the shortcut `[cmd]+[shift]+[p]` (on Windows its `[ctrl]+[shift]+[p]`.

Type "Azure" in the Command Palette textbox.

![Azure Account Sign In](~/images/azureCli/Azure_1.png "Azure Account Sign In")

The palette will show you all the commands available for the Azure Account extension. Select "Azure: Sign In."

This command is going to prompt you to copy a device code (shown in the prompt) and launch a url in your default browser. It will even launch the browser for you and copy the device code to your clipboard. It's easiest to click the "Copy & Open" button.

In the browser, enter your device code. Next, login using your Azure credentials. If you don't have an account you'll need to create one, and fortunately you can do that from the Azure Account extension too.

## Install the Remaining Dependencies
Go ahead and clone the SAFE BookStore repo.

**Don't forget to use your github username here**:

`git clone git@github.com:<your github username>/SAFE-BookStore.git`

The SAFE BookStore app runs on [.NET Core](https://www.microsoft.com/net/download/), but some of its dependencies still require the full .NET Framework (or [Mono](http://www.mono-project.com/) if you're not on Windows).

You'll also need to have [node](https://nodejs.org/), [yarn](https://yarnpkg.com), and [docker](https://www.docker.com/) installed if you don't have it already. The installation steps for these tools will vary depending on your OS, but if you install yarn via [HomeBrew](https://brew.sh/) it will also install node for you. For docker you'll have to install the desktop client appropriate for your OS.

The SAFE Bookstore application not only uses F# for the application itself, but also for the build chain thanks to [FAKE](https://fake.build/). FAKE is a powerful CI/CD orchestration tool, and the SAFE Bookstore sample app demonstrates how powerful it can be.

### Quick Check In
At this point you should have the following installed/forked/cloned:

1. [VS Code](https://code.visualstudio.com/)
1. [Azure Account extension for VS Code](https://marketplace.visualstudio.com/items?itemName=ms-vscode.azure-account)
1. .NET Framework/[Mono](http://www.mono-project.com/)
1. [node](https://nodejs.org/)
1. [yarn](https://yarnpkg.com)
1. [Docker](https://www.docker.com/)
1. [SAFE Bookstore repo](https://github.com/SAFE-Stack/SAFE-BookStore) (forked and cloned)

## Make a Docker Image

It's worth repeating that FAKE is awesome. It's going to do the rest of the heavy lifting for you at this point. Open a terminal to where the SAFE BookStore repo was cloned.

Open the `RELEASE_NOTES.md` files and add a new line to the top like:

```md
### 1.0.0 - 2017-12-10
* F# is Amazing
```

]This step is important because the next step assumnes that a new version is about to be deployed, and in a way it is: it's your version!

Enter the following command:

*On macOS:* `./build.sh Deploy "DockerLoginServer=docker.io" "DockerImageName=****" "DockerUser=****" "DockerPassword=***"`

*On Windows:* `.\build.cmd Deploy "DockerLoginServer=docker.io" "DockerImageName=****" "DockerUser=****" "DockerPassword=***"`

Regardless of OS you'll need to replace `DockerImageName`, `DockerUser`, and `DockerPassword` with your credentials.

*Note:* Manually entering your DockerPassword in the terminal isn't great, and the docker cli will warn you of that. You can look at the [docker login](https://docs.docker.com/engine/reference/commandline/login/) documentation for details on how to send your password to docker login without having to type it in. Just make sure that you **ADD ANY FILES WITH SECURE INFORMATION TO YOUR .gitignore**.

### What Did We Just Do???

SO MANY THINGS. Actually the script called in the previous step is telling FAKE to depoy a new docker image to Docker Hub. And to do that it had to do a lot of other things:

- Clean
- InstallDotNetCore
- InstallClient
- SetReleaseNotes
- BuildServer
- BuildClient
- BuildServerTests
- RunServerTests
- BuildClientTests
- RenameDrivers
- RunClientTests
- BundleClient
- All
- CreateDockerImage
- TestDockerImage
- PrepareRelease
- Deploy

That's right. FAKE is installing .NET Core if it isn't there, installing all the client and server dependencies, running all the unit and integration tests, creating an optimized production bundle for the front-end, then creating **AND** testing a Docker Image, and finally publishing the image to Docker Hub.

FAKE already tested the image and made a container, but you can test it again yourself by running `docker run -d -p 8085:8085 <docker username>/<image name>` where `<image name>` is what you passed in for `DockerImageName`.

For example, if your docker username was "pikachu" and you named the image "fsharpisawesome", you would run `docker run -d -p 8085:8085 pikachu/fsharpisawesome`.

The `-d` runs the docker container in detached mode, which prevents docker from tying up your terminal. `-p 8085:8085` maps port 8085 on your host machine to port 8085 within the container. If you open the `Dockerfile` in the repo, you'll see that port 8085 is exposed via the `EXPOSE` command. Port 8080 is also exposed, but that's only used during development.

You can stop the running container by getting the docker container id via `docker ps`. This will list your running containers. Copy the container ID for the container you just created. Now run `docker stop <container id>`.

## Deploying the Image to Azure
Now that we have our own SAFE Stack application packed into an image and stored in DockerHub, we can deploy it to Azure!

In VS Code hit `[cmd]+[shift]+[p]` (or `[ctrl]+[shift]+[p]` on Windows) to open the Command Palette and enter "Azure" again. This time select "Open Bash in Cloud Shell".

After executing the command VS Code will notify you that it is provisioning "Bash in Cloud Shell" for you.

A new terminal window should open in VS Code. Type `az` in it to use Azure CLI 2.0.

Now we just need to enter 4 commands to get our image deployed using Azure Web App for Containers.

First we need to create a [resource group](https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-overview). Resource groups are in specific geographic locations, and are used to group related Azure resources (like web apps, databases, etc.) together.

```sh
az group create --name <some resource name> --location "West US"
```

A working (variables filled out) command would be:

```sh
az group create --name fsharpIsAwesome --location "West US"
```

Next we need to create an [app service plan](https://docs.microsoft.com/en-us/azure/app-service/azure-web-sites-web-hosting-plans-in-depth-overview). An app service plan describes the resources available to your application. The following command creates an "S1" linux app service plan for the resource group we created in the previous step.

```sh
az appservice plan create --name <app service plan name> --resource-group <resource group name> --sku S1 --is-linux
```

Again, a working example would look like:

```sh
az appservice plan create --name fsharpIsAwesome --resource-group fsharpIsAwesome --sku S1 --is-linux
```

Finally we need to create the web app instance. This command uses the resource group and app service plan created in the last two steps:

```sh
az webapp create --resource-group <resource group name> --plan <app service plan name> --name <web app name> --deployment-container-image-name <fully qualified docker image name>
```

And with the variables filled out, this command would look like:

```sh
az webapp create --resource-group fsharpIsAwesome --plan fsharpIsAwesome --name fsharpIsAwesome --deployment-container-image-name pikachu/fsharpisawesome:latest
```

Finally, we need to map the web app's website port to the deployed container's exposed port:

```sh
az webapp config appsettings set --resource-group <resource group name> --name <web app name> --settings WEBSITES_PORT=8085
```

```sh
az webapp config appsettings set --resource-group fsharpIsAwesome --name fsharpIsAwesome --settings WEBSITES_PORT=8085
```

Note that if you create your Web App for Containers instance via Azure Portal, [you still have to map your container's port via the Azure CLI. There is no support for this via the portal](https://blogs.msdn.microsoft.com/waws/2017/09/08/things-you-should-know-web-apps-and-linux/#OnePort).

It may take a few minutes for your app to start up, but when it's ready, it will be available at `http://<web app name>.azurewebistes.net`.

Finally, after you've verified that your app is running on Azure, you can clean up your resource using the following command:

```sh
az group delete --name <resource group name>
```

For convenience, here are all 5 commands at in one block:

```sh
# create a resource group
az group create --name <some resource name> --location "West US"

# create an app service plan
az appservice plan create --name <app service plan name> --resource-group <resource group name> --sku S1 --is-linux

# create the web app
az webapp create --resource-group <resource group name> --plan <app service plan name> --name <web app name> --deployment-container-image-name <fully qualified docker image name>

# map port 8085 to the web app's WEBSITES_PORT
az webapp config appsettings set --resource-group <resource group name> --name <web app name> --settings WEBSITES_PORT=8085

# delete the resource group and anything that depends on it.
az group delete --name <resource group name>
```

## Wrapping it Up

The [Azure documentation](https://docs.microsoft.com/en-us/azure/app-service/containers/tutorial-custom-docker-image) goes into detail about how to setup a Web App for Containers from a private docker registry, in addition to configuring SSH for accessing your container, and setting up a custom domain, and configuring SSL. Note that SSL terminates at Azure, so you do not need to configure Suave to support SSL.

The SAFE Stack allows you to be wildy productive. Not only do you get hot reloading on the client and server, you get shared code in a terse, readable typesafe language that's easy to reason about. And the stack itself isn't the only thing using F#. The testing frameworks ([Canopy](https://lefthandedgoat.github.io/canopy/) and [Expecto](https://github.com/haf/expecto)), the .NET package manager, and the CI/CD script are written in F#.

There's never been a better time to give functional programming a try with F# and .NET, and a lot of the "yak shaving" has already been done for you!