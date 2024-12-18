using Microsoft.Extensions.DependencyInjection;
using Svrooij.PowerShell.DI;
using System;
using System.Collections.Generic;
using System.Text;

namespace Svrooij.PowerShell.DependencyInjection.SamplePs5
{
    public class Startup : PsStartup
    {
        override public void ConfigureServices(IServiceCollection services)
        {
            //services.AddSingleton<ISampleService, SampleService>();
        }
    }
}
