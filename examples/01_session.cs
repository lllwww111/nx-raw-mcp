// Paste the contents into nx_exec_csharp(code).
// No model is created or changed.
result = workPart == null
    ? "NX session is ready; no work part is open."
    : "Work part: " + workPart.Name + "\nPath: " + workPart.FullPath;
