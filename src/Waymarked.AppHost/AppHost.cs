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

    graphhopper.WithContainerRegistry(registry);
    api.WithContainerRegistry(registry);
    web.WithContainerRegistry(registry);
#pragma warning restore ASPIRECOMPUTE003, ASPIREPIPELINES003

    // Caddy handles TLS termination and proxies to waymarked-web.
    // Bind mounts for TLS cert persistence — paths filled in .env.production on the server.
    builder.AddContainer("caddy", "caddy", "2-alpine")
        .WithBindMount("../../infra/deploy/Caddyfile", "/etc/caddy/Caddyfile", isReadOnly: true)
        .WithBindMount("../../infra/deploy/caddy-data", "/data", isReadOnly: false)
        .WithBindMount("../../infra/deploy/caddy-config", "/config", isReadOnly: false)
        .WithHttpEndpoint(port: 80, targetPort: 80, name: "http")
        .WithHttpsEndpoint(port: 443, targetPort: 443, name: "https")
        .WaitFor(web)
        .PublishAsDockerComposeService((resource, service) =>
        {
            // Caddy must be reachable from the internet — promote ports from expose → ports
            service.Ports.Add("80:80");
            service.Ports.Add("443:443");
            // HTTP/3 (UDP)
            service.Ports.Add("443:443/udp");
        });
}

builder.Build().Run();
