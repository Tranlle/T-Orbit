# T-Orbit

基于 `.NET 10` 与 `Avalonia 12` 的插件化桌面工具宿主。

T-Orbit 的目标不是做单一业务应用，而是提供一个可扩展的桌面宿主：
- 宿主管理插件发现、加载、生命周期、导航、设置、诊断和持久化。
- 插件负责提供具体功能页面、工具能力和业务逻辑。
- 所有插件共享统一设计系统、主题机制、变量管理和宿主工具接口。

## 当前能力

- 插件化桌面宿主，支持内置插件和目录插件
- 插件生命周期管理，支持启动、停止、重启
- 每插件执行串行化，避免同一插件并发启停冲突
- 启动协调器与关闭协调器，减少入口阻塞并统一退出流程
- 应用级诊断通道，可展示启动、迁移、插件发现阶段的错误和告警
- 统一设置页，支持主题、字体、窗口行为和插件变量管理
- 快捷键系统，支持注册、覆盖、持久化和运行期刷新
- SQLite 持久化，统一保存应用偏好、快捷键绑定和插件变量
- 插件变量支持默认值、加密标记、运行期注入和校验提示
- 监控页支持插件状态查看、启停控制和诊断信息查看

## 内置与示例插件

- `torbit.keymap`
  用于查看和管理快捷键列表。

- `torbit.settings`
  用于管理主题、字体、关闭行为和插件变量。

- `torbit.monitor`
  用于查看插件状态、能力声明、最近错误和应用诊断。

- `torbit.migration`
  EF Core 迁移插件，支持默认连接串注入和多 Profile 配置。

- `torbit.promptor`
  外部插件示例。

## 快速开始

### 环境要求

- `.NET 10 SDK`
- 可运行 Avalonia 桌面应用的图形环境

### 构建

```bash
dotnet build TOrbit.slnx
```

### 运行

```bash
dotnet run --project TOrbit.App/TOrbit.App.csproj
```

## 项目结构

```text
T-Orbit/
├─ TOrbit.App/                 # 桌面宿主入口、主窗口、导航与启动流程
├─ TOrbit.Core/                # 核心服务：插件、存储、变量、快捷键、诊断
├─ TOrbit.Designer/            # 统一设计系统与主题
├─ TOrbit.Plugin.Core/         # 插件契约、基础模型、基类
├─ TOrbit.Plugin.KeyMap/       # 内置插件：快捷键管理
├─ TOrbit.Plugin.Settings/     # 内置插件：设置与变量管理
├─ TOrbit.Plugin.Monitor/      # 内置插件：插件监控与应用诊断
├─ TOrbit.Plugin.Migration/    # 外部插件：EF Core 迁移
├─ TOrbit.Plugin.Promptor/     # 外部插件：示例插件
├─ TOrbit.Core.Tests/          # 核心测试
└─ TOrbit.wiki/                # Wiki 文档
```

## 关键架构点

### 启动

- 主窗口先创建，避免大量初始化阻塞 UI 线程。
- 启动协调器异步执行：
  - 内置插件注册
  - 外部插件发现与加载
  - 插件变量注入
  - 快捷键覆盖加载

### 生命周期

- `PluginLifecycleService` 负责普通启停与重启。
- `PluginExecutionGate` 为每个插件提供独立串行化门控。
- 关闭流程由 `AppShutdownCoordinator` 统一接管，并复用相同门控，避免与运行中的启停操作并发冲突。

### 持久化

- 使用 `%APPDATA%/T-Orbit/t-orbit.db`
- 主要存储：
  - 应用偏好
  - 快捷键绑定
  - 插件变量

### 插件变量

- 插件通过 `PluginVariableDefinition` 声明变量元数据
- 设置页集中管理变量值
- 保存不会被变量校验阻塞
- 校验结果会以“插件级提醒”的形式显示在对应插件页面标题旁

### 诊断

- 存储迁移异常不再静默吞掉
- 插件发现/加载失败会进入应用诊断
- 监控页可直接查看诊断记录

## 开发插件

插件最小实现通常包括：

1. 元数据类
2. 插件类
3. ViewModel
4. View

基础模式如下：

```csharp
public sealed class MyPlugin : BasePlugin, IVisualPlugin
{
    public override PluginDescriptor Descriptor { get; } =
        CreateDescriptor<MyPlugin>(MyPluginMetadata.Instance);

    public override Control GetMainView()
    {
        EnsureView();
        return _view!;
    }
}
```

## 测试

```bash
dotnet test TOrbit.Core.Tests/TOrbit.Core.Tests.csproj
```

## 文档

仓库内 Wiki 位于 [TOrbit.wiki](https://github.com/Tranlle/T-Orbit/wiki/Home)。

## 许可证

以仓库中的 [LICENSE](./LICENSE) 为准。
