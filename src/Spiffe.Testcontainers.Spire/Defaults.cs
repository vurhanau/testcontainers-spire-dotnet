using System;
using System.IO;
using System.Reflection;

namespace Spiffe.Testcontainers.Spire;

public class Defaults
{
    // Common
    public const string TrustDomain = "example.com";

    public const string ServerAddress = "spire-server";

    // Server
    public const string ServerImage = "ghcr.io/spiffe/spire-server:1.10.0";

    public const int ServerPort = 8081;

    public const string ServerConfigPath = "/etc/spire/server/server.conf";

    public const string ServerCertPath  = "/etc/spire/server/server.cert";

    public const string ServerKeyPath = "/etc/spire/server/server.key";

    public const string ServerAgentCertPath = "/etc/spire/server/agent.cert";

    public static readonly string ServerConfig = FromResource("server.conf");

    public static readonly string ServerCert = FromResource("server.cert");

    public static readonly string ServerKey = FromResource("server.key");

    // Agent
    public const string AgentImage = "ghcr.io/spiffe/spire-agent:1.10.0";

    public const int AgentPort = 8080;

    public const string AgentConfigPath = "/etc/spire/agent/agent.conf";

    public const string AgentSocketDir = "/tmp/spire/agent/public";

    public const string AgentSocketPath = $"{AgentSocketDir}/api.sock";

    public const string AgentServerCertPath = "/etc/spire/agent/server.cert";

    public const string AgentCertPath = "/etc/spire/agent/agent.cert";

    public const string AgentKeyPath = "/etc/spire/agent/agent.key";

    public static readonly string AgentConfig = FromResource("agent.conf");

    public static readonly string AgentCert = FromResource("agent.cert");

    public static readonly string AgentKey = FromResource("agent.key");

    public static string FromResource(string resource)
    {
        Assembly? assembly = Assembly.GetAssembly(typeof(Defaults))
                                ?? throw new Exception("Assembly not found.");

        string fullResourceName = $"Spiffe.Testcontainers.Spire.resources.{resource}";
        using Stream? s = assembly.GetManifestResourceStream(fullResourceName)
                                ?? throw new Exception($"Resource '{fullResourceName}' not found.");

        using StreamReader c = new(s);
        return c.ReadToEnd();
    }
}
