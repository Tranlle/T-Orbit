# Tranbok.Tools.Plugin.KeyMap — 实施计划

> 作者：Claude Sonnet 4.6 · 日期：2026-04-15

---

## 一、功能概述

新增一个名为 `Tranbok.Tools.Plugin.KeyMap` 的内置插件，负责：

1. **全局快捷键分发**：MainWindow 捕获 `KeyDown` 事件，调用 `IKeyMapService.Dispatch(...)` 统一分发。
2. **快捷键注册**：App 层和各插件通过 `IKeyMapService.Register(...)` 注册命名动作及其默认键绑定。
3. **用户自定义**：用户可在 KeyMap 插件页面修改键绑定、启用/禁用单个快捷键；配置持久化到 `keymap-bindings.json`。
4. **统一管理 UI**：分左右双栏布局，左侧分组列表（按所属插件分类）+ 右侧详情/编辑面板，风格与 Settings / PluginManager 页面完全统一。

---

## 二、架构层次归属

```
┌──────────────────────────────────────────────────────┐
│  Plugin.KeyMap  (新建)                                │  UI + 管理页面
├──────────────────────────────────────────────────────┤
│  Designer                                            │  复用现有控件（SplitWorkspace、SectionCard 等）
├──────────────────────────────────────────────────────┤
│  Core  (扩展)                                        │  IKeyMapService + 持久化
│  Plugin.Core                                         │
├──────────────────────────────────────────────────────┤
│  App   (修改)                                        │  MainWindow 监听键盘；注册内置快捷键；注册插件
└──────────────────────────────────────────────────────┘
```

**关键约束**：`Tranbok.Tools.Core` 不依赖 Avalonia，因此 `IKeyMapService` 只使用字符串格式的键名（`"Ctrl+K"`），Avalonia 类型的转换在 App 层完成。

---

## 三、键名格式规范

采用规范化字符串：`[修饰符+]键名`

- 修饰符按固定顺序：`Ctrl`、`Alt`、`Shift`、`Meta`（出现哪个写哪个）
- 键名使用 Avalonia `Key` 枚举的 `.ToString()` 值（首字母大写，如 `K`、`F5`、`Delete`、`Space`）
- 示例：`"Ctrl+K"`、`"Ctrl+Shift+N"`、`"Alt+F4"`、`"F5"`、`"Ctrl+,"`

---

## 四、需修改 / 新建的文件清单

### 4.1 Tranbok.Tools.Core（扩展）

| 文件 | 操作 | 说明 |
|------|------|------|
| `Models/KeyMapEntry.cs` | 新建 | 运行时快捷键条目（含 Handler） |
| `Models/KeyMapStore.cs` | 新建 | JSON 持久化模型（仅存 Id、CustomKey、IsEnabled） |
| `Services/IKeyMapService.cs` | 新建 | 服务接口 |
| `Services/KeyMapService.cs` | 新建 | 服务实现（注册、分发、持久化） |
| `DependencyInjection/ToolHostCoreServiceCollectionExtensions.cs` | 修改 | 注册 `IKeyMapService` 为单例 |

### 4.2 Tranbok.Tools.App（修改）

| 文件 | 操作 | 说明 |
|------|------|------|
| `Views/MainWindow.axaml.cs` | 修改 | 构造注入 `IKeyMapService`；`KeyDown` 事件 → `Dispatch` |
| `Views/MainWindow.axaml` | 修改 | 注入 ctor 参数（改为 DI 构造） |
| `App.axaml.cs` | 修改 | 注册 `KeyMapPlugin` 单例；在 `RegisterBuiltInPluginsAsync` 中初始化；注册内置快捷键 |

### 4.3 Tranbok.Tools.Plugin.KeyMap（全新项目）

```
Tranbok.Tools.Plugin.KeyMap/
├── Tranbok.Tools.Plugin.KeyMap.csproj
├── KeyMapPlugin.cs
├── KeyMapPluginMetadata.cs
├── ViewModels/
│   ├── KeyMapViewModel.cs
│   └── KeyMapBindingViewModel.cs
└── Views/
    ├── KeyMapView.axaml
    ├── KeyMapView.axaml.cs
    ├── KeyCaptureBox.axaml           ← 自定义按键捕获控件
    └── KeyCaptureBox.axaml.cs
```

### 4.4 解决方案

| 文件 | 操作 |
|------|------|
| `Tranbok.Tools.slnx` | 添加 `KeyMap` 项目引用 |
| `Tranbok.Tools.App/Tranbok.Tools.App.csproj` | 添加 `KeyMap` 项目引用 |

---

## 五、数据模型设计

### KeyMapEntry（运行时）

```csharp
public sealed class KeyMapEntry
{
    public string Id { get; init; }           // "tranbok.app:toggle-nav"
    public string PluginId { get; init; }     // "tranbok.app"
    public string PluginName { get; init; }   // "应用" / "设置"
    public string Name { get; init; }         // "折叠/展开导航"
    public string Description { get; init; } // 可选说明
    public string DefaultKey { get; init; }  // "Ctrl+B"
    public string? CustomKey { get; set; }   // 用户覆盖，null = 使用默认
    public bool IsEnabled { get; set; }
    public Action? Handler { get; init; }    // 不序列化
    public string EffectiveKey => CustomKey ?? DefaultKey;
}
```

### KeyMapStore（持久化）

```csharp
public sealed class KeyMapStore
{
    public List<KeyMapStoreEntry> Entries { get; set; } = [];
}

public sealed class KeyMapStoreEntry
{
    public string Id { get; set; } = string.Empty;
    public string? CustomKey { get; set; }
    public bool IsEnabled { get; set; } = true;
}
```

---

## 六、IKeyMapService 接口设计

```csharp
public interface IKeyMapService
{
    IReadOnlyList<KeyMapEntry> Entries { get; }

    // 注册一条快捷键（幂等：重复注册同 Id 覆盖旧值）
    void Register(
        string id,
        string pluginId,
        string pluginName,
        string name,
        string description,
        string defaultKey,
        Action handler);

    // 从 MainWindow 调用，分发按键；返回 true 表示已处理
    bool Dispatch(string keyString);

    // 加载持久化覆盖（应用启动后调用）
    void Load();

    // 保存用户覆盖到文件
    void Save();

    // 重置单条到默认（传 null 表示全部重置）
    void Reset(string? id = null);
}
```

---

## 七、UI 设计

### 总体布局

使用 `SplitWorkspace` 实现左右分栏：

```
┌──────────────────┬──────────────────────────────────┐
│  搜索框           │  [选中条目详情]                   │
│  ─────────────── │  名称：折叠/展开导航               │
│  ▸ 应用 (3)      │  所属：应用                       │
│    折叠/展开导航  │  说明：切换侧边导航栏折叠状态        │
│    切换到插件管理  │                                  │
│    切换到设置      │  ─────────────────────────────── │
│  ▸ Promptor (1)  │  当前绑定    [Ctrl + B]           │
│    生成 Prompt    │             [KeyCaptureBox]       │
│                  │                                  │
│                  │  [重置默认]  [已启用 ●]            │
└──────────────────┴──────────────────────────────────┘
```

### 左侧列表

- 分组折叠头（PluginName + 条目数量徽章）
- 每个条目显示：名称 + 有效键绑定（右对齐徽章）+ 禁用时置灰
- 搜索框过滤（名称 / 键绑定）

### 右侧详情

- `SectionCard` 包裹
- 显示名称、所属插件、说明
- `KeyCaptureBox`：点击后进入捕获状态，下次按键即记录为新绑定
- 启用/禁用 `ToggleSwitch`
- "重置默认" 按钮

### 页头动作（`IPluginHeaderActionsProvider`）

- `重置全部` — 调用 `IKeyMapService.Reset()`
- `保存` (Primary) — 调用 `IKeyMapService.Save()`

---

## 八、内置快捷键（应用层注册）

在 `App.axaml.cs` 的 `RegisterBuiltInPluginsAsync` 之后注册：

| Id | 名称 | 默认键 | 动作 |
|----|------|--------|------|
| `tranbok.app:toggle-nav` | 折叠/展开导航 | `Ctrl+B` | `MainViewModel.ToggleNavigationCommand` |
| `tranbok.app:goto-settings` | 跳转到设置 | `Ctrl+,` (逗号) | 激活 Settings 导航项 |
| `tranbok.app:goto-plugin-manager` | 跳转到插件管理 | `Ctrl+Shift+P` | 激活 PluginManager 导航项 |
| `tranbok.app:goto-keymap` | 跳转到快捷键管理 | `Ctrl+K` | 激活 KeyMap 导航项 |

---

## 九、KeyCaptureBox 控件设计

自定义 Avalonia `UserControl`，内部一个 `TextBox`（只读显示），点击后：
1. 显示"请按下快捷键..."提示
2. 捕获下一次 `PreviewKeyDown`（不传递）
3. 将 Avalonia Key 转为规范字符串写回 `BindableValue`（`AvaloniaProperty`）
4. 失去焦点或按 Escape 则取消

---

## 十、插件元数据

```csharp
public override string Id      => "tranbok.keymap";
public override string Name    => "快捷键";
public override string Version => "1.0.0";
public override string Description => "管理和自定义全局快捷键绑定。";
public override string Author  => "Tranbok";
public override string Icon    => "Keyboard";
public override string Tags    => "system,keyboard,builtin";
```

排序值：`97`（PluginManager=99，Settings=100）

---

## 十一、实施顺序

```
Step 1  Core 层新增模型和服务（无 Avalonia 依赖）
Step 2  Core DI 注册
Step 3  App.axaml.cs 修改（注册插件 + 内置快捷键）
Step 4  MainWindow.axaml.cs 修改（KeyDown 分发）
Step 5  新建 Tranbok.Tools.Plugin.KeyMap 项目（csproj + slnx）
Step 6  KeyMapPluginMetadata + KeyMapPlugin
Step 7  KeyMapBindingViewModel + KeyMapViewModel
Step 8  KeyCaptureBox 控件
Step 9  KeyMapView（AXAML + code-behind）
Step 10 构建验证
```

---

## 十二、风险与注意事项

1. **键冲突检测**：注册时检测 `EffectiveKey` 是否与已有条目重复，重复时记录警告但不抛异常（允许用户手动解决）。
2. **Avalonia 焦点**：`KeyDown` 在某些输入控件（TextBox）中会被消费，需在 `MainWindow` 的 `AddHandler(KeyDownEvent, ..., RoutingStrategies.Tunnel)` 以隧道模式监听，并在目标为输入控件时跳过分发。
3. **线程安全**：`IKeyMapService.Dispatch` 在 UI 线程调用，`Handler` 也在 UI 线程执行，无需额外同步。
4. **持久化时机**：仅在用户点击"保存"时写盘，不自动保存，符合 Settings 插件的一致体验。
5. **Reset 操作**：`Reset(null)` 全量重置需在 UI 层弹出确认对话框（`IDesignerDialogService.ShowConfirmAsync`）。
```
