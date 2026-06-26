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

        Assert.That(store.HasApiKey, Is.True);
        Assert.That(store.GetApiKey(), Is.EqualTo("sk-test"));
    }
}
