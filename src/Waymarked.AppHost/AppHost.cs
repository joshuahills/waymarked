var builder = DistributedApplication.CreateBuilder(args);

// Add GraphHopper routing engine - built locally from Dockerfile (no official image on Docker Hub/GHCR)
var graphhopper = builder.AddDockerfile("graphhopper", "../../infra/graphhopper")
    .WithBindMount("../../infra/graphhopper/config.yml", "/data/config.yml", isReadOnly: true)
    .WithBindMount("../../infra/graphhopper/data", "/data", isReadOnly: false)
    .WithHttpEndpoint(port: 8989, targetPort: 8989, name: "http")
    .WithEnvironment("JAVA_OPTS", "-Xmx6g -Xms512m");

// Add Waymarked API service
var api = builder.AddProject<Projects.Waymarked_Api>("waymarked-api")
    .WithReference(graphhopper.GetEndpoint("http"))
    .WithHttpHealthCheck("/health")
    .WaitFor(graphhopper);

builder.Build().Run();
