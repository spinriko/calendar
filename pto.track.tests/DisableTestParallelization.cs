using Xunit;

// Disable xUnit test parallelization to avoid testhost collisions and file-locking
[assembly: CollectionBehavior(DisableTestParallelization = true)]
