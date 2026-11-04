"""Standalone raw-code MCP: no production command router or response projection."""
import os
import requests
from fastmcp import FastMCP

mcp = FastMCP('raw-nx-csharp')

@mcp.tool
def nx_exec_csharp(code: str) -> dict:
    """Execute C# statements in NX; no using/class/Main.
    Variables: session, ufSession, workPart, displayPart, result.
    Locals do not persist between calls; native model changes do.
    This endpoint does NOT update, check features, create undo marks, undo,
    save, register objects, or inject state. Exceptions can leave partial changes.
    Perform any necessary native update, checking or recovery explicitly in code.
    status=success means only journal execution returned without a reported error.
    """
    response = requests.post(os.environ['NX_RAW_ENDPOINT'], json={'code': code}, timeout=180)
    response.raise_for_status()
    return response.json()

if __name__ == '__main__':
    mcp.run()
