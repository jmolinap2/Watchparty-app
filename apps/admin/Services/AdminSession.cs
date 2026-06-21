namespace WatchParty.Admin.Services;

public sealed class AdminSession
{
    public string? Token { get; private set; }
    public string? Email { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrEmpty(Token);

    public void SetToken(string token, string email)
    {
        Token = token;
        Email = email;
    }

    public void Clear()
    {
        Token = null;
        Email = null;
    }
}
