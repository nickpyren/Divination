namespace Divination.TwitterIntegration.Credentials;

public interface ICredentialStore
{
    string? Read(string key);

    void Write(string key, string value);

    void Delete(string key);
}
