# Svrooij.PowerShell.DI

This package is a simple way to use Dependency Injection in your PowerShell module.
It is built as a [Source Generator](#source-generator) so this package won't show up in your module.

- Dependency injection, create a `Startup` class and allow your command to automatically get the services.
- Using `ILogger` throughout your PowerShell Module.
- Run any asynchronous code in your command lets.

All code doing some task is probably async, so I choose to only support a `Task` which you can override.

## Getting started

1. Choose your module type:
   - [Create a new PowerShell module](#create-powershell-core-module)
   - [Create a new Windows PowerShell module (template)](#create-a-new-windows-powershell-module-template)
2. Create module using the links above
3. Add [Startup class](#startup-class)
4. Add [SampleCommand](#command-with-di)
5. Start your module (by pressing `F5`)
6. Test your command
7. (optional) Add [manifest](https://learn.microsoft.com/en-us/powershell/scripting/developer/module/how-to-write-a-powershell-module-manifest?view=powershell-7.4&wt.mc_id=SEC-MVP-5004985)

## Source generator

This package is a roslyn [source generator](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/?wt.mc_id=SEC-MVP-5004985#source-generators) which means that it will generate code during the build process.

Advantages:

- No need to add a reference to this package in your module.
- Compatible with `Microsoft.PowerShell.SDK` [get started](#create-powershell-core-module) and `PowerShellStandard.Library` [get started](#create-a-new-windows-powershell-module-template).
- Compatible with Windows PowerShell and PowerShell Core.
- You pick your own version of the `Microsoft.Extensions.DependencyInjection` package.

## Startup class

Once the library is added (see below), you need to create a `Startup` class that inherits from `PsStartup`.

```csharp
using Microsoft.Extensions.DependencyInjection;
using Svrooij.PowerShell.DI;

namespace YouNamespace
{
    public class Startup : PsStartup
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            // Add your services here
            //services.AddSingleton<ISampleService, SampleService>();
        }
        
        // Optionally over the logging configuration
        public override Action<PowerShellLoggerConfiguration> ConfigurePowerShellLogging()
        {
            return builder =>
            {
                builder.DefaultLevel = LogLevel.Information;
                builder.LogLevel["Svrooij.PowerShell.DependencyInjection.Sample.TestSampleCmdletCommand"] = LogLevel.Debug;
                // builder.LogLevel["Svrooij.PowerShell.DependencyInjection.Sample.TestService"] = LogLevel.Information;
                builder.IncludeCategory = true;
                builder.StripNamespace = true;
            };
        }
    }
}
```

## Command with DI

To start using the DI in your command let you'll need to create a class that inherits from `DependencyCmdlet<Startup>`.

```csharp
using Microsoft.Extensions.Logging;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using Svrooij.PowerShell.DI;

namespace Svrooij.PowerShell.DependencyInjection.Sample;

[Cmdlet(VerbsDiagnostic.Test, "SampleCmdlet")] // You can specify the name of the cmdlet
[OutputType(typeof(FavoriteStuff))] // You can specify an output type as you're used to.
[GenerateBindings] // If your class is partial, and you want to go for max speed, add this attribute to have it generate the bindings at compile time.
public partial class TestSampleCmdletCommand : DependencyCmdlet<Startup>
{
    [Parameter(
        Mandatory = true,
        Position = 0,
        ValueFromPipeline = true,
        ValueFromPipelineByPropertyName = true)]
    public int FavoriteNumber { get; set; }

    [Parameter(
        Position = 1,
        ValueFromPipelineByPropertyName = true)]
    [ValidateSet("Cat", "Dog", "Horse")]
    public string FavoritePet { get; set; } = "Dog";

    // Give your dependencies the ServiceDependency attribute
    // Only private/internal properties and fields with this attribute will be resolved
    // public properties and fields will be ignored because they might also be exposed to powershell.
    [ServiceDependency]
    private Svrooij.PowerShell.DependencyInjection.Sample.ITestService _testService;

    // Logging using Microsoft.Extensions.Logging is supported (and configured automatically)
    // You can alse use the regular WriteVerbose(), WriteDebug(), WriteInformation(), WriteWarning() and WriteError() methods
    [ServiceDependency(Required = true)]
    private Microsoft.Extensions.Logging.ILogger<TestSampleCmdletCommand> _logger;

    // This method will be called automatically by DependencyCmdlet which is called by ProcessRecord()
    public override async Task ProcessRecordAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting ProcessRecordAsync()");

        // In the startup class we configured the logging level for this namespace to Debug
        _logger.LogDebug("FavoriteNumber: {FavoriteNumber}, FavoritePet: {favoritePet}", FavoriteNumber, FavoritePet);

        _logger.LogWarning("This is a warning");
        _logger.LogError("This is an error");

        await _testService.DoSomethingAsync(cancellationToken);

        WriteObject(new FavoriteStuff
        {
            FavoriteNumber = this.FavoriteNumber,
            FavoritePet = this.FavoritePet
        });
    }
}

public class FavoriteStuff
{
    public int FavoriteNumber { get; set; }
    public string FavoritePet { get; set; }
}
```

## Create PowerShell core module

I did not find a template for creating a new PowerShell module with the new SDK. So let's create a new class library and add the required packages.

```shell
# PowerShell Core is build on NET8.0 so we need to create a new class library with that framework.
# Azure Automation requires NET6.0 (as of Dec 2024), so you might want to create a new module with that framework.
dotnet new classlib -n MyNewModule -o MyNewModule --framework net8.0
cd MyNewModule
# Add the PowerShell SDK
dotnet add package Microsoft.PowerShell.SDK
# Add the logging package (which also installs Microsoft.Extensions.Logging and Microsoft.Extensions.DependencyInjection)
dotnet add package Microsoft.Extensions.Logging.Configuration --version 8.0.1
```

### Add package to core module

Let's add the [source generator](#source-generator) to the project, and build it at least once (to generate the attributes and classes)

```shell
dotnet add package Svrooij.PowerShell.DI
# And build the project at least once
dotnet build
```

### Add launch settings

Add a `launchSettings.json` file to the `Properties` folder with the following content.

```json
{
  "profiles": {
    "Start in PowerShell Core": {
      "executablePath": "C:\\Program Files\\PowerShell\\7\\pwsh.exe",
      "commandName": "Executable",
      "commandLineArgs": "-noexit -command &{ Import-Module .\\\\MyNewModule.dll -Verbose}"
    }
  }
}
```

### Start your core module

Now that you added all the mandatory parts, and added you [startup class](#startup-class) and [command let](#command-with-di) you can start your module.

Just press `F5` and you should see the PowerShell Core window pop up with your module loaded.

## Create a new Windows PowerShell module (template)

```shell
# Install the correct template (only once)
dotnet new install Microsoft.PowerShell.Standard.Module.Template

# Create a new directory and enter it
mkdir MyNewModule
cd MyNewModule

# Create a new PowerShell module
dotnet new psmodule
```

### Add package to new template module

You just need to add some packages to your project file.

```xml
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Logging.Configuration" Version="8.0.1" />
    <PackageReference Include="PowerShellStandard.Library" Version="5.1.1">
      <PrivateAssets>All</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Svrooij.PowerShell.DI" />
  </ItemGroup>
```

```shell
# Dependencies (not sure about the version)
dotnet add package Microsoft.Extensions.Logging.Configuration --version 8.0.1
# PowerShell Standard Library (for Windows PowerShell)
# should already be in the project file, but if not, add it
dotnet add package PowerShellStandard.Library --version 5.1.1

# Source Generator
dotnet add package Svrooij.PowerShell.DI

## Build at least once to generate the classes
dotnet build
```

### Add Launch settings to template project

Add a `launchSettings.json` file to the `Properties` folder with the following content.

```json
{
  "profiles": {
    "Start in Windows PowerShell": {
      "executablePath": "C:\\Windows\\System32\\WindowsPowerShell\\v1.0\\powershell.exe",
      "commandName": "Executable",
      "commandLineArgs": "-noexit -command &{ Import-Module .\\MyNewModule.dll -Verbose}"
    }
  }
}
```

### Start your Windows PowerShell module

Now that you added all the mandatory parts, and added you [startup class](#startup-class) and [command let](#command-with-di) you can start your module.

Just press `F5` and you should see the Windows PowerShell window pop up with your module loaded.
