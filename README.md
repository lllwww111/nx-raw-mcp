# NX Raw MCP

[English](README.md) | [简体中文](README.zh-CN.md)

A small, archived MCP bridge for running NXOpen C# statements in one persistent Siemens NX session. It also supports inspecting the installed NXOpen DLL through C# reflection.

This is a source-code share of an existing implementation. It is provided as-is, with no ongoing development or support commitment.

## What it does

An MCP client sends C# statements to `nx_exec_csharp`. The Python bridge forwards them to a local NX process. NX runs the statements as a native journal and returns a result string, an error, and any native warning.

```text
Your MCP client -> native_bridge.py -> local HTTP -> RawExec in NX
```

There is one NX-facing MCP tool. There are no predefined modeling or assembly tools, no bundled Agent or model API, and no automatic modeling workflow. DLL inspection uses the same C# execution tool, not an additional service.

The included launcher starts a separate, headless NX session. It does not attach to an already-open NX GUI. Repeated calls use the same session while that process remains running.

## Requirements

- Windows x64 and a locally installed, licensed Siemens NX with NXOpen .NET and `NXBIN/run_dotnet_nxopen.exe`.
- The .NET Framework compiler at `%WINDIR%/Microsoft.NET/Framework64/v4.0.30319/csc.exe`.
- Python and an MCP client that can start a stdio server.

The existing execution path was used with NX 2406. The Python environment inspected during packaging was Python 3.12 with FastMCP 3.1.0 and Requests 2.34.2; these direct package versions are recorded in `requirements.txt`. Other versions are not claimed as supported. NX itself, its DLLs, licenses and API manuals are not included.

## Set up

Open PowerShell in this directory. Substitute your actual NX installation path:

```powershell
python -m venv .venv
.\.venv\Scripts\python.exe -m pip install -r requirements.txt
.\build.ps1 -NxRoot 'C:\Program Files\Siemens\NX2406'
```

Start NX in a dedicated terminal:

```powershell
.\start.ps1 -NxRoot 'C:\Program Files\Siemens\NX2406' -Port 20790
```

Keep that terminal open. `RAW_EXEC_READY 20790` means the executor is listening. The original executor only permits ports 20700–20799; choose an unused port in this range. NX licensing or native initialization can fail before the ready line appears. Read the terminal error rather than assuming the bridge started.

Copy the server entry in `mcp.example.json` into your MCP client's configuration. Replace both example paths with absolute paths to your checkout and its virtual environment. If you changed the port, change `NX_RAW_ENDPOINT` as well. The MCP client starts the Python bridge; `start.ps1` starts NX separately. Closing the MCP connection does not shut down NX.

This executor has no shutdown endpoint. End the dedicated launch through your local process controls when finished; never stop unrelated NX processes by name. Depending on launcher behavior, closing or interrupting the terminal may not terminate every child process. This archive includes no process-cleanup manager.

## Execute code

Call `nx_exec_csharp` with an argument such as:

```json
{"code": "result = 6 * 7;"}
```

Expected response shape:

```json
{"status": "success", "result": "42", "warning": ""}
```

`warning` comes from NX and may be empty or null. On a reported failure, the response contains `status: "error"` and `error`; native journal failures also include `warning`. HTTP or connection failures can instead surface as MCP tool exceptions.

The input is a C# statement body, not a complete program: do not include `using`, a class declaration or `Main`. Available variables are:

| Variable | Value at the start of each call |
| --- | --- |
| `session` | `NXOpen.Session.GetSession()` |
| `ufSession` | `NXOpen.UF.UFSession.GetUFSession()` |
| `workPart` | Current work part, possibly null |
| `displayPart` | Current display part, possibly null |
| `result` | Output object, initially null; converted to a string on return |

Local C# variables do not persist between calls. Native model changes do. Re-read `session.Parts.Work` in the same call if your code changes the work part: the initial `workPart` variable is a snapshot reference.

Requests run sequentially on the NX entry thread. The executor creates a temporary journal for each request, calls `JournalManager.PlayDotNetJournal`, reads the result/error files, and removes its temporary transport files.

## Inspect the NXOpen DLL

Paste an example's contents into the `code` argument:

| File | Purpose |
| --- | --- |
| [01_session.cs](examples/01_session.cs) | Read the current work-part name and path |
| [02_dll_types.cs](examples/02_dll_types.cs) | Find public types in the installed NXOpen assembly |
| [03_dll_members.cs](examples/03_dll_members.cs) | Read signatures, overloads and parameter names |
| [04_dll_enum.cs](examples/04_dll_enum.cs) | List enum member names |
| [05_new_part.cs](examples/05_new_part.cs) | Create a new unsaved part after replacing the example path |

Reflection reads metadata from the DLL supplied by your NX installation. It does not require distributing that DLL or an API documentation copy. It shows available signatures, not API usage instructions or a guarantee that an operation is valid. For UF types, start from `typeof(NXOpen.UF.UFSession).Assembly` instead of `typeof(NXOpen.Session).Assembly`.

## Optional Agent reference and skill

The [common NXOpen calls](skills/nx-raw/references/common-calls.md) and [DLL lookup notes](skills/nx-raw/references/dll-inspection.md) are plain Markdown references an Agent can read as needed. They cover session inspection, opening/saving files, bodies/features/expressions, a simple block and top-level component poses. The reference notes are in Chinese, with C# examples.

For clients that support skills, the self-contained folder [skills/nx-raw](skills/nx-raw/SKILL.md) can be copied into that client's skill directory. This is optional; no skill is installed automatically. The skill calls the existing `nx_exec_csharp` tool and does not add server-side commands. The examples are starting points, not a complete API or new native runtime validation.

## Behavior and limitations

- No automatic update, feature-health check, undo mark, rollback or save. Perform those explicitly in your C# when needed. An exception may leave partial model changes.
- `status: "success"` means the journal returned without a reported error. It says nothing about dimensions, constraints or geometric correctness.
- Calls block until execution finishes or the client times out. The Python HTTP timeout is 180 seconds; this is not an NX execution deadline. A timed-out call may still be executing. Do not blindly resubmit a modifying operation.
- The original request limit is 100,000 C# characters. `result` is a string, not an object registry, persistent C# context, or automatic stdout capture.
- The listener binds to `127.0.0.1`, has no authentication, and executes supplied code with the NX process's permissions. It is not a sandbox. Use trusted local clients only and do not expose or forward the port. The server does not confine model or filesystem access to a workspace.
- There is no async job API, automatic NX startup from MCP, GUI attachment, or automatic recovery.

## Source and packaging

`RawExec.cs` and `native_bridge.py` preserve the original execution implementation. Packaging adds configurable build/start paths, dependency metadata, configuration and reflection examples. Research runners, model credentials, CAD datasets, compiled binaries and proprietary dependencies are excluded.

See [SOURCE.md](SOURCE.md) for the file scope and [LICENSE](LICENSE) for the MIT license. Real NX was not launched again during packaging; static checks and compilation are separate from runtime compatibility.
