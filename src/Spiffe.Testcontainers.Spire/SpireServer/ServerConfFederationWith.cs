using System;

namespace Spiffe.Testcontainers.Spire.Server;

public class ServerConfFederationWith
{
  public ServerConfFederationWith()
  {
  }

  public ServerConfFederationWith(ServerConfFederationWith confFederationWith)
  {
    _ = confFederationWith ?? throw new ArgumentNullException(nameof(confFederationWith));

    TrustDomain = confFederationWith.TrustDomain;
    Host = confFederationWith.Host;
    Port = confFederationWith.Port;
  }

  public string TrustDomain { get; set; } = string.Empty;

  public string Host { get; set; } = string.Empty;

  public int Port { get; set; } = 8443;
}
