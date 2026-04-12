var builder = DistributedApplication.CreateBuilder(args);

// Add GraphHopper routing engine - built locally from Dockerfile (no official image on Docker Hub/GHCR)
var graphhopper = builder.AddDockerfile("graphhopper", "../../infra/graphhopper")
    .WithBindMount("../../infra/graphhopper/config.yml", "/data/config.yml", isReadOnly: true)
    .WithBindMount("../../infra/graphhopper/data", "/data", isReadOnly: false)
    .WithHttpEndpoint(targetPort: 8989, name: "http")
    .WithEnvironment("JAVA_OPTS", "-Xmx6g -Xms512m")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDeveloperCertificateTrust(trust: false);

// Add Waymarked API service
var api = builder.AddProject<Projects.Waymarked_Api>("waymarked-api")
    .WithReference(graphhopper.GetEndpoint("http"))
    .WithHttpHealthCheck("/health")
    .WaitFor(graphhopper);

// Add Waymarked Web frontend
var web = builder.AddProject<Projects.Waymarked_Web>("waymarked-web")
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
