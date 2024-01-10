using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using System;
using System.Management.Automation;

namespace Svrooij.PowerShell.DependencyInjection.Logging;

#nullable enable
internal static class ServiceProviderLoggerExtensions
{
    /// <summary>
    /// Adds a PowerShell logger named 'PowerShell' to the service collection.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> you want to add logging to</param>
    /// <param name="cmdlet"><see cref="PSCmdlet"/> that is used to output the log info</param>
    /// <param name="configure">(optional) action to configure the <see cref="PowerShellLoggerConfiguration"/></param>
    /// <returns><see cref="IServiceCollection"/> to support chaining</returns>
    /// <exception cref="ArgumentNullException">When one of the required arguments are not set</exception>
    internal static IServiceCollection AddPowerShellLogging(this IServiceCollection services, PSCmdlet cmdlet, Action<PowerShellLoggerConfiguration>? configure = null)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (cmdlet is null)
        {
            throw new ArgumentNullException(nameof(cmdlet));
        }

        services.AddLogging(builder =>
        {
            builder.AddPowerShellLogging(cmdlet);
            if (configure is not null)
            {
                builder.Services.Configure(configure);
            }
        });

        return services;
    }
}

internal static class PowershellLoggingBuilderExtensions
{
    /// <summary>
    /// Adds a PowerShell logger named 'PowerShell' to the logger factory.
    /// </summary>
    /// <param name="builder"><see cref="ILoggingBuilder"/> that you get when you call serviceCollection.AddLogging(builder =>)</param>
    /// <param name="cmdlet">The <see cref="PSCmdlet"/> that is used to output the log info</param>
    /// <returns><see cref="ILoggingBuilder"/> to support chaining</returns>
    public static ILoggingBuilder AddPowerShellLogging(this ILoggingBuilder builder, PSCmdlet cmdlet)
    {
        builder.AddConfiguration();

        // Register provider (to use DI for the properties)
        builder.Services.AddSingleton<PowerShellLoggerProvider>();

        // Register the previous registered provider ILoggerProvider, so the SetCmdlet() method can be called.
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, PowerShellLoggerProvider>(fac =>
        {
            var p = fac.GetRequiredService<PowerShellLoggerProvider>();
            p.SetCmdlet(cmdlet);
            return p;
        }));

        // Register the logger options
        LoggerProviderOptions.RegisterProviderOptions
            <PowerShellLoggerConfiguration, PowerShellLoggerProvider>(builder.Services);
        return builder;
    }
}