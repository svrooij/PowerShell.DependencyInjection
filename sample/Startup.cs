using Microsoft.Extensions.DependencyInjection;

namespace Svrooij.PowerShell.DependencyInjection.Sample
{
    public class Startup : PsStartup
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<ITestService, TestService>();
        }
    }
}