using MinGo.CertManager.Core.Services;

namespace MinGo.CertManager.Tests.Services;

public class CredentialMaskHelperTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData("LTAI5tExampleKeyId", "LTAI5t****")]
    [InlineData("short", "****")]
    [InlineData("123456", "****")]
    [InlineData("1234567", "123456****")]
    public void MaskAccessKeyId_ShouldKeepPrefixOnly(string? input, string expected)
    {
        Assert.Equal(expected, CredentialMaskHelper.MaskAccessKeyId(input));
    }

    [Fact]
    public void MaskAccessKeyId_ShouldNotContainFullSecret()
    {
        var full = "LTAI5tVeryLongAccessKeyIdValue123456";
        var masked = CredentialMaskHelper.MaskAccessKeyId(full);

        Assert.NotEqual(full, masked);
        Assert.StartsWith("LTAI5t", masked);
        Assert.DoesNotContain("VeryLongAccessKeyIdValue123456", masked);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("  ", false)]
    [InlineData("x", true)]
    public void IsProvided_ShouldDetectNonWhitespace(string? value, bool expected)
    {
        Assert.Equal(expected, CredentialMaskHelper.IsProvided(value));
    }
}
