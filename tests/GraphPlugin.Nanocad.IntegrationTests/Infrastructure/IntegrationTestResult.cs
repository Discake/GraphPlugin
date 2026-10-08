namespace GraphPlugin.Nanocad.Runtime;

public sealed record IntegrationTestResult(string Name, bool Passed, string? Error = null);
