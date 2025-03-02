using System;
using System.IO;
using System.Reflection;
using Spiffe.Testcontainers.Spire.Server;

namespace Spiffe.Testcontainers.Spire;

public static class SpireResources
{
  public static string Load(string resource)
  {
    var assembly = Assembly.GetAssembly(typeof(SpireServerBuilder));
    _ = assembly ?? throw new Exception("Assembly not found.");

    var fullResourceName = $"Spiffe.Testcontainers.Spire.resources.{resource}";
    using var s = assembly.GetManifestResourceStream(fullResourceName);
    _ = s ?? throw new Exception($"Resource '{fullResourceName}' not found.");

    using StreamReader c = new(s);
    return c.ReadToEnd();
  }
}
