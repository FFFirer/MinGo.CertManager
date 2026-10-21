using MinGo.CertManager.Core.Services;

namespace MinGo.CertManager.Tests.Services;

public class DnsRecordValidatorTests
{
    [Theory]
    [InlineData("A", true)]
    [InlineData("a", true)]
    [InlineData("AAAA", true)]
    [InlineData("CNAME", true)]
    [InlineData("TXT", false)]
    [InlineData("MX", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsAllowedType_OnlyAaaaCnameA(string? type, bool expected)
    {
        Assert.Equal(expected, DnsRecordValidator.IsAllowedType(type));
    }

    [Theory]
    [InlineData("_acme-challenge", true)]
    [InlineData("_acme-challenge.www", true)]
    [InlineData("_acme-challenge.www.", true)]
    [InlineData("www", false)]
    [InlineData("@", false)]
    [InlineData("", false)]
    public void IsAcmeChallengeRr_DetectsPrefix(string? rr, bool expected)
    {
        Assert.Equal(expected, DnsRecordValidator.IsAcmeChallengeRr(rr));
    }

    [Fact]
    public void ValidateCreate_ARecord_Success()
    {
        Assert.Null(DnsRecordValidator.ValidateCreate("www", "A", "1.2.3.4", 600));
    }

    [Fact]
    public void ValidateCreate_AaaaRecord_Success()
    {
        Assert.Null(DnsRecordValidator.ValidateCreate("www", "AAAA", "2001:db8::1", 600));
    }

    [Fact]
    public void ValidateCreate_CnameRecord_Success()
    {
        Assert.Null(DnsRecordValidator.ValidateCreate("www", "CNAME", "cdn.example.com", 600));
    }

    [Fact]
    public void ValidateCreate_DefaultTtlConstant_Is600()
    {
        Assert.Equal(600, DnsRecordValidator.DefaultTtl);
        Assert.Null(DnsRecordValidator.ValidateCreate("www", "A", "1.2.3.4", DnsRecordValidator.DefaultTtl));
    }

    [Theory]
    [InlineData("www", "A", "not-an-ip", 600)]
    [InlineData("www", "AAAA", "1.2.3.4", 600)]
    [InlineData("www", "CNAME", "1.2.3.4", 600)]
    [InlineData("", "A", "1.2.3.4", 600)]
    [InlineData("www", "MX", "10 mail.example.com", 600)]
    [InlineData("www", "A", "1.2.3.4", 60)]
    [InlineData("www", "A", "1.2.3.4", 999999)]
    public void ValidateCreate_Invalid_ThrowsMessage(string rr, string type, string value, int ttl)
    {
        Assert.NotNull(DnsRecordValidator.ValidateCreate(rr, type, value, ttl));
    }

    [Fact]
    public void ValidateCreate_RootCname_Rejected()
    {
        var error = DnsRecordValidator.ValidateCreate("@", "CNAME", "cdn.example.com", 600);
        Assert.NotNull(error);
        Assert.Contains("根域名", error);
    }

    [Fact]
    public void ValidateCreate_AcmeRr_Rejected()
    {
        var error = DnsRecordValidator.ValidateCreate("_acme-challenge.www", "TXT", "x", 600);
        Assert.NotNull(error);
    }

    [Fact]
    public void ValidateDelete_AcmeRr_Rejected()
    {
        Assert.NotNull(DnsRecordValidator.ValidateDelete("_acme-challenge.www"));
        Assert.Null(DnsRecordValidator.ValidateDelete("www"));
    }
}
