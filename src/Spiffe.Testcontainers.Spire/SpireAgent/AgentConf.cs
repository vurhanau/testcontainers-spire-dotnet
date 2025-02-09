using System;
using HandlebarsDotNet;

namespace Spiffe.Testcontainers.Spire.Agent;

public class AgentConf
{
    private static readonly HandlebarsTemplate<object, object> template = Handlebars.Compile(SpireResources.Load("agent.conf.hbars"));

    public string ServerAddress { get; set; } = "spire-server";

    public int ServerPort { get; set; } = 8081;

    public string SocketPath { get; set; } = "/tmp/spire/agent/public/api.sock";

    public string TrustBundlePath { get; set; } = "/etc/spire/agent/server.cert";

    public string TrustDomain { get; set; } = "example.com";

    public string DataDir { get; set; } = "/var/lib/spire/agent/.data";

    public string LogLevel { get; set; } = "INFO";

    public string CertFilePath { get; set; } = "/etc/spire/agent/agent.cert";

    public string KeyFilePath { get; set; } = "/etc/spire/agent/agent.key";

    public string DockerSocketPath { get; set; } = "/var/run/docker.sock";

    public AgentConf()
    {
    }

    public AgentConf(AgentConf conf)
    {
        _ = conf ?? throw new ArgumentNullException(nameof(conf));

        ServerAddress = conf.ServerAddress;
        ServerPort = conf.ServerPort;
        SocketPath = conf.SocketPath;
        TrustBundlePath = conf.TrustBundlePath;
        TrustDomain = conf.TrustDomain;
        DataDir = conf.DataDir;
        LogLevel = conf.LogLevel;
        CertFilePath = conf.CertFilePath;
        KeyFilePath = conf.KeyFilePath;
        DockerSocketPath = conf.DockerSocketPath;
    }

    public string Render() => template(this);
}
