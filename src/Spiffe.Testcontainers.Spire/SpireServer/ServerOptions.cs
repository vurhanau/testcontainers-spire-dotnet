using System;

namespace Spiffe.Testcontainers.Spire.Server;

public class ServerOptions
{
    public string ConfPath { get; set; } = "/etc/spire/server/server.conf";

    public ServerConf Conf { get; set; } = new();

    public string Cert { get; set; } = SpireResources.Load("server.cert");

    public string Key { get; set; } = SpireResources.Load("server.key");

    public string CaBundle { get; set; } = SpireResources.Load("agent.cert");

    public ServerOptions()
    {
    }

    public ServerOptions(ServerOptions options)
    {
        _ = options ?? throw new ArgumentNullException(nameof(options));

        ConfPath = options.ConfPath;
        Conf = new(options.Conf);
    }
}
