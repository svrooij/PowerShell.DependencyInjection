using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Svrooij.PowerShell.DependencyInjection.Extensions;
using Svrooij.PowerShell.DependencyInjection.Tests.TestObjects;
namespace Svrooij.PowerShell.DependencyInjection.Tests;

public class ServiceProviderExtensionsTests
{
    private static ServiceProvider GetServiceProvider(bool registerTestService = true)
    {
        var services = new ServiceCollection();
        if (registerTestService)
            services.AddTransient<TestService>();
        return services.BuildServiceProvider();
    }

    [Fact]
    public void ServiceDependencyProperty_ShouldBeResolved_WhenServiceWasRegistered()
    {
        // Arrange
        var testClass = new TestClassWithServiceDependencyProperty();
        var serviceProvider = GetServiceProvider();

        // Act
        serviceProvider.BindDependencies(testClass);

        // Assert
        testClass.TestService.Should().NotBeNull();
    }

    [Fact]
    public void NonRequiredServiceDependencyProperty_ShouldNotThrowException_WhenServiceNotRegistered()
    {
        // Arrange
        var testClass = new TestClassWithServiceDependencyProperty();
        var serviceProvider = GetServiceProvider(false);

        // Act
        Action act = () => serviceProvider.BindDependencies(testClass);

        // Assert
        act.Should().NotThrow();
        testClass.TestService.Should().BeNull();
    }

    [Fact]
    public void RequiredServiceDependencyProperty_ShouldThrowException_WhenServiceNotRegistered()
    {
        // Arrange
        var testClass = new TestClassWithRequiredServiceDependencyProperty();
        var serviceProvider = GetServiceProvider(false);

        // Act
        Action act = () => serviceProvider.BindDependencies(testClass);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ServiceDependencyField_ShouldBeResolved_WhenServiceWasRegistered()
    {
        // Arrange
        var testClass = new TestClassWithServiceDependencyField();
        var serviceProvider = GetServiceProvider();

        // Act
        serviceProvider.BindDependencies(testClass);

        // Assert
        testClass.TestService.Should().NotBeNull();
    }

    [Fact]
    public void RequiredServiceDependencyField_ShouldNotThrowException_WhenServiceNotRegistered()
    {
        // Arrange
        var testClass = new TestClassWithServiceDependencyField();
        var serviceProvider = GetServiceProvider(false);

        // Act
        Action act = () => serviceProvider.BindDependencies(testClass);

        // Assert
        act.Should().NotThrow();
        testClass.TestService.Should().BeNull();
    }

    [Fact]
    public void RequiredServiceDependencyField_ShouldThrowException_WhenServiceNotRegistered()
    {
        // Arrange
        var testClass = new TestClassWithRequiredServiceDependencyField();
        var serviceProvider = GetServiceProvider(false);

        // Act
        Action act = () => serviceProvider.BindDependencies(testClass);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }
}

internal class TestClassWithServiceDependencyProperty
{
    [ServiceDependency(Required = false)]
    internal TestService TestService { get; set; }
}

internal class TestClassWithRequiredServiceDependencyProperty
{
    [ServiceDependency(Required = true)]
    internal TestService TestService { get; set; }
}

internal class TestClassWithServiceDependencyField
{
    [ServiceDependency(Required = false)]
    internal TestService TestService;
}

internal class TestClassWithRequiredServiceDependencyField
{
    [ServiceDependency(Required = true)]
    internal TestService TestService;
}

