using GraphPlugin.Nanocad.Runtime;
using HostMgd.EditorInput;

namespace GraphPlugin.Nanocad.IntegrationTests.Commands;

internal static class IntegrationTestCommandOutput
{
    public static void WriteResults(Editor editor, string title, IReadOnlyList<IntegrationTestResult> results)
    {
        editor.WriteMessage($"\n=== {title} ===");

        foreach (var result in results)
        {
            editor.WriteMessage(result.Passed ? $"\n[PASS] {result.Name}" : $"\n[FAIL] {result.Name}: {result.Error}");
        }

        var passed = results.Count(x => x.Passed);

        var failed = results.Count - passed;

        editor.WriteMessage(
            $"\n------------------------------"
                + $"\nPassed: {passed}"
                + $"\nFailed: {failed}"
                + $"\nTotal:  {results.Count}"
        );
    }
}
