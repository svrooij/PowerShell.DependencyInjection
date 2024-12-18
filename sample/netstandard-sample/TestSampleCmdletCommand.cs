using Microsoft.Extensions.Logging;
using System;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Threading.Tasks;
using Svrooij.PowerShell.DI;

namespace Svrooij.PowerShell.DependencyInjection.SamplePs5
{
    [Cmdlet(VerbsDiagnostic.Test, "SampleCmdlet")]
    [OutputType(typeof(FavoriteStuff))]
    [GenerateBindings]
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

        [ServiceDependency]
        private ILogger<TestSampleCmdletCommand> _logger;

        // This method will be called for each input received from the pipeline to this cmdlet; if no input is received, this method is not called
        public override async Task ProcessRecordAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting to process record (with a fake 5 seconds delay, cancellable with CTRL+C");
            await Task.Delay(5000, cancellationToken);
            _logger.LogInformation("This was logged with the injected logger");
            WriteObject(new FavoriteStuff
            {
                FavoriteNumber = FavoriteNumber,
                FavoritePet = FavoritePet
            });
        }

    }

    public class FavoriteStuff
    {
        public int FavoriteNumber { get; set; }
        public string FavoritePet { get; set; }
    }
}
