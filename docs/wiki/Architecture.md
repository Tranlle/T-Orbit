# 架构概览

## 分层结构

Tranbok.Tools 按照从底到顶的依赖方向组织成五层，每层只能向下依赖：

```
┌─────────────────────────────────────────────────────┐
│                   Plugin.*                          │  具体插件实现层
│  (Settings / PluginManager / Migration / 自定义...)  │
├─────────────────────────────────────────────────────┤
│                   Designer                          │  统一设计系统层
│      (主题 · 控件 · 对话框 · 布局 · 调色板)           │
├────────────────────┬────────────────────────────────┤
│        Core        │        Plugin.Core              │  宿主服务层 / 插件契约层
│  (插件目录·偏好·    │  (IPlugin · BasePlugin ·       │
│   变量存储·Shell)   │   PluginDescriptor · 模型)     │
├────────────────────┴────────────────────────────────┤
│                     App                             │  宿主入口层
│          (DI配置 · 插件注册 · 主窗口)                │
└─────────────────────────────────────────────────────┘
```

---

## 各模块职责

### `Tranbok.Tools.App`

桌面宿主入口。

- 初始化 Avalonia 应用与 DI 容器
- 配置 `IServiceCollection`：注册 Core 服务、Designer 服务、内置插件单例
- 同步启动内置插件（`PluginManagerPlugin`、`SettingsPlugin`）
- 调用 `IPluginDiscoveryService.LoadAsync` 扫描外部插件目录
- 创建主窗口 `MainWindow`

关键文件：`App.axaml.cs` · `ViewModels/MainViewModel.cs`

---

### `Tranbok.Tools.Core`

宿主共享服务层，所有插件通过 `PluginContext.Services` 访问这些服务。

| 服务 | 接口 | 职责 |
|---|---|---|
| 插件目录 | `IPluginCatalogService` | 注册、查询已加载插件；检测重复 ID 与命名规范 |
| 插件发现 | `IPluginDiscoveryService` | 扫描目录、反射实例化、调用生命周期 |
| 应用偏好 | `IAppPreferencesService` | 读写 `app-preferences.json` |
| 插件变量 | `IPluginVariableService` | 读写 `plugin-variables.json`；回退到元数据默认值 |
| Shell | `IAppShellService` | 暴露宿主名称、工作区路径等 Shell 信息 |

持久化文件：

| 文件 | 内容 |
|---|---|
| `app-preferences.json` | 字体方案等应用级偏好 |
| `plugin-variables.json` | 用户配置的插件变量键值对 |

---

### `Tranbok.Tools.Designer`

统一 UI 设计系统，所有插件共用同一套视觉规范。

**主题系统**
- `IThemeService`：切换深色/浅色、调色板、字体
- `BuiltInThemePaletteProvider`：内置调色板
- `JsonThemePaletteProvider`：从 `themes/*.json` 加载自定义调色板
- `ThemePaletteRegistry`：统一注册表

**通用控件库**

| 控件 | 用途 |
|---|---|
| `ToolPageLayout` | 插件页面的标准三区布局（Header / Body / Footer） |
| `SectionCard` | 带标题、描述、左侧强调条的内容卡片 |
| `FormField` | 统一的表单字段容器（Label + 描述 + 输入控件） |
| `SplitWorkspace` | 左右分栏布局 |
| `EmptyState` | 空状态占位 |
| `LogViewer` | 日志输出视图 |
| `StatusBadge` | 状态标签徽章 |
| `PluginCard` | 插件展示卡片 |

**对话框服务** (`IDesignerDialogService`)
- `ShowConfirmAsync` — 确认对话框
- `ShowPromptAsync` — 输入对话框
- `ShowSheetAsync` — 自定义内容 Sheet

---

### `Tranbok.Tools.Plugin.Core`

插件契约层，是插件与宿主之间的唯一约定边界。

**核心接口**

```csharp
public interface IPlugin : IAsyncDisposable
{
    PluginDescriptor Descriptor { get; }
    ValueTask InitializeAsync(PluginContext context, CancellationToken ct = default);
    ValueTask StartAsync(CancellationToken ct = default);
    ValueTask StopAsync(CancellationToken ct = default);
}

// 插件提供 Avalonia Control 主视图
public interface IVisualPlugin
{
    Control GetMainView();
}

// 插件提供页头动作按钮
public interface IPluginHeaderActionsProvider
{
    IReadOnlyList<PluginHeaderAction> GetHeaderActions();
}
```

**生命周期状态机**

```
Discovered → Loaded → Running → Stopping → Loaded
                                           ↓
                                        Unloaded
```

| 状态 | 触发时机 |
|---|---|
| `Discovered` | 实例创建后 |
| `Loaded` | `InitializeAsync` 完成后 |
| `Running` | `StartAsync` 完成后 |
| `Stopping` | `StopAsync` 执行中 |
| `Unloaded` | `DisposeAsync` 完成后 |

**插件上下文**

`PluginContext` 在 `InitializeAsync` 时注入，携带：
- `PluginId` — 当前插件 ID
- `BaseDirectory` — 插件所在目录
- `Services` — DI 容器（`IServiceProvider`）
- `HostEnvironment` — 宿主版本、平台、API 版本
- `IsolationMode` — 加载隔离策略
- `Properties` — 宿主传入的扩展属性

---

## 数据流示意

```
用户启动应用
    │
    ▼
App.OnFrameworkInitializationCompleted()
    │  ├─ ConfigureServices()       注册所有 DI 服务
    │  ├─ RegisterBuiltInPlugins()  初始化内置插件
    │  └─ DiscoveryService.Load()   扫描外部插件目录
    │
    ▼
MainWindow 加载 → MainViewModel
    │  └─ IPluginCatalogService.Plugins  绑定侧边导航
    │
    ▼
用户切换插件
    │  └─ MainViewModel.SelectedPlugin 变化
    │       └─ IVisualPlugin.GetMainView()  渲染插件内容
    │
    ▼
插件通过 PluginContext.Services 访问宿主服务
    ├─ IPluginVariableService.GetValue()  读取环境变量
    ├─ IThemeService                      访问主题
    └─ IDesignerDialogService             弹出对话框
```

---

## 设计原则

- **宿主不感知具体插件**：`App` 只知道 `IPlugin` 接口，不引用任何具体插件类
- **插件间完全隔离**：插件之间不应直接相互引用，共享能力通过 `Core` 服务传递
- **早失败**：插件 ID 格式错误、ID 重复均在注册阶段抛出异常，不会等到运行时才暴露
- **设计系统集中**：所有 UI 规范在 `Designer` 层统一定义，插件只消费不重新实现
