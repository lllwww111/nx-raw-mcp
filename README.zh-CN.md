# NX Raw MCP

[English](README.md) | [简体中文](README.zh-CN.md)

让 Agent 在你打开的 NX 窗口里执行 NXOpen C# 代码。只提供一个执行入口，也可以用它反射查询 NXOpen DLL 的类型、方法和参数。按现状分享，不承诺持续开发维护。

## 1. 下载并编译

需要 Windows x64、本机 NX（含 NXOpen .NET 和相应许可）、Python，以及支持 stdio MCP 的客户端。当前编译和有界面操作验证使用 NX 2406、Python 3.12。其他版本尚未验证；本脚本面向 .NET Framework，不面向新版 .NET 8 运行环境。

下载源码，在仓库目录运行，填上自己的 NX 安装目录即可，不需要设置 `UGII_BASE_DIR`：

```powershell
python -m venv .venv
.\.venv\Scripts\python.exe -m pip install -r requirements.txt
.\build.ps1 -NxRoot 'D:\Siemens\NX2406'
```

脚本使用 Windows 自带的 .NET Framework 编译器，引用本机 NX 的 `NXBIN/managed` 中的程序集，生成 **`build/NxRawMcp.dll`**。仓库不附带 NX DLL、安装包或许可。

## 2. 在 NX 中加载

打开 NX，按 **Ctrl+U**，选择编译得到的 DLL。信息窗口显示下面这一行，表示开始监听：

```text
NX Raw MCP: Listening on http://127.0.0.1:20790/
```

也可以设置启动按钮。添加按钮和卸载插件见这份[五张截图教程](docs/load-in-nx.zh-CN.md)。MVP 不安装自启动，也不自动修改 NX 配置。

操作的是加载 DLL 的那个 NX 窗口及其当前工作零件，不另开无头进程。两次调用之间可以正常使用窗口；单次代码执行期间会占用 NX 主线程。同一端口只能由一个 NX 会话监听。

## 3. 连接 MCP

参考 [mcp.example.json](mcp.example.json)，把 Python 和 `native_bridge.py` 的路径换成自己的绝对路径。客户端负责启动 Python 桥接；NX 中的 DLL 需要先加载。

```text
Agent 客户端 → MCP（stdio）→ Python 桥接 → 本机 HTTP → NX 中的 DLL
```

## 使用执行入口

只有 **`nx_exec_csharp(code)`** 一个工具。例如：

```json
{"code": "result = workPart == null ? \"No work part\" : workPart.Name;"}
```

直接传 C# 语句，不写 `using`、类声明或 `Main`。可使用 `session`、`ufSession`、`workPart`、`displayPart`、`result`。`result` 转成字符串返回，不会自动序列化成对象。

局部变量不跨调用保留，模型修改会保留。更换工作零件后，同一次代码内需要重新读 `session.Parts.Work`。

返回包括 `status`、`result` 或 `error`，以及可能的原生 `warning`。执行成功不代表模型正确；更新、检查、撤销标记、恢复和保存都由调用方按需显式执行。HTTP 超时为 180 秒，不会取消 NX 中的执行，超时后不要直接重复修改操作。

服务只监听本机 `127.0.0.1`，没有认证或文件访问沙箱。代码拥有 NX 进程的权限，只连接可信的本机客户端，不对外暴露或转发端口。

## 查 DLL 和可选 skill

查 DLL 也走同一个入口，不额外配工具。例如：

```csharp
var type = typeof(NXOpen.Features.BlockFeatureBuilder);
result = type.GetMethod("SetOriginAndLengths").ToString();
```

这里读取 NX 已加载的程序集，不需要再找 DLL 路径。反射能查接口签名，但不等于完整的使用说明。

- [examples/](examples/)：简短调用代码和 DLL 查询示例。
- [常用调用](skills/nx-raw/references/common-calls.md)、[DLL 查询](skills/nx-raw/references/dll-inspection.md)：供 Agent 按需查看的文档。
- [skills/nx-raw/](skills/nx-raw/SKILL.md)：可选 skill；支持 skill 的客户端可以自行安装，不自动配置。

范围与已做检查见 [SOURCE.md](SOURCE.md)。代码采用 [MIT 许可证](LICENSE)。
