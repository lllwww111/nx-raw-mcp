# NX Raw MCP

[English](README.md) | [简体中文](README.zh-CN.md)

A minimal MCP bridge for executing NXOpen C# statements in your open Siemens NX window. One execution tool also supports inspecting NXOpen DLL types, methods and parameters through reflection. Shared as-is, without an ongoing development commitment.

## Why a code execution interface

Our internal testing suggests that, at least with models at the **DeepSeek V4.1 Flash** capability level, an Agent can look up NXOpen APIs, write code and perform well on the tasks we tried without a catalog of predefined modeling tools. This observation informs our design; it is not a guarantee for every model or CAD task.

Initial API exploration typically costs more tokens. Within the same conversation, however, previously used APIs and commands can be reused while they remain in context, reducing repeated lookups. We also observed that too many predefined tools can sometimes misdirect an Agent or constrain how it combines native APIs.

Developing and maintaining NX-specific tools is expensive. Instead of continually expanding a fixed tool catalog, we prefer to explore experience-driven self-improvement: let the Agent investigate, then capture validated commands and working procedures as documents or skills that can be read on demand. This repository provides such references, not a background system that automatically generates or updates skills.

A related perspective is Zach Dive's Adam blog post, [The Bitter Lesson of AI CAD](https://adam.new/blog/bitter-lesson-ai-cad) (May 24, 2026). It describes moving away from specialized CAD abstractions toward code generation and visual feedback as general models improve.

For a programmable interface such as NXOpen, many CAD operations can already be expressed as code. We therefore choose to reuse a general coding Agent and keep the connection layer simple, rather than build an elaborate CAD-specific Agent or tool system.

## 1. Build

You need Windows x64, your own NX installation with NXOpen .NET, Python and a stdio MCP client. The current build and GUI demonstration used NX 2406 and Python 3.12. Other NX versions are not verified; this script targets .NET Framework, not the newer .NET 8 runtime.

Download the source and run from the repository directory, supplying your NX installation path. No `UGII_BASE_DIR` environment variable is required.

```powershell
python -m venv .venv
.\.venv\Scripts\python.exe -m pip install -r requirements.txt
.\build.ps1 -NxRoot 'D:\Siemens\NX2406'
```

The Windows .NET Framework compiler references your installed `NXBIN/managed` assemblies and produces **`build/NxRawMcp.dll`**. NX binaries and licenses are not included.

## 2. Load in NX

Open NX, press **Ctrl+U**, and select the DLL. The NX information window should show:

```text
NX Raw MCP: Listening on http://127.0.0.1:20790/
```

Use the [five-screenshot guide](docs/load-in-nx.zh-CN.md) (Chinese UI) to add a toolbar button and unload the plugin. No automatic-startup installer or NX configuration changes are included.

The plugin uses the current work part in the window where it was loaded, without launching a headless process. NX is usable between calls; a running journal occupies the UI thread until it returns. Only one NX session can listen on this port at a time.

## 3. Connect the MCP client

Use [mcp.example.json](mcp.example.json), replacing both paths with your actual absolute paths. The client starts `native_bridge.py`; the DLL must already be loaded in NX.

```text
MCP client --stdio--> Python bridge --local HTTP--> DLL in NX
```

## Execute code

The only tool is `nx_exec_csharp(code)`. For example:

```json
{"code": "result = workPart == null ? \"No work part\" : workPart.Name;"}
```

Send C# statements, without `using`, a class declaration or `Main`. Available variables are `session`, `ufSession`, `workPart`, `displayPart` and `result`. The result is converted to a string. Local variables disappear between calls; model changes remain. After changing the work part, read `session.Parts.Work` again.

Responses contain `status`, a string `result` or `error`, and sometimes a native `warning`. Success means execution returned without a reported error, not geometric correctness. Updates, checks, undo marks, recovery and saving are explicit caller responsibilities. The 180-second HTTP timeout does not cancel NX execution; inspect the state before repeating a modifying operation.

The listener binds to `127.0.0.1` but has no authentication or filesystem sandbox. Code has the NX process's permissions. Use trusted local clients; do not expose or forward the port.

## DLL lookup and optional references

DLL lookup uses the same tool, not an additional tool:

```csharp
var type = typeof(NXOpen.Features.BlockFeatureBuilder);
result = type.GetMethod("SetOriginAndLengths").ToString();
```

This reads the already-loaded NXOpen assembly, so no DLL path is needed. Reflection provides signatures, not full API usage instructions.

- [examples/](examples/): short C# call bodies and DLL inspection examples.
- [Common calls](skills/nx-raw/references/common-calls.md) and [DLL lookup](skills/nx-raw/references/dll-inspection.md): optional references (Chinese).
- [skills/nx-raw/](skills/nx-raw/SKILL.md): optional skill for compatible clients. Nothing is installed automatically.

See [SOURCE.md](SOURCE.md) for scope and observed checks. Code is shared under the [MIT license](LICENSE).
