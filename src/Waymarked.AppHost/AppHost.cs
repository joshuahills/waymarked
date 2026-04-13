var builder = DistributedApplication.CreateBuilder(args);

// Docker Compose publish environment — only affects `aspire publish` / `aspire deploy`,
// not the local `aspire start` experience.
builder.AddDockerComposeEnvironment("env");

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

// In publish mode, configure GHCR as the container registry for all images,
// and add Caddy as the public-facing reverse proxy with auto-TLS.
if (builder.ExecutionContext.IsPublishMode)
{
    var registryEndpoint = builder.AddParameterFromConfiguration("registryEndpoint", "REGISTRY_ENDPOINT");
    var registryRepository = builder.AddParameterFromConfiguration("registryRepository", "REGISTRY_REPOSITORY");

#pragma warning disable ASPIRECOMPUTE003, ASPIREPIPELINES003
    var registry = builder.AddContainerRegistry("ghcr", registryEndpoint, registryRepository);

    graphhopper.WithContainerRegistry(registry).WithRemoteImageTag("latest");
    api.WithContainerRegistry(registry).WithRemoteImageTag("latest");
    web.WithContainerRegistry(registry).WithRemoteImageTag("latest");
#pragma warning restore ASPIRECOMPUTE003, ASPIREPIPELINES003

    // TLS termination is handled by the existing Caddy instance on this server
    // (shared with other apps). Expose waymarked-web on host port 8081 so the
    // existing Caddy can reverse-proxy to it — see infra/deploy/Caddyfile for
    // the snippet to add to the existing Caddy's config.
    web.PublishAsDockerComposeService((resource, service) =>
    {
        service.Ports.Add("8081:8080");
    });
}

builder.Build().Run();
