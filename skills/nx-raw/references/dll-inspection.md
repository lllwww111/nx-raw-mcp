# 查本机 NXOpen DLL

以下代码作为 `nx_exec_csharp` 的 `code` 参数执行。反射查询不修改 CAD 模型，不需要分发 NX DLL，也不依赖单独的 API 检索服务。查询结果对应当前安装版本。

## 按关键词找公共类型

```csharp
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
```

要查 UF API，把程序集入口改成 `typeof(NXOpen.UF.UFSession).Assembly`。类型名字比较明确后，直接查该类型，不用反复列出整个程序集。

## 查方法、重载和参数

```csharp
string typeName = "NXOpen.PartCollection";
string keyword = "Open";
var type = typeof(NXOpen.Session).Assembly.GetType(typeName, true);
var flags = System.Reflection.BindingFlags.Public
    | System.Reflection.BindingFlags.Instance
    | System.Reflection.BindingFlags.Static;
var text = new System.Text.StringBuilder();
text.AppendLine(type.FullName);
foreach (var method in type.GetMethods(flags))
{
    if (method.Name.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) < 0)
        continue;
    text.AppendLine(method.ToString());
    foreach (var parameter in method.GetParameters())
        text.AppendLine("  " + parameter.ParameterType + " " + parameter.Name
            + (parameter.IsOut ? " [out]" : ""));
}
result = text.ToString();
```

要看属性，使用 `type.GetProperties(flags)`；要看构造函数，使用 `type.GetConstructors(flags)`。方法查询会包含继承成员，需要时增加 `BindingFlags.DeclaredOnly` 只看当前类型声明的成员。

## 从已有对象查实际类型

```csharp
if (workPart == null)
    throw new System.InvalidOperationException("No work part is open.");
result = workPart.Features.GetType().FullName;
```

拿到名字后可以继续查 `workPart.Features.GetType().GetMethods(...)`，无需猜测它属于哪个类型。

## 查枚举成员

```csharp
var type = typeof(NXOpen.Part.Units);
result = string.Join("\n", System.Enum.GetNames(type));
```

接口存在不代表操作在当前模型状态下可执行。反射也不提供完整的参数语义、许可条件或几何前提；结合任务、原生错误和使用者可访问的官方资料判断，不猜测业务包装接口。
