using System;
using HandlebarsDotNet;

namespace Spiffe.Testcontainers.Spire.Server;

public class ServerConf
{
  private static readonly HandlebarsTemplate<object, object> template =
    Handlebars.Compile(SpireResources.Load("server.conf.hbars"));

  public ServerConf()
  {
  }

  public ServerConf(ServerConf conf)
  {
    _ = conf ?? throw new ArgumentNullException(nameof(conf));

    Port = conf.Port;
    SocketPath = conf.SocketPath;
    TrustDomain = conf.TrustDomain;
    LogLevel = conf.LogLevel;
    DataDir = conf.DataDir;
    KeyFilePath = conf.KeyFilePath;
    CertFilePath = conf.CertFilePath;
    CaBundlePath = conf.CaBundlePath;
    AgentPathTemplate = conf.AgentPathTemplate;
    Federation = conf.Federation != null ? new ServerConfFederation(conf.Federation) : null;
  }

  public int Port { get; set; } = 8081;

  public string SocketPath { get; set; } = "/tmp/spire-server/private/api.sock";

  public string TrustDomain { get; set; } = "example.com";

  public string LogLevel { get; set; } = "INFO";

  public string DataDir { get; set; } = "/var/lib/spire/server/.data";

  public string KeyFilePath { get; set; } = "/etc/spire/server/server.key";

  public string CertFilePath { get; set; } = "/etc/spire/server/server.cert";

  public string CaBundlePath { get; set; } = "/etc/spire/server/agent.cert";

  public string AgentPathTemplate { get; set; } = "/x509pop/cn/{{ .Subject.CommonName }}";

  public ServerConfFederation? Federation { get; set; }

  public string Render()
  {
    return template(this);
  }
}
