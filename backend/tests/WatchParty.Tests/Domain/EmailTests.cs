using WatchParty.Domain.Identity;
using Xunit;

namespace WatchParty.Tests.Domain;

public sealed class EmailTests
{
    [Theory]
    [InlineData(" USER@Example.COM ", "user@example.com")]
    [InlineData("admin@watchparty.local", "admin@watchparty.local")]
    public void Create_normalizes_valid_email(string input, string expected)
    {
        var result = Email.Create(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("missing-domain@")]
    public void Create_rejects_invalid_email(string input)
    {
        var result = Email.Create(input);

        Assert.True(result.IsFailure);
    }
}
