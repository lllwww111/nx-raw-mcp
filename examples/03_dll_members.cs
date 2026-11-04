// Inspect public properties, methods, constructors and parameter names.
// Change the type name and keyword to narrow the output.
string typeName = "NXOpen.PartCollection";
string keyword = "Open";
var type = typeof(NXOpen.Session).Assembly.GetType(typeName, true);
var flags = System.Reflection.BindingFlags.Public
    | System.Reflection.BindingFlags.Instance
    | System.Reflection.BindingFlags.Static
    | System.Reflection.BindingFlags.DeclaredOnly;
var text = new System.Text.StringBuilder();
text.AppendLine(type.FullName);
foreach (var property in type.GetProperties(flags))
{
    if (property.Name.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0)
        text.AppendLine("property " + property.ToString());
}
foreach (var method in type.GetMethods(flags))
{
    if (method.Name.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) < 0)
        continue;
    text.AppendLine("method " + method.ToString());
    foreach (var parameter in method.GetParameters())
        text.AppendLine("  " + parameter.ParameterType + " " + parameter.Name
            + (parameter.IsOut ? " [out]" : ""));
}
foreach (var constructor in type.GetConstructors(flags))
    text.AppendLine("constructor " + constructor.ToString());
result = text.ToString();
