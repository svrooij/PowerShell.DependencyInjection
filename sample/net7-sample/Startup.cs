using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Svrooij.PowerShell.DependencyInjection.Logging;

namespace Svrooij.PowerShell.DependencyInjection.Sample
{
    public class Startup : PsStartup
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<ITestService, TestService>();
        }

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