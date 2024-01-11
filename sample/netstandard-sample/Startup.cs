using Microsoft.Extensions.DependencyInjection;
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
