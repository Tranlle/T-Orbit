# 插件系统

## ID 命名约定

Tranbok.Tools 要求所有插件 ID 遵循**反向域名命名约定**，格式与 Java 包名、VS Code 扩展 ID、Android 应用包名一致。

### 规则

| 要求 | 示例（合法） | 示例（非法） |
|---|---|---|
| 全小写字母、数字、连字符 | `tranbok.settings` | `Tranbok.Settings` |
| 至少两段，以 `.` 分隔 | `com.example.tool` | `mytool` |
| 每段不能以连字符开头/结尾 | `my-company.my-plugin` | `my-company.-plugin` |

```
正则：^[a-z0-9][a-z0-9\-]*(\.[a-z0-9][a-z0-9\-]*)+$
```

### 内置插件 ID

| 插件 | ID |
|---|---|
| 设置 | `tranbok.settings` |
| 插件管理 | `tranbok.plugin-manager` |
| EF Core 迁移 | `tranbok.migration` |

### 第三方插件建议

使用你控制的域名（或 GitHub 用户名）作为命名空间：

```
io.github.yourname.plugin-name
com.yourcompany.tool-name
```

### 违规时机

ID 格式错误与重复注册均在**开发期**即可发现：

- `PluginBaseMetadata.ValidateId()` 在 `CreateDescriptor` 时调用 → 静态字段初始化阶段即抛出
- `PluginCatalogService.Register` 再做一道防线 → 运行时注册时校验

---

## 插件类型

### 内置插件

由 `App.axaml.cs` 直接实例化并注册，与宿主共享同一 DI 容器。适合平台级功能（设置、管理类）。

```csharp
// App.axaml.cs — ConfigureServices
services.AddSingleton<MyPlugin>();

// RegisterBuiltInPluginsAsync
var plugin = services.GetRequiredService<MyPlugin>();
await plugin.InitializeAsync(context, ct);
await plugin.StartAsync(ct);
catalog.Register(plugin, isBuiltIn: true, canDisable: false);
```

### 目录插件（外部插件）

构建产物放入 `<AppDir>/plugins/<PluginName>/` 后，宿主在启动时自动扫描并加载：

1. 递归查找 `*.dll`
2. 反射找到实现 `IPlugin` 的非抽象类型
3. `Activator.CreateInstance()` 实例化
4. 按顺序调用 `InitializeAsync` → `StartAsync` → 注册到目录

外部插件通过 `.csproj` 配置输出路径：

```xml
<PropertyGroup>
  <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
  <OutputPath>../Tranbok.Tools.App/bin/$(Configuration)/net10.0/plugins/MyPlugin/</OutputPath>
  <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
</PropertyGroup>
```

---

## 插件生命周期

```
[程序集加载]
     │
     ▼
Activator.CreateInstance()    →  State: Discovered
     │
     ▼
InitializeAsync(context)      →  State: Loaded
  PluginContext 注入，可访问 Services
     │
     ▼
StartAsync()                  →  State: Running
  插件开始工作，EnsureView() 通常在此调用
     │
     ▼
（用户禁用或应用关闭）
     │
     ▼
StopAsync()                   →  State: Stopping → Loaded
     │
     ▼
DisposeAsync()                →  State: Unloaded
```

> **注意**：当前版本"禁用插件"仅将该插件从宿主导航中移除（`IsEnabled = false`），不会真正调用 `StopAsync` 卸载程序集。

---

## 插件接口契约

### 基础接口

```csharp
public interface IPlugin : IAsyncDisposable
{
    PluginDescriptor Descriptor { get; }
    ValueTask InitializeAsync(PluginContext context, CancellationToken ct = default);
    ValueTask StartAsync(CancellationToken ct = default);
    ValueTask StopAsync(CancellationToken ct = default);
}
```

### 视图接口

实现 `IVisualPlugin` 后，宿主在用户切换到该插件时调用 `GetMainView()` 渲染内容区：

```csharp
// 来自 Tranbok.Tools.Designer
public interface IVisualPlugin
{
    Control GetMainView();
}
```

### 页头动作接口

实现 `IPluginHeaderActionsProvider` 后，宿主在页面顶部右侧渲染动作按钮（最多显示数个，最后一个 `IsPrimary = true` 的按钮使用强调色）：

```csharp
public interface IPluginHeaderActionsProvider
{
    IReadOnlyList<PluginHeaderAction> GetHeaderActions();
}

// 示例
_headerActions =
[
    new PluginHeaderAction("重置", _viewModel.ResetCommand),
    new PluginHeaderAction("保存", _viewModel.SaveCommand, IsPrimary: true),
];
```

---

## BasePlugin 基类

直接继承 `BasePlugin` 可获得生命周期管理、`PluginContext` 注入与 `CreateDescriptor` 工厂方法：

```csharp
public abstract class BasePlugin : IPlugin, IPluginViewProvider
{
    // 插件可访问的上下文（InitializeAsync 之后才可用）
    public PluginContext Context { get; }

    public PluginState State { get; }

    // 工厂方法：从元数据构建 PluginDescriptor，内部调用 ValidateId()
    protected static PluginDescriptor CreateDescriptor<TPlugin>(
        PluginBaseMetadata metadata,
        PluginLoadMode loadMode = PluginLoadMode.Lazy,
        PluginIsolationMode isolationMode = PluginIsolationMode.AssemblyLoadContext)
        where TPlugin : IPlugin;

    // 子类重写的生命周期钩子
    protected virtual ValueTask OnInitializeAsync(PluginContext context, CancellationToken ct);
    protected virtual ValueTask OnStartAsync(CancellationToken ct);
    protected virtual ValueTask OnStopAsync(CancellationToken ct);
    protected virtual ValueTask OnDisposeAsync();
}
```

---

## PluginDescriptor

`PluginDescriptor` 是插件的不可变身份标识，由 `CreateDescriptor` 从元数据构建：

```csharp
public sealed record PluginDescriptor(
    string Id,                // 反向域名 ID（如 tranbok.settings）
    string Name,              // 显示名称
    string Version,           // 版本字符串
    string EntryAssembly,     // 程序集名称
    string EntryType,         // 完整类型名
    string? Description,
    string? Author,
    string? Icon,             // Material Icon 名称
    string? Tags,             // 逗号分隔标签
    PluginLoadMode LoadMode,
    PluginIsolationMode IsolationMode,
    IReadOnlyList<PluginVariableDefinition>? VariableDefinitions);  // 见「插件变量管理」
```

---

## PluginCatalogService

`IPluginCatalogService` 是运行时插件注册表，提供：

```csharp
IReadOnlyList<PluginEntry> Plugins { get; }
IEnumerable<PluginEntry> EnabledPlugins { get; }

void Register(
    IPlugin plugin,
    bool enabledByDefault = true,
    int? sort = null,
    bool isBuiltIn = false,
    bool canDisable = true,
    string? builtInHint = null);

PluginEntry? Get(string id);
```

注册时执行两项校验：
1. ID 是否已存在（重复检测）
2. ID 是否包含 `.`（命名规范快速检查）

> 完整格式校验由 `PluginBaseMetadata.ValidateId()` 在更早的 `CreateDescriptor` 阶段完成。
