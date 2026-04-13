var builder = DistributedApplication.CreateBuilder(args);

builder.AddDockerComposeEnvironment("env");

var db = builder.AddPostgres("db")
    .AddDatabase("waymarked");

// In CI, use the pre-built image (graph already on disk) to avoid a 5-min rebuild.
var prebuiltImage = Environment.GetEnvironmentVariable("GRAPHHOPPER_PREBUILT_IMAGE");

IResourceBuilder<ContainerResource> graphhopper;
if (!string.IsNullOrEmpty(prebuiltImage))
{
    graphhopper = builder.AddContainer("graphhopper", prebuiltImage)
        .WithImagePullPolicy(ImagePullPolicy.Never);
}
else
{
    graphhopper = builder.AddDockerfile("graphhopper", "../../infra/graphhopper")
        .WithLifetime(ContainerLifetime.Persistent)
        .WithDeveloperCertificateTrust(trust: false);
}

graphhopper
    .WithBindMount("../../infra/graphhopper/config.yml", "/data/config.yml", isReadOnly: true)
    .WithBindMount("../../infra/graphhopper/data", "/data", isReadOnly: false)
    .WithHttpEndpoint(targetPort: 8989, name: "http")
    .WithHttpHealthCheck("/info", endpointName: "http")
    .WithEnvironment("JAVA_OPTS", string.IsNullOrEmpty(prebuiltImage) ? "-Xmx6g -Xms512m" : "-Xmx2g -Xms256m");

// CI config-ci.yml builds the graph without elevation; disable elevation requests to match.
var api = builder.AddProject<Projects.Waymarked_Api>("waymarked-api", launchProfileName: "http")
    .WithReference(graphhopper.GetEndpoint("http"))
    .WithReference(db)
    .WithHttpHealthCheck("/health", endpointName: "http")
    .WaitFor(graphhopper)
    .WaitFor(db)
    .WithEnvironment("GRAPHHOPPER__ELEVATIONENABLED", string.IsNullOrEmpty(prebuiltImage) ? "true" : "false");

var web = builder.AddProject<Projects.Waymarked_Web>("waymarked-web", launchProfileName: "http")
    .WithReference(api)
    .WaitFor(api);

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

    // Expose on 8081 so the shared Caddy instance can reverse-proxy to it.
    web.PublishAsDockerComposeService((resource, service) =>
    {
        service.Ports.Add("8081:8080");
    });
}

builder.Build().Run();
