# MVP scope

Updated on 2026-11-04.

- `RawExec.cs`: DLL loaded in an interactive NX session. HTTP reception runs on a background thread; NX journal execution is dispatched through a hidden WinForms control onto the NX UI thread, using the existing GUI implementation's scheduling pattern. The raw journal execution body is retained from the original standalone executor, without automatic update, rollback or saving.
- `native_bridge.py`: one stdio MCP tool forwarding C# statements to local HTTP. Its description mentions reflection; no separate DLL lookup tool is registered.
- `build.ps1`: compile against an explicit `-NxRoot`, producing `build/NxRawMcp.dll`. No NX environment variable or runtime setting is changed. Optional `-OutputName` permits a fresh assembly name when avoiding a cached development build.
- `mcp.example.json`, dependencies and README files: basic client setup.
- `docs/`: five user-provided screenshots for adding a manual startup button and unloading the plugin. Screenshot paths are examples.
- `examples/` and `skills/nx-raw/`: optional C# examples and references, not server-side tools.

No headless launcher, automatic-startup installer, bundled Agent or model-provider integration is included. Research runners/results, CAD models, credentials, proprietary NX DLLs/manuals and compiled binaries are excluded. The MIT license covers the shared project code, not separately installed Siemens software.

## Observed checks

- Compiled x64 DLL against local NX 2406 assemblies with the .NET Framework compiler and `UGII_BASE_DIR` unset.
- The user loaded the revised DLL in an existing NX 2406 window. A development name (`NxRawMcp_v2.dll`) avoided the cached earlier assembly.
- A standalone MCP client discovered only `nx_exec_csharp`, created a separate 20 × 10 × 12.5 mm block, queried its bounding box, observed one body, six faces and no reported feature errors, and saved a non-empty native part.
- The same tool returned metadata for the installed NXOpen assembly, `CreateBlockFeatureBuilder` and `SetOriginAndLengths`, including parameter types and names.
- Earlier packaging checks compiled all 18 example/reference C# bodies against the local SDK and validated the optional skill structure.

These bounded demonstrations do not certify every API, NX version, unload/reload sequence or error path. No model-provider request or benchmark rerun was used for these checks.
