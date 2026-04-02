using Sentri.Api.Domain;

namespace Sentri.Tests.Common.Builders;

public class ProviderBuilder
{
    private string _name = "Test Provider";
    private decimal _monthlyBudget = 100m;
    private decimal _warningThreshold = 0.8m;
    private Guid _userId = Guid.NewGuid();

    public ProviderBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public ProviderBuilder WithBudget(decimal budget)
    {
        _monthlyBudget = budget;
        return this;
    }

    public ProviderBuilder WithThreshold(decimal threshold)
    {
        _warningThreshold = threshold;
        return this;
    }

    public ProviderBuilder WithUserId(Guid userId)
    {
        _userId = userId;
        return this;
    }

    public Provider Build() =>
        new Provider(_name, _monthlyBudget, _warningThreshold, _userId);
}
