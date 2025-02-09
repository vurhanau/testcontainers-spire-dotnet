using System;
using System.IO;
using System.Reflection;

namespace Spiffe.Testcontainers.Spire;

public class SpireResources
{
    public static string Load(string resource)
    {
        Assembly? assembly = Assembly.GetAssembly(typeof(Defaults));
        _ = assembly ?? throw new Exception("Assembly not found.");

        string fullResourceName = $"Spiffe.Testcontainers.Spire.resources.{resource}";
        using Stream? s = assembly.GetManifestResourceStream(fullResourceName);
        _ = s ?? throw new Exception($"Resource '{fullResourceName}' not found.");

        using StreamReader c = new(s);
        return c.ReadToEnd();
    }
}
