using System;
using System.Linq;
using Spiffe.Grpc;
using Spiffe.WorkloadApi;

Console.WriteLine("Starting...");
using var channel = GrpcChannelFactory.CreateChannel("unix:///tmp/agent.sock");
var client = WorkloadApiClient.Create(channel);
using var x509Source = await X509Source.CreateAsync(client);
var context  = await client.FetchX509ContextAsync();

Console.WriteLine("Workload id: " + context.X509Svids.First().Id.Id);
