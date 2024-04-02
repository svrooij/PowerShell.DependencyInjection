using System;
using Microsoft.Extensions.DependencyInjection;
using Svrooij.PowerShell.DependencyInjection.Logging;
using System.Management.Automation;
using Microsoft.Extensions.Logging;

namespace Svrooij.PowerShell.DependencyInjection;

/// <summary>
/// Base class for startup classes for PowerShell cmdlets.
/// Create a class that extends this class and override <see cref="ConfigureServices(IServiceCollection)"/>.
/// </summary>
/// <remarks>
/// This class is called automatically by the <see cref="DependencyCmdlet{T}"/> constructor.
/// Logging is automatically added to the <see cref="IServiceCollection"/>, please don't mess with the logging part. 
/// </remarks>
public abstract class PsStartup
{
    internal void ConfigurePowerShellServices(IServiceCollection services, PSCmdlet cmdlet)
    {
        // services.AddPowerShellLogging(cmdlet, builder =>
        // {
        //     builder.DefaultLevel = LogLevel.Information;
        //     builder.LogLevel["Svrooij.PowerShell.DependencyInjection.Sample"] = LogLevel.Debug;
        // });

        services.AddPowerShellLogging(cmdlet, ConfigurePowerShellLogging());
    }

    /// <summary>
    /// Override this method to configure the <see cref="IServiceCollection"/> needed by your application.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> that you can add services to.</param>
    /// <remarks>Logging is setup for you, overriding it breaks stuff!</remarks>
    public virtual void ConfigureServices(IServiceCollection services)
    {
    }

#nullable enable
    /// <summary>
    /// Override this method to configure the <see cref="PowerShellLoggerConfiguration"/> needed by your application.
    /// </summary>
    /// <code>
    /// return builder =>
    /// {
    ///    builder.MinimumLevel = LogLevel.Information;
    /// };
    /// </code>
    public virtual Action<PowerShellLoggerConfiguration>? ConfigurePowerShellLogging()
    {
        // Default implementation, you can override this method to change the default behavior.
        return null;
    }
}