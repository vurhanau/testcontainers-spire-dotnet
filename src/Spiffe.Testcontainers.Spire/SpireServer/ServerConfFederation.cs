using System;
using System.Collections.Generic;
using System.Linq;

namespace Spiffe.Testcontainers.Spire.Server;

public class ServerConfFederation
{
  public ServerConfFederation()
  {
  }

  public ServerConfFederation(ServerConfFederation confFederation)
  {
    _ = confFederation ?? throw new ArgumentNullException(nameof(confFederation));

    Port = confFederation.Port;
    FederatesWith = confFederation.FederatesWith?.Select(f => new ServerConfFederationWith(f)).ToList() ?? [];
  }

  public int Port { get; set; } = 8443;

  public List<ServerConfFederationWith> FederatesWith { get; set; } = [];
}
