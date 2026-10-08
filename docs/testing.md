# Integration test workflow

The nanoCAD integration test assembly exposes two primary user workflows.

## 1. Current-document regression

Load these assemblies from `artifacts/plugin/<Configuration>`:

- `GraphPlugin.Nanocad.dll`
- `GraphPlugin.Nanocad.IntegrationTests.dll`
- `GraphPlugin.Native.dll`

Run:

```text
GRAPHTESTS
```

The command automatically runs the C# integration suite, the edge ERASE/UNDO scenario,
the vertex cascade ERASE/UNDO scenario, the attachment ERASE/UNDO scenario, the native
C++ integration suite, and the staged C++/C# interop scenarios.

The harness uses an internal continuation command to preserve real nanoCAD command
boundaries. ERASE processing is therefore still handled by `GraphDatabaseWatcher` on
`CommandEnded`, and UNDO is executed through the nanoCAD command stack rather than by
calling repository methods directly.

Do not run other commands while `GRAPHTESTS` is progressing through its queued steps.

## 2. Persistence regression

Run:

```text
GRAPHTESTS_PERSISTENCE
```

The first invocation prepares both the graph/settings persistence scenario and the
attachment persistence scenario in the active DWG. Then:

1. save the DWG;
2. close it;
3. open the same DWG again;
4. run `GRAPHTESTS_PERSISTENCE` again.

The second invocation verifies the restored graph topology, runtime index, edge style,
settings and attachment XRecords, prints one combined report, and removes the test
objects from the currently opened document.

The command refuses to verify while the original nanoCAD `Document` instance is still
active, which protects against accidentally checking persistence without a close/open
cycle.

## Legacy staged commands

The old user-facing prepare/erase/verify command wrappers were removed after the two
regression workflows became available. Diagnostic probe commands remain separate because
they are interactive debugging tools rather than regression scenarios.
