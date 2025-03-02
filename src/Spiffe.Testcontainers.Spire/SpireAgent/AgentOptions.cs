using System;
using Spiffe.Testcontainers.Spire.Agent;

namespace Spiffe.Testcontainers.Spire;

public class AgentOptions
{
  public AgentOptions()
  {
  }

  public AgentOptions(AgentOptions options)
  {
    _ = options ?? throw new ArgumentNullException(nameof(options));

    ConfPath = options.ConfPath;
    Conf = new AgentConf(options.Conf);
  }

  public string ConfPath { get; set; } = "/etc/spire/agent/agent.conf";

  public AgentConf Conf { get; set; } = new();

  public string TrustBundleCert { get; set; } = SpireResources.Load("server.cert");

  public string Cert { get; set; } = SpireResources.Load("agent.cert");

  public string Key { get; set; } = SpireResources.Load("agent.key");
}
