using System.Reflection;
using WinFillTheDoc.Application.Services;

namespace WinFillTheDoc.Infrastructure.Services;

public sealed class AssemblyAppVersionProvider : IAppVersionProvider
{
    public string CurrentVersion => GetVersion(Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly());

    public static string GetVersion(Assembly assembly) =>
        assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? assembly.GetName().Version?.ToString(3)
        ?? "0.0.0";
}
