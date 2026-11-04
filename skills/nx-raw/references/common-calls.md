# 常见 NXOpen C# 调用

把需要的片段作为 `nx_exec_csharp` 的 `code` 参数提交，不用一次执行整份文档。这里的代码用于查阅和改写，不是额外的服务端工具。按实际路径、对象和单位调整；保存、改参数和新建几何会产生副作用。

本文片段在整理时只做语法与 NXOpen 程序集编译检查，没有重新启动 NX 验证运行行为。

## 查看当前零件

```csharp
result = workPart == null
    ? "No work part is open."
    : workPart.Name + "\n" + workPart.FullPath
        + "\nUnits: " + workPart.PartUnits.ToString();
```

连接的是加载 DLL 的 NX 窗口，读取它当前的工作零件；这个窗口也可能没有打开零件。

## 打开已有零件或总装

```csharp
NXOpen.PartLoadStatus loadStatus = null;
try
{
    var opened = session.Parts.OpenBaseDisplay(@"C:\NX-work\assembly.prt", out loadStatus);
    var text = new System.Text.StringBuilder();
    text.AppendLine(opened.FullPath);
    if (loadStatus != null)
    {
        text.AppendLine("Unloaded parts: " + loadStatus.NumberUnloadedParts);
        for (int i = 0; i < loadStatus.NumberUnloadedParts; i++)
            text.AppendLine(loadStatus.GetPartName(i) + ": " + loadStatus.GetStatusDescription(i));
    }
    result = text.ToString();
}
finally
{
    if (loadStatus != null) loadStatus.Dispose();
}
```

打开总装不保证所有原型全部加载。检查返回的加载状态；需要操作组件时再按任务确认它的原型是否已加载。文件必须在执行 NX 的机器上可访问。

## 保存当前零件

```csharp
var part = session.Parts.Work;
if (part == null)
    throw new System.InvalidOperationException("No work part is open.");
NXOpen.PartSaveStatus saveStatus = null;
try
{
    saveStatus = part.Save(
        NXOpen.BasePart.SaveComponents.False,
        NXOpen.BasePart.CloseAfterSave.False);
    if (saveStatus != null && (saveStatus.NumberUnsavedParts > 0 || saveStatus.NumberUnsavedObjects > 0))
        throw new System.InvalidOperationException("NX reported unsaved parts or objects.");
    result = part.FullPath;
}
finally
{
    if (saveStatus != null) saveStatus.Dispose();
}
```

这里使用现有保存路径，不负责“另存为”，也不保存所有修改过的组件。保存总装及组件需要另行按任务处理，不能从这个返回值推断整个装配树都已保存。

## 列出当前零件的体

```csharp
if (workPart == null)
    throw new System.InvalidOperationException("No work part is open.");
var text = new System.Text.StringBuilder();
foreach (NXOpen.Body body in workPart.Bodies)
    text.AppendLine("Tag=" + body.Tag + " Name=" + body.Name + " Solid=" + body.IsSolidBody);
result = text.ToString();
```

这里列出当前工作零件的体，不代表遍历了总装所有组件。返回的 Tag 是当前会话信息，不是跨文件重开后仍有效的永久标识。

## 列出当前零件的特征及错误

```csharp
if (workPart == null)
    throw new System.InvalidOperationException("No work part is open.");
var text = new System.Text.StringBuilder();
foreach (NXOpen.Features.Feature feature in workPart.Features)
{
    text.AppendLine("Tag=" + feature.Tag + " Type=" + feature.FeatureType + " Name=" + feature.Name);
    foreach (string message in feature.GetFeatureErrorMessages())
        text.AppendLine("  ERROR: " + message);
}
result = text.ToString();
```

特征没有报告错误不代表满足全部尺寸或装配要求，还要按用户关心的结果做相应查询。

## 读取表达式

```csharp
if (workPart == null)
    throw new System.InvalidOperationException("No work part is open.");
var text = new System.Text.StringBuilder();
foreach (NXOpen.Expression expression in workPart.Expressions)
    text.AppendLine(expression.Name + " = " + expression.RightHandSide);
result = text.ToString();
```

表达式可能是公式而不是数字。查阅单位或编辑方式时，先确认表达式类型和本机 DLL 的签名。

## 修改已确认的表达式并显式更新

```csharp
if (workPart == null)
    throw new System.InvalidOperationException("No work part is open.");
var expression = workPart.Expressions.FindObject("p1");
var mark = session.SetUndoMark(NXOpen.Session.MarkVisibility.Visible, "Edit expression");
try
{
    workPart.Expressions.Edit(expression, "25");
    int errors = session.UpdateManager.DoUpdate(mark);
    if (errors != 0)
        throw new System.InvalidOperationException("NX update reported " + errors + " errors.");
    result = expression.Name + " = " + expression.RightHandSide;
}
catch
{
    session.UndoToMark(mark, null);
    throw;
}
```

先把 `p1` 和 `25` 换成已查明且符合任务的表达式与值。这里的更新和失败恢复是片段自己显式调用的，不是执行器自动提供的；更新失败时不能仅靠再次赋值判断已恢复。这个片段也不会保存文件。

## 创建一个简单方块

```csharp
if (workPart == null)
    throw new System.InvalidOperationException("Open or create a part first.");
var builder = workPart.Features.CreateBlockFeatureBuilder(null);
try
{
    builder.SetOriginAndLengths(new NXOpen.Point3d(0, 0, 0), "20", "10", "12.5");
    var feature = builder.CommitFeature();
    result = feature.FeatureType + " Tag=" + feature.Tag;
}
finally
{
    builder.Destroy();
}
```

尺寸按当前零件单位解释。这个片段会新增几何，不保存，也没有加失败回滚；先确认要操作的工作零件。

## 读取顶层组件位姿

```csharp
if (workPart == null)
    throw new System.InvalidOperationException("No work part is open.");
var root = workPart.ComponentAssembly.RootComponent;
var text = new System.Text.StringBuilder();
if (root == null)
{
    text.AppendLine("No assembly root.");
}
else
{
    foreach (NXOpen.Assemblies.Component component in root.GetChildren())
    {
        NXOpen.Point3d origin;
        NXOpen.Matrix3x3 rotation;
        component.GetPosition(out origin, out rotation);
        text.AppendLine(component.Name + " Tag=" + component.Tag);
        text.AppendLine("  Origin: " + origin.X + ", " + origin.Y + ", " + origin.Z);
        text.AppendLine("  Rotation: " + rotation.Xx + ", " + rotation.Xy + ", " + rotation.Xz
            + "; " + rotation.Yx + ", " + rotation.Yy + ", " + rotation.Yz
            + "; " + rotation.Zx + ", " + rotation.Zy + ", " + rotation.Zz);
    }
}
result = text.ToString();
```

只读取根组件的直接子组件，不能把这个结果当作已经遍历所有嵌套装配或检查了装配约束。位姿数值的解释要结合当前装配上下文；更深层的世界位姿需要按层级处理。
