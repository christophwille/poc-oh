var builder = DistributedApplication.CreateBuilder(args);

var pubSub = builder.AddDaprPubSub("demopubsub");

builder.AddProject<Projects.DaprPubSubMinimalAspire>("daprpubsubminimalaspire")
    .WithDaprSidecar()
    .WithReference(pubSub);

builder.Build().Run();
