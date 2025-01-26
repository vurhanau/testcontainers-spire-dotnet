using System;
using System.IO;
using System.Reflection;

namespace Testcontainers.Spire;

public class Defaults
{
    public const string TrustDomain = "example.org";

    public const string ServerAddress = "spire-server";

    public static readonly string ServerConf = FromResource("server.conf");

    public static readonly string ServerCert = FromResource("server.crt");

    public static readonly string ServerKey = FromResource("server.key");

    public static readonly string AgentConf = FromResource("agent.conf");

    public static readonly string AgentCert = FromResource("agent.crt");

    public static readonly string AgentKey = FromResource("agent.key");

    public static string FromResource(string resource)
    {
        Assembly? assembly = Assembly.GetAssembly(typeof(Defaults))
                                ?? throw new Exception("Assembly not found.");

        string fullResourceName = $"Testcontainers.Spire.resources.{resource}";
        using Stream? s = assembly.GetManifestResourceStream(fullResourceName)
                                ?? throw new Exception($"Resource '{fullResourceName}' not found.");

        using StreamReader c = new(s);
        return c.ReadToEnd();
    }
}
