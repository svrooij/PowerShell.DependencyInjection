using Microsoft.Extensions.Logging;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;

namespace Svrooij.PowerShell.DependencyInjection.Sample
{
    [Cmdlet(VerbsDiagnostic.Test, "SampleCmdlet")]
    [OutputType(typeof(FavoriteStuff))]
    public class TestSampleCmdletCommand : DependencyCmdlet<Startup>
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

    public class FavoriteStuff
    {
        public int FavoriteNumber { get; set; }
        public string FavoritePet { get; set; }
    }
}