var builder = DistributedApplication.CreateBuilder(args);

// Docker Compose publish environment — only affects `aspire publish` / `aspire deploy`,
// not the local `aspire start` experience.
builder.AddDockerComposeEnvironment("env");

// In CI, use the pre-built graphhopper-ci image (built by the graph pre-build step)
// to avoid rebuilding from the Dockerfile during test runs (~5 min saved).
// Locally, build from the Dockerfile so changes to the GH config are picked up.
var prebuiltImage = Environment.GetEnvironmentVariable("GRAPHHOPPER_PREBUILT_IMAGE");

IResourceBuilder<ContainerResource> graphhopper;
if (!string.IsNullOrEmpty(prebuiltImage))
{
    graphhopper = builder.AddContainer("graphhopper", prebuiltImage);
}
else
{
    graphhopper = builder.AddDockerfile("graphhopper", "../../infra/graphhopper")
        .WithLifetime(ContainerLifetime.Persistent)
        .WithDeveloperCertificateTrust(trust: false);
}

// Common configuration — bind-mount config + data dir, expose HTTP, set JVM heap.
// In CI with the small IoW extract, 2 GB is plenty. Locally use 6 GB for full UK data.
graphhopper
    .WithBindMount("../../infra/graphhopper/config.yml", "/data/config.yml", isReadOnly: true)
    .WithBindMount("../../infra/graphhopper/data", "/data", isReadOnly: false)
    .WithHttpEndpoint(targetPort: 8989, name: "http")
    .WithEnvironment("JAVA_OPTS", string.IsNullOrEmpty(prebuiltImage) ? "-Xmx6g -Xms512m" : "-Xmx2g -Xms256m");

// Add Waymarked API service
var api = builder.AddProject<Projects.Waymarked_Api>("waymarked-api", launchProfileName: "http")
    .WithReference(graphhopper.GetEndpoint("http"))
    .WithHttpHealthCheck("/health", endpointName: "http")
    .WaitFor(graphhopper);

// Add Waymarked Web frontend
var web = builder.AddProject<Projects.Waymarked_Web>("waymarked-web", launchProfileName: "http")
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
