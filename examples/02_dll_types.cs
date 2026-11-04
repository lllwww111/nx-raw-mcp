// Search the installed NXOpen assembly's public types without changing a model.
string keyword = "Extrude";
var assembly = typeof(NXOpen.Session).Assembly;
var text = new System.Text.StringBuilder();
text.AppendLine(assembly.FullName);
foreach (var type in assembly.GetExportedTypes())
{
    if (type.FullName.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0)
        text.AppendLine(type.FullName);
}
result = text.ToString();
