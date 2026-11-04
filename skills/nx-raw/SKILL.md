---
name: nx-raw
description: Operate a connected Siemens NX session with the nx_exec_csharp MCP tool, including inspecting local NXOpen DLL signatures through C# reflection. Use for native NXOpen queries and model changes through this raw bridge.
---

# NX raw code execution

Use the client's `nx_exec_csharp` tool (possibly prefixed by its MCP server name). It accepts C# statements and returns `status`, a string `result` or an `error`, and sometimes a native `warning`.

The journal already provides `session`, `ufSession`, `workPart`, `displayPart`, and `result`. Do not send a complete program with `using`, class declarations or `Main`. Use qualified names for additional namespaces. There is no object-ID registry and no automatic state injection.

Each request runs in the same dedicated NX session. C# locals disappear after a call; NX model changes remain. Re-query objects in later calls. If code changes the work part, read `session.Parts.Work` again within that call.

## References to read as needed

- For checking the session, opening/saving parts, listing bodies/features/expressions, changing an expression, creating a block, or reading component poses: [references/common-calls.md](references/common-calls.md).
- For finding an unfamiliar NXOpen type, overload, argument or enum: [references/dll-inspection.md](references/dll-inspection.md).

These are optional examples, not a tool allowlist or a complete API catalog. Adapt them to the user's task and installed NX version. If an API signature is uncertain, inspect the installed DLL before constructing the call. Reflection shows signatures, not engineering meaning or valid model conditions.

## Execution behavior

Inspect enough of the current native state to identify the intended part and objects. Submit requests sequentially. Assign concise observations to `result`; `Console.WriteLine` is not automatically captured. Check returned errors and warnings before issuing dependent changes.

The executor does not add updates, health checks, undo marks, rollback or saves. Add the native operations the task actually requires and destroy builders/status objects when finished. An exception can leave a partial edit. A successful journal is not evidence that geometry or constraints satisfy the user's request; query the relevant native facts after editing.

An HTTP timeout does not cancel NX execution. Do not repeat a modifying call merely because the response timed out; first establish whether it finished and inspect the resulting state. If that cannot be established, report the uncertainty and avoid duplicate edits.

Use only the models, output locations and operations authorized by the user. DLL lookup should target the local NXOpen/NXOpen.UF API metadata. The execution service itself is not a filesystem or code sandbox and has no authentication; the skill does not grant broader access or authority.
