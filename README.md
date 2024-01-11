# Svrooij.PowerShell.DependencyInjection

A library that allows you to use dependency injection in binary PowerShell modules.

- Dependency injection (based on Microsoft.Extensions.DependencyInjection)
- Using `ILogger` throughout your PowerShell Module.
- Run any asynchronous code in your commandlets.

I choose to only support a Task, I guess that all code that you're executing using this package will by async anyway.

## Create a new PowerShell module

```shell
# Install the correct template (only once)
dotnet new install Microsoft.PowerShell.Standard.Module.Template

# Create a new directory and enter it
mkdir MyNewModule
cd MyNewModule

# Create a new PowerShell module
dotnet new psmodule
```

## Add Package

Add the pacakge `dotnet add package Svrooij.PowerShell.DependencyInjection`

## Edit project file

Add the `CopyLocalLockFileAssemblies` to your project file, this will make sure that the dependencies are copied to the output folder.

```xml
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <AssemblyName>Svrooij.PowerShell.DependencyInjection.SamplePs5</AssemblyName>
	<CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
  </PropertyGroup>
```

And change the version of `PowerShellStandard.Library` to `5.1.1`.

```xml
  <ItemGroup>
	<PackageReference Include="PowerShellStandard.Library" Version="5.1.1" />
	<PackageReference Include="Svrooij.PowerShell.DependencyInjection" Version="1.0.1" />
  </ItemGroup>
```

## Create a Startup class

```csharp
using Microsoft.Extensions.DependencyInjection;
using Svrooij.PowerShell.DependencyInjection;

public class Startup : PsStartup
    {
        // You need to override this method.
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<ITestService, TestService>();
        }
    }
```

## Create a CmdLet

1. Instead of inheriting from `PsCmdLet`, you need to inherit `DependencyCmdlet<YourStartupClass>`.
2. And then you put the `[ServiceDependency]` attribute above every private (or internal), field or property you want loaded from dependency injection.
3. Override the `Task ProcessRecordAsync(CancellationToken cancellationToken)` method and call all the async stuff you want.

```csharp
    [Cmdlet(VerbsDiagnostic.Test, "SampleCmdlet")]
    [OutputType(typeof(FavoriteStuff))]
    public class TestSampleCmdletCommand : DependencyCmdlet<YourStartupClass>
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
        [ServiceDependency]
        private ITestService TestService { get; set; }

        // Logging using Microsoft.Extensions.Logging is supported (and configured automatically)
        // You can alse use the regular WriteVerbose(), WriteDebug(), WriteInformation(), WriteWarning() and WriteError() methods
        [ServiceDependency]
        private ILogger<TestSampleCmdletCommand> _logger;

        // This method will be called automatically by DependencyCmdlet which is called by ProcessRecord()
        public override async Task ProcessRecordAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting ProcessRecordAsync()");

            await TestService.DoSomethingAsync(cancellationToken);

            WriteObject(new FavoriteStuff
            {
                FavoriteNumber = this.FavoriteNumber,
                FavoritePet = this.FavoritePet
            });
        }
    }
```