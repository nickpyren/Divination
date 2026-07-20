namespace Divination.TwitterIntegration.Credentials;

public static class TwitterCredentialKeys
{
    private const string Prefix = "Dalamud.Divination.TwitterIntegration/";

    public const string ConsumerKey = Prefix + nameof(ConsumerKey);
    public const string ConsumerSecret = Prefix + nameof(ConsumerSecret);
    public const string AccessToken = Prefix + nameof(AccessToken);
    public const string AccessTokenSecret = Prefix + nameof(AccessTokenSecret);
}
