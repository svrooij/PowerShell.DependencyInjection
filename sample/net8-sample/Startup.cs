using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Svrooij.PowerShell.DI;
using Svrooij.PowerShell.DI.Logging;

namespace Svrooij.PowerShell.DependencyInjection.Sample;

/// <summary>
/// Startup class for the PowerShell Dependency Injection sample application.
/// </summary>
public class Startup : PsStartup
{
    /// <inheritdoc />
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<ITestService, TestService>();
    }

    /// <inheritdoc />
    public override Action<PowerShellLoggerConfiguration> ConfigurePowerShellLogging()
    {
        return builder =>
        {
            builder.DefaultLevel = LogLevel.Information;
            builder.LogLevel["Svrooij.PowerShell.DependencyInjection.Sample"] = LogLevel.Debug;
            builder.IncludeCategory = true;
            builder.StripNamespace = true;
        };
    }
}
