@{
    Layout = "post";
    Title = "Hosting Suave as a Sub App in IIS & Asking for Help";
    Date = "2016-02-13T08:14:38";
    Tags = "F#,fsharp,suave";
    Description = "A quick explanation on how to host a Suave as an ISS Sub App, with a reminder that asking for help is okay.";
}

While experimenting with [Suave](https://suave.io/) for a side project I wanted to deploy it locally to IIS to see how [HttpPlatformHandler](http://www.iis.net/downloads/microsoft/httpplatformhandler) (also [this](http://azure.microsoft.com/blog/2015/02/04/announcing-the-release-of-the-httpplatformhandler-module-for-iis-8/)) worked. Easy enough, right?! Actually yes, but it took me a long time (several evenings) to get it right. 

First let's take a look at how to host a Suave app as a sub app in IIS. The requirements are pretty straightforward. In fact, you can follow [Scott Hanselman's post about deploying Suave to Azure](http://www.hanselman.com/blog/RunningSuaveioAndFWithFAKEInAzureWebAppsWithGitAndTheDeployButton.aspx) and get most of the way there. Before you do anything else [download](http://www.iis.net/downloads/microsoft/httpplatformhandler) and install version 1.2 of the HttpPlatformHandler IIS module.

