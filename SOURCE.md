# Source and archive scope

Prepared on 2026-11-04 from the existing standalone raw executor.

## Existing implementation

- `RawExec.cs`: same C# execution source as the original `raw_exec_20260916/RawExec.cs`; line endings may differ.
- `native_bridge.py`: same MCP forwarding source as the original `raw_exec_20260916/native_bridge.py`; line endings may differ.

The executor creates a dedicated NX session with a serialized local HTTP listener and runs C# statement bodies through the native journal manager. The archive keeps its original port restriction and synchronous execution behavior.

## Packaging material

- `build.ps1`: retains the existing compiler and NX assembly references; replaces the local installation path with `-NxRoot` / `UGII_BASE_DIR` and puts output in `build/`.
- `start.ps1`: documents and wraps the existing `run_dotnet_nxopen.exe RawExec.exe <port>` launch command. It does not manage process ownership, attach to GUI NX, or introduce a worker service.
- `requirements.txt`: records the two direct Python package versions found in the existing environment during packaging, not a fresh-install certification or full lockfile.
- `mcp.example.json`: placeholder client configuration with no provider credentials.
- `examples/`: short C# call bodies, including native DLL reflection. The examples do not add MCP tools.
- `skills/nx-raw/`: optional, self-contained Agent guidance plus common-call and DLL-inspection references. Nothing is installed automatically and no additional server tools are registered.
- README files, `.gitignore` and the MIT `LICENSE`.

## Excluded

Research runners and results, model-provider integrations, hidden evaluators, dedicated modeling/assembly/fixture tools, commercial CAD models, local configuration, secrets, NX binaries, NX API documentation, compiled executables, Docker/Agent infrastructure and previous repository history.

Only the source files explicitly included in this directory are intended for sharing. The license applies to this project's code; it does not grant rights to Siemens software or any separately installed dependency.

No NX process or model-provider request was started for packaging. Any local static validation or compilation does not establish a new runtime-compatibility claim.

## Packaging checks on 2026-11-04

- Both core source files match the original bytes exactly.
- The executor builds against the locally installed NX 2406 managed assemblies with the .NET Framework compiler.
- All 18 C# example bodies across the example files and skill references compile against those assemblies. Compilation does not run their NX calls.
- PowerShell scripts parse, Python syntax and example JSON parse, and local Markdown links resolve.
- MCP stdio discovery returns only `nx_exec_csharp`. Forwarding of multiline Unicode code and success/error JSON was checked using a temporary fake local HTTP receiver, not NX.
- The optional skill passes its frontmatter/structure validator.
- A source-only file inventory was scanned for private machine paths, provider-key patterns and private-key blocks. No such matches were found; this is a targeted check, not a claim of universal security certification.

No new CAD experiment, provider request or real-NX acceptance was performed.
