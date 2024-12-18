using System.Collections.Immutable;

namespace Svrooij.PowerShell.DI.Generator;

public readonly record struct BindingToGenerate(string BaseClass, string ClassName, string Namespace, ImmutableArray<BindingPropertyToGenerate> Properties)
{
    public string BaseClass { get; } = BaseClass;
    public string ClassName { get; } = ClassName;
    public string Namespace { get; } = Namespace;

    public ImmutableArray<BindingPropertyToGenerate> Properties { get; } = Properties;
}

public readonly record struct BindingPropertyToGenerate(string PropertyName, string PropertyType, bool Required)
{
    public string PropertyName { get; } = PropertyName;
    public string PropertyType { get; } = PropertyType;
    public bool Required { get; } = Required;
}