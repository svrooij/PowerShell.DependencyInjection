using System;
using Microsoft.Extensions.Logging;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Svrooij.PowerShell.DependencyInjection.Sample
{
    [Cmdlet(VerbsDiagnostic.Test, "SampleCmdlet")]
    [OutputType(typeof(FavoriteStuff))]
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
        internal ITestService TestService { get; set; }

        // Logging using Microsoft.Extensions.Logging is supported (and configured automatically)
        // You can alse use the regular WriteVerbose(), WriteDebug(), WriteInformation(), WriteWarning() and WriteError() methods
        [ServiceDependency]
        internal ILogger<TestSampleCmdletCommand> _logger;

        // This method will be called automatically by DependencyCmdlet which is called by ProcessRecord()
        public override async Task ProcessRecordAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting ProcessRecordAsync()");
            
            // In the startup class we configured the logging level for this namespace to Debug
            _logger.LogDebug("FavoriteNumber: {FavoriteNumber}, FavoritePet: {favoritePet}", FavoriteNumber, FavoritePet);
            
            _logger.LogWarning("This is a warning");
            _logger.LogError("This is an error");

            await TestService.DoSomethingAsync(cancellationToken);

            WriteObject(new FavoriteStuff
            {
                FavoriteNumber = this.FavoriteNumber,
                FavoritePet = this.FavoritePet
            });
        }
        
        // protected override Action<DependencyCmdlet<Startup>, IServiceProvider> BindDependencies { get; } = (obj, serviceProvider) =>
        // {
        //     if (obj is TestSampleCmdletCommand cmdlet)
        //     {
        //         cmdlet.TestService = (ITestService)serviceProvider.GetService(typeof(ITestService));
        //         cmdlet._logger = (ILogger<TestSampleCmdletCommand>)serviceProvider.GetService(typeof(ILogger<TestSampleCmdletCommand>));
        //     }
        // };
    }

    public class FavoriteStuff
    {
        public int FavoriteNumber { get; set; }
        public string FavoritePet { get; set; }
    }
}