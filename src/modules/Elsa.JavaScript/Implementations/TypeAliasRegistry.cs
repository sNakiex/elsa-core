using System.Dynamic;
using Elsa.JavaScript.Extensions;
using Elsa.JavaScript.Services;
using Elsa.Workflows.Management.Options;
using Microsoft.Extensions.Options;

namespace Elsa.JavaScript.Implementations;

/// <inheritdoc />
public class TypeAliasRegistry : ITypeAliasRegistry
{
    private readonly IDictionary<Type, string> _typeAliasDictionary = new Dictionary<Type, string>();

    /// <summary>
    /// Constructor.
    /// </summary>
    public TypeAliasRegistry(IOptions<ManagementOptions> managementOptions)
    {
        this.RegisterType<object>("any");
        this.RegisterType<ExpandoObject>("any");
        this.RegisterType<string>("string");
        this.RegisterType<bool>("boolean");
        this.RegisterType<short>("number");
        this.RegisterType<int>("number");
        this.RegisterType<long>("number");
        this.RegisterType<decimal>("Decimal");
        this.RegisterType<float>("Single");
        this.RegisterType<double>("Double");
        this.RegisterType<DateTime>("Date");
        this.RegisterType<DateTimeOffset>("Date");
        this.RegisterType<DateOnly>("Date");
        this.RegisterType<TimeOnly>("Date");

        foreach (var variableDescriptor in managementOptions.Value.VariableDescriptors)
        {
            if(!_typeAliasDictionary.ContainsKey(variableDescriptor.Type))
                RegisterType(variableDescriptor.Type, variableDescriptor.Type.Name);
        }
    }

    /// <inheritdoc />
    public void RegisterType(Type type, string alias)
    {
        _typeAliasDictionary[type] = alias;
    }

    /// <inheritdoc />
    public bool TryGetAlias(Type type, out string alias) => _typeAliasDictionary.TryGetValue(type, out alias!);
}