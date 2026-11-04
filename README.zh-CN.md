# NX Raw MCP

[English](README.md) | [简体中文](README.zh-CN.md)

一份现有代码的归档分享：让 MCP 客户端把 NXOpen C# 代码发进同一个常驻的 Siemens NX 会话执行，并取回结果或报错。也保留通过 C# 反射查询本机 NXOpen DLL 的能力。

按现状提供，不承诺持续开发、维护或技术支持。

## 工作方式

```text
你的 MCP 客户端 → native_bridge.py → 本机 HTTP → NX 中的 RawExec
```

只有一个操作 NX 的 MCP 入口：`nx_exec_csharp(code)`。具体查询、建模、装配、保存，都由调用方写代码完成。没有专用建模工具，没有附带 Agent、模型 API 或自动建模流程。查 DLL 也走这个入口，不另起一个服务。

这一版通过启动脚本打开独立的无头 NX 会话，不连接你已经打开的 NX 窗口。只要这个进程还在，多次调用就操作同一会话中的模型。

## 环境

- Windows x64、本机合法安装且具备相应许可的 Siemens NX；需有 NXOpen .NET 和 `NXBIN/run_dotnet_nxopen.exe`。
- Windows 的 .NET Framework C# 编译器：`%WINDIR%/Microsoft.NET/Framework64/v4.0.30319/csc.exe`。
- Python，以及能启动 stdio MCP 服务的客户端。

原执行路径使用 NX 2406。整理时查看的本机 Python 环境为 Python 3.12、FastMCP 3.1.0、Requests 2.34.2；后两项的直接依赖版本记录在 `requirements.txt`，不是完整依赖锁。其他版本未声明兼容。仓库不附带 NX 安装包、NX DLL、许可或 API 手册。

## 安装和启动

在本目录打开 PowerShell，替换成你自己的 NX 安装路径：

```powershell
python -m venv .venv
.\.venv\Scripts\python.exe -m pip install -r requirements.txt
.\build.ps1 -NxRoot 'C:\Program Files\Siemens\NX2406'
```

然后在一个单独的终端启动 NX：

```powershell
.\start.ps1 -NxRoot 'C:\Program Files\Siemens\NX2406' -Port 20790
```

保持终端运行。看到 `RAW_EXEC_READY 20790` 才表示执行器开始监听。原代码只接受 20700–20799 范围内的端口，选一个未占用的即可。NX 许可或原生初始化失败时可能到不了这一步，需要查看终端错误。

把 `mcp.example.json` 中的服务配置放进你的 MCP 客户端，将示例中的两个路径改成当前目录和虚拟环境的绝对路径。如果更换端口，也要同步修改 `NX_RAW_ENDPOINT`。客户端负责启动 Python 桥接，NX 仍由前面的启动脚本单独启动；断开 MCP 不会自动关闭 NX。

原执行器没有关闭接口。使用结束后，通过本机进程管理方式结束这次独立启动；不要按进程名批量结束 NX。关闭终端或按 Ctrl+C 是否连带退出全部子进程取决于启动器行为，这份归档没有新增进程清理管理器。

## 调用

调用 `nx_exec_csharp`，传入：

```json
{"code": "result = 6 * 7;"}
```

返回形态：

```json
{"status": "success", "result": "42", "warning": ""}
```

`warning` 由 NX 返回，可能为空字符串或 null。报告执行失败时返回 `status: "error"` 和 `error`，原生 journal 错误还带 `warning`。连接失败、HTTP 失败也可能直接表现为 MCP 调用异常。

传入的是 C# 语句，不要写 `using`、类声明或 `Main`。可直接使用这些变量：

| 变量 | 每次调用开始时的值 |
| --- | --- |
| `session` | 当前 NX Session |
| `ufSession` | 当前 UF Session |
| `workPart` | 当前工作零件，可能为 null |
| `displayPart` | 当前显示零件，可能为 null |
| `result` | 输出变量，初始为 null，最后转换成字符串返回 |

局部 C# 变量不跨调用保留，模型修改会保留。同一次代码里若更换工作零件，要重新读取 `session.Parts.Work`；初始提供的 `workPart` 不会自动重新赋值。

每次请求在 NX 入口线程串行执行：生成临时 journal → 调用 `JournalManager.PlayDotNetJournal` → 读取结果或报错 → 清理本次临时传输文件。

## 查 DLL

把对应示例文件的内容放进 `code`：

| 示例 | 内容 |
| --- | --- |
| [01_session.cs](examples/01_session.cs) | 读取当前工作零件名称、路径 |
| [02_dll_types.cs](examples/02_dll_types.cs) | 按关键词找 NXOpen 公共类型 |
| [03_dll_members.cs](examples/03_dll_members.cs) | 查方法、重载、参数名称和公共属性 |
| [04_dll_enum.cs](examples/04_dll_enum.cs) | 查枚举成员名称 |
| [05_new_part.cs](examples/05_new_part.cs) | 修改示例路径后新建一个未保存的零件 |

查询的是使用者本机 NX 安装提供的 DLL 元数据，不需要附带 DLL 或手册。反射显示接口签名，不能代替使用说明，也不保证某个操作在当前状态可执行。查询 UF 类型时，把程序集入口换为 `typeof(NXOpen.UF.UFSession).Assembly`。

## 可选文档和 skill

常见操作放在 [NXOpen 调用示例](skills/nx-raw/references/common-calls.md)，DLL 查询放在 [反射查询说明](skills/nx-raw/references/dll-inspection.md)。Agent 可以按需查看普通 Markdown 文档，再自由编写代码，不需要增加服务端命令。

支持 skill 的客户端也可以把完整的 [skills/nx-raw](skills/nx-raw/SKILL.md) 文件夹复制到自己的 skill 目录。这是可选项，仓库不会自动安装。skill 只说明怎样使用现有 `nx_exec_csharp` 和按需查阅参考资料，不封装成新的建模工具。

## 现有边界

- 不自动更新模型、检查特征健康、创建撤销标记、回滚或保存；需要时由调用方在代码里做。异常可能留下部分修改。
- `success` 只表示 journal 没有报告执行错误，不代表尺寸、约束或几何正确。
- 调用会等待执行结束或客户端超时。Python HTTP 超时为 180 秒，不代表 NX 到时停止；超时后代码可能仍在执行，不能直接重发修改操作。
- 原请求限制为 100,000 个 C# 字符。返回的 `result` 是字符串，不是持久变量、对象注册表，也不是自动捕获的控制台输出。
- 仅监听本机 `127.0.0.1`，没有认证。代码拥有 NX 进程的权限，不是安全沙箱，也没有工作目录访问限制。只连接可信客户端，不对外暴露或转发端口。
- 没有异步任务接口、由 MCP 自动启动 NX、GUI 接入或自动恢复。

## 这次整理的范围

核心 `RawExec.cs` 和 `native_bridge.py` 保持原实现。只整理可配置的构建、启动路径，补充依赖、MCP 配置、DLL 查询示例和说明。原研究脚本、模型密钥、CAD 数据、编译产物及专有依赖均不纳入。

文件范围见 [SOURCE.md](SOURCE.md)，代码采用 [MIT 许可证](LICENSE)。整理期间没有重新启动 NX；静态检查、编译与真实 NX 运行兼容性分别记录。
