namespace GraphPlugin.NanoCad.Runtime;

public sealed record IntegrationTestResult(
    string Name,
    bool Passed,
    string? Error = null);