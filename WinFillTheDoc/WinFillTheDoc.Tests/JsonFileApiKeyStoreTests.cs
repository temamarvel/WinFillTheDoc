using NUnit.Framework;
using WinFillTheDoc.Infrastructure.Services;

namespace WinFillTheDoc.Tests;

public sealed class JsonFileApiKeyStoreTests
{
    [Test]
    public void SaveApiKey_WritesProtectedSettingsFile()
    {
        var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"{Guid.NewGuid():N}", "settings.json");
        var store = new JsonFileApiKeyStore(path, new FakeSecretProtector());

        store.SaveApiKey(" sk-test ");
        store.SaveToken(" dadata-test ");
        var json = File.ReadAllText(path);

        Assert.That(store.HasApiKey, Is.True);
        Assert.That(store.HasToken, Is.True);
        Assert.That(store.GetApiKey(), Is.EqualTo("sk-test"));
        Assert.That(store.GetToken(), Is.EqualTo("dadata-test"));
        Assert.That(json, Does.Contain("ProtectedApiKey"));
        Assert.That(json, Does.Not.Contain("sk-test"));
        Assert.That(json, Does.Not.Contain("dadata-test"));
    }

    [Test]
    public void GetApiKey_MigratesPlainSettingsToProtectedSettings()
    {
        var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"{Guid.NewGuid():N}", "settings.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, """
        {
          "OpenAI": { "ApiKey": "sk-plain" },
          "DaData": { "ApiKey": "dadata-plain" }
        }
        """);
        var store = new JsonFileApiKeyStore(path, new FakeSecretProtector());

        Assert.That(store.GetApiKey(), Is.EqualTo("sk-plain"));
        Assert.That(store.GetToken(), Is.EqualTo("dadata-plain"));

        var migrated = File.ReadAllText(path);
        Assert.That(migrated, Does.Contain("ProtectedApiKey"));
        Assert.That(migrated, Does.Not.Contain("sk-plain"));
        Assert.That(migrated, Does.Not.Contain("dadata-plain"));
    }

    [Test]
    public void DeleteApiKey_RemovesOnlySelectedKey()
    {
        var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"{Guid.NewGuid():N}", "settings.json");
        var store = new JsonFileApiKeyStore(path, new FakeSecretProtector());
        store.SaveApiKey("sk-test");
        store.SaveToken("dadata-test");

        store.DeleteApiKey();

        Assert.That(store.HasApiKey, Is.False);
        Assert.That(store.GetToken(), Is.EqualTo("dadata-test"));
    }

    [Test]
    public void GetApiKey_ReturnsNullForCorruptedProtectedValue()
    {
        var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"{Guid.NewGuid():N}", "settings.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, """
        {
          "OpenAI": { "ProtectedApiKey": "bad-value" }
        }
        """);
        var store = new JsonFileApiKeyStore(path, new FakeSecretProtector());

        Assert.That(store.GetApiKey(), Is.Null);
        Assert.That(store.HasApiKey, Is.False);
    }

    private sealed class FakeSecretProtector : ISecretProtector
    {
        public string Protect(string value) => $"protected:{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value))}";

        public string? Unprotect(string protectedValue)
        {
            if (!protectedValue.StartsWith("protected:", StringComparison.Ordinal)) return null;

            var bytes = Convert.FromBase64String(protectedValue["protected:".Length..]);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
    }
}
