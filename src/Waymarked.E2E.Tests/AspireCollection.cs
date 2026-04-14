namespace Waymarked.E2E.Tests;

using Xunit;

/// <summary>
/// xunit collection that shares a single <see cref="AspireFixture"/> instance
/// across all E2E test classes. This avoids booting two separate Aspire stacks
/// (one for route tests, one for auth tests) which would double infrastructure
/// cost and risk port collisions.
/// </summary>
[CollectionDefinition("Aspire")]
public sealed class AspireCollection : ICollectionFixture<AspireFixture> { }
