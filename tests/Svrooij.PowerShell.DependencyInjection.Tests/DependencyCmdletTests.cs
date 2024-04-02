using System.Management.Automation;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Svrooij.PowerShell.DependencyInjection.Tests.TestObjects;

namespace Svrooij.PowerShell.DependencyInjection.Tests;

public class DependencyCmdletTests
{
    [Fact]
    public void BeginProcessing_ShouldResolveDependencies_WhenBeginProcessingIsCalled()
    {
        var cmdlet = new TestSampleCmdlet();
        cmdlet.TriggerBeginProcessing();
        cmdlet.TestService.Should().NotBeNull();
        cmdlet._logger.Should().NotBeNull();
    }

    [Fact]
    public void ProcessRecord_ShouldCallProcessRecordAsyncWithAValidCancellationToken()
    {
        // arrange
        var cmdlet = new TestSampleCmdlet();
        cmdlet.TriggerBeginProcessing();

        // act
        cmdlet.TriggerProcessRecord();

        // assert
        // Happens in the ProcessRecordAsync method

    }
}

[Cmdlet(VerbsDiagnostic.Test, "SampleCmdLet")]
public class TestSampleCmdlet : DependencyCmdlet<TestStartup>
{
    [ServiceDependency]
    internal TestService TestService { get; set; }

    [ServiceDependency]
    internal ILogger<TestSampleCmdlet> _logger;

    public override Task ProcessRecordAsync(CancellationToken cancellationToken)
    {
        cancellationToken.Should().NotBeNull();
        return Task.CompletedTask;
    }

    public void TriggerBeginProcessing()
    {
        this.BeginProcessing();
    }

    public void TriggerEndProcessing()
    {
        this.EndProcessing();
    }

    public void TriggerStopProcessing()
    {
        this.StopProcessing();
    }

    public void TriggerProcessRecord()
    {
        this.ProcessRecord();
    }
}

public class TestStartup : PsStartup
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<TestService>();
    }
}