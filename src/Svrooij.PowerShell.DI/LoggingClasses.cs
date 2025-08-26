namespace Svrooij.PowerShell.DI.Generator;

internal class LoggingClasses
{
    internal const string Classes = @"
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
namespace Svrooij.PowerShell.DI.Logging
{
    #nullable enable
    /// <summary>
    /// <see cref=""ILogger""/> that outputs to the PowerShell <see cref=""PSCmdlet""/>
    /// </summary>
    public class PowerShellLogger : ILogger
    {
        /// <summary>
        /// Constructor for <see cref=""PowerShellLogger""/>
        /// </summary>
        /// <remarks>Called automatically by the <see cref=""PowerShellLoggerProvider""/></remarks>
        public PowerShellLogger(string name, Func<PowerShellLoggerConfiguration> getConfig, PowerShellLoggerContainer container)
        {
            this._name = name;
            this._getConfig = getConfig;
            this._container = container;
            this._cmdletName = container.Cmdlet?.GetType().FullName;
        }

        private readonly string _name;
        private string? _cmdletName;
        private readonly Func<PowerShellLoggerConfiguration> _getConfig;
        private readonly PowerShellLoggerContainer _container;

        /// <inheritdoc/>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                try {
                    _container.Cmdlet?.WriteLog(logLevel, eventId.Id, FormatMessage(state, exception, formatter), exception);
                }
                catch (Exception ex)
                {
                    // Logging should never mess up the cmdlet
                    Console.WriteLine($""[ERROR] Write log to PowerShell failed: {ex.Message}"");
                }
            }

        }

        /// <inheritdoc/>
        public bool IsEnabled(LogLevel logLevel)
        {
            var minLevel = _getConfig().GetLogLevel(_name);
            return logLevel >= minLevel;
        }


        /// <inheritdoc/>
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => default!;

        private string FormatMessage<TState>(TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            _cmdletName ??= _container.Cmdlet?.GetType().FullName;
            var config = _getConfig();
            if (config.IncludeCategory && _cmdletName != _name)
            {
                return config.StripNamespace
                    ? $""[{_name.Substring(_name.LastIndexOf('.') + 1)}] {formatter(state, exception)}""
                    : $""[{_name}] {formatter(state, exception)}"";
            }
            return formatter(state, exception);
        }
    }

    /// <summary>
    /// Configuration for the <see cref=""PowerShellLoggerProvider""/>
    /// </summary>
    public sealed class PowerShellLoggerConfiguration
    {
        private const string DefaultLogLevelKey = ""Default"";

        /// <summary>
        /// Minimum level of log messages to output
        /// </summary>
        public LogLevel DefaultLevel
        {
            get => this.LogLevel.ContainsKey(DefaultLogLevelKey) ? this.LogLevel[DefaultLogLevelKey] : Microsoft.Extensions.Logging.LogLevel.Information;
            set => this.LogLevel[DefaultLogLevelKey] = value;
        }

        /// <summary>
        /// Override the minimum level for specific categories (Type Names)
        /// </summary>
        public Dictionary<string, LogLevel> LogLevel { get; set; } = new Dictionary<string, LogLevel>();

        /// <summary>
        /// Specify if the log message should be prefixed with the category name
        /// </summary>
        public bool IncludeCategory { get; set; } = false;

        /// <summary>
        /// Strip the namespace from the category name
        /// </summary>
        public bool StripNamespace { get; set; } = false;

        internal LogLevel GetLogLevel(string name)
        {
            if (LogLevel.TryGetValue(name, out var level))
            {
                return level;
            }

            // Searching without linq for more speed  
            string? bestMatch = null;
            foreach (var key in LogLevel.Keys)
            {
                if (name.StartsWith(key, StringComparison.Ordinal) &&
                    (bestMatch == null || key.Length > bestMatch.Length))
                {
                    bestMatch = key;
                }
            }

            if (bestMatch != null)
            {
                var bestLevel = LogLevel[bestMatch];
                LogLevel[name] = bestLevel; // Cache for next time
                return bestLevel;
            }

            return DefaultLevel;
        }
    }

    /// <summary>
    /// Container for the <see cref=""PSCmdlet""/> that is used by the <see cref=""PowerShellLoggerProvider""/>
    /// </summary>
    public sealed class PowerShellLoggerContainer
    {
        internal PSCmdlet? Cmdlet { get; set; }
    }

    /// <summary>
    /// <see cref=""ILoggerProvider""/> that outputs to the PowerShell <see cref=""PSCmdlet""/>
    /// </summary>
    [ProviderAlias(""Powershell"")]
    public sealed class PowerShellLoggerProvider : ILoggerProvider
    {
        private readonly IDisposable? _onChangeToken;
        private PowerShellLoggerConfiguration _currentConfig;
        private readonly PowerShellLoggerContainer _powerShellLoggerContainer;

        private readonly ConcurrentDictionary<string, PowerShellLogger> _loggers =
            new ConcurrentDictionary<string, PowerShellLogger>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Creates a new instance of <see cref=""PowerShellLoggerProvider""/>
        /// </summary>
        /// <param name=""config"">Auto loaded configuration</param>
        /// <param name=""powerShellLoggerContainer"">Container for the <see cref=""PSCmdlet""/></param>
        public PowerShellLoggerProvider(IOptionsMonitor<PowerShellLoggerConfiguration> config, PowerShellLoggerContainer powerShellLoggerContainer)
        {
            _currentConfig = config.CurrentValue;
            _onChangeToken = config.OnChange(updatedConfig => _currentConfig = updatedConfig);
            _powerShellLoggerContainer = powerShellLoggerContainer;
        }

        private PowerShellLoggerConfiguration GetCurrentConfig()
        {
            return _currentConfig;
        }

        /// <summary>
        /// Creates a new <see cref=""ILogger""/> instance.
        /// </summary>
        /// <param name=""categoryName"">Category name to use</param>
        /// <returns><see cref=""ILogger""/></returns>
        public ILogger CreateLogger(string categoryName)
        {
            // What to do if cmdlet is null?
            return _loggers.AddOrUpdate(
                  categoryName,
                  name => new PowerShellLogger(name, GetCurrentConfig, _powerShellLoggerContainer),
                  (name, logger) => logger
            );
        }

        /// <summary>
        /// Dispose the provider
        /// </summary>
        public void Dispose()
        {
            _loggers.Clear();
            _onChangeToken?.Dispose();
        }
    }

    internal static class PsCmdletExtensions
    {
        /// <summary>
        /// Write a log message to the provider PowerShell <see cref=""PSCmdlet""/>
        /// </summary>
        /// <param name=""cmdlet""><see cref=""PSCmdlet""/> that is used for the log message</param>
        /// <param name=""logLevel""><see cref=""LogLevel""/> for the message, will be put in from the message</param>
        /// <param name=""eventId"">The ID for this specific event</param>
        /// <param name=""message"">Log message</param>
        /// <param name=""e"">(optional) <see cref=""Exception""/></param>
        public static void WriteLog(this PSCmdlet cmdlet, LogLevel logLevel, int eventId, string message, Exception? e = null)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                    cmdlet.CommandRuntime.WriteVerbose(message);
                    break;

                case LogLevel.Debug:
                    cmdlet.CommandRuntime.WriteDebug(message);
                    break;

                case LogLevel.Information:
                    cmdlet.Host.UI.WriteLine($""INFO: {message}"");
                    break;

                case LogLevel.Warning:
                    cmdlet.WriteWarning(message);
                    break;

                case LogLevel.Error:
                    cmdlet.WriteError(new ErrorRecord(e ?? new Exception(message), eventId.ToString(), ErrorCategory.InvalidOperation, null));
                    break;
            }
        }
    }

    internal static class PowershellLoggingBuilderExtensions
    {
        /// <summary>
        /// Adds a PowerShell logger named 'PowerShell' to the logger factory.
        /// </summary>
        /// <param name=""builder""><see cref=""ILoggingBuilder""/> that you get when you call serviceCollection.AddLogging(builder =>)</param>
        /// <returns><see cref=""ILoggingBuilder""/> to support chaining</returns>
        public static ILoggingBuilder AddPowerShellLogging(this ILoggingBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
            Microsoft.Extensions.Logging.Configuration.LoggingBuilderConfigurationExtensions.AddConfiguration(builder);
            //builder.AddConfiguration();

            builder.Services.AddSingleton<PowerShellLoggerContainer>();

            // Register provider (to use DI for the properties)
            builder.Services.AddScoped<ILoggerProvider, PowerShellLoggerProvider>();

            // Register the logger options
            Microsoft.Extensions.Logging.Configuration.LoggerProviderOptions.RegisterProviderOptions
                <PowerShellLoggerConfiguration, PowerShellLoggerProvider>(builder.Services);
            return builder;
        }
    }

    internal static class ServiceProviderLoggerExtensions
    {
        /// <summary>
        /// Adds a PowerShell logger named 'PowerShell' to the service collection.
        /// </summary>
        /// <param name=""services""><see cref=""IServiceCollection""/> you want to add logging to</param>
        /// <param name=""configure"">(optional) action to configure the <see cref=""PowerShellLoggerConfiguration""/></param>
        /// <returns><see cref=""IServiceCollection""/> to support chaining</returns>
        /// <exception cref=""ArgumentNullException"">When one of the required arguments are not set</exception>
        internal static IServiceCollection AddPowerShellLogging(this IServiceCollection services, Action<PowerShellLoggerConfiguration>? configure = null)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddLogging(builder =>
            {
                // Trick to call extension method on ILoggingBuilder because of strange behavior when importing the namespace without alias
                //Microsoft.Extensions.Logging.LoggingBuilderExtensions.SetMinimunLevel(builder, Microsoft.Extensions.Logging.LogLevel.Debug);
                builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
                builder.AddPowerShellLogging();
                if (configure != null)
                {
                    builder.Services.Configure(configure);
                }
            });

            return services;
        }
    }
}";
}