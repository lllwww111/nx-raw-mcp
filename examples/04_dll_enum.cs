// List an enum from the locally installed NXOpen DLL.
var type = typeof(NXOpen.Part.Units);
var text = new System.Text.StringBuilder();
text.AppendLine(type.FullName);
foreach (string name in System.Enum.GetNames(type))
    text.AppendLine(name);
result = text.ToString();
