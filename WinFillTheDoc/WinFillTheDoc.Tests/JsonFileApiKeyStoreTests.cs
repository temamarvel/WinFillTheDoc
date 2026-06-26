using NUnit.Framework;
using WinFillTheDoc.Infrastructure.Services;

namespace WinFillTheDoc.Tests;

public sealed class JsonFileApiKeyStoreTests
{
    [Test]
    public void SaveApiKey_WritesAndReadsSettingsFile()
    {
        var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"{Guid.NewGuid():N}", "settings.json");
        var store = new JsonFileApiKeyStore(path);

        store.SaveApiKey(" sk-test ");
        store.SaveToken(" dadata-test ");

        Assert.That(store.HasApiKey, Is.True);
        Assert.That(store.HasToken, Is.True);
        Assert.That(store.GetApiKey(), Is.EqualTo("sk-test"));
        Assert.That(store.GetToken(), Is.EqualTo("dadata-test"));
    }
}
