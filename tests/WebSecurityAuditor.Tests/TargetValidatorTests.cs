using Xunit;
using WebSecurityAuditor.Core;

namespace WebSecurityAuditor.Tests;

public sealed class TargetValidatorTests
{
    [Fact]
    public void NormalizeTarget_ExtractsHostFromUrl()
    {
        var target = TargetValidator.NormalizeTarget("https://Example.com/path");
        Assert.Equal("example.com", target);
    }

    [Fact]
    public void ValidatePorts_RejectsLargeRange()
    {
        var error = Assert.Throws<ArgumentException>(() => TargetValidator.ValidatePorts(1, 1001));
        Assert.Contains("limited", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_RequiresAuthorization()
    {
        Assert.Throws<ArgumentException>(() => TargetValidator.Validate("example.com", "80", "443", "800", false));
    }
}
