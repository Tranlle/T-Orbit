# TranbokTools

TranbokTools 是一个基于 **WPF + .NET 10** 构建的桌面工具集，面向 Tranbok 相关项目的开发辅助场景。

当前项目以**插件化工具容器**为核心，已经内置数据库迁移管理能力，并预留了后续扩展更多工具插件的基础设施。

## 项目定位

这个项目不是一个单一功能应用，而是一个可扩展的开发工具宿主，目标是：

- 统一承载多个开发辅助插件
- 提供一致的桌面端交互体验
- 降低数据库迁移、配置管理、插件切换等高频操作成本
- 为后续工具能力扩展提供稳定的插件注册和视图承载框架

## 当前能力

### 1. 插件化宿主框架

项目提供了基础的插件系统：

- 通过 `IPlugin` 定义插件契约
- 通过 `PluginManager` 统一注册、启用、排序和缓存插件视图
- 主窗口在启动时注册插件，并将插件渲染到左侧菜单与主内容区

当前启动入口在：

- `Tranbok.Tools/MainWindow.xaml.cs`
- `Tranbok.Tools/Infrastructure/PluginManager.cs`
- `Tranbok.Tools/Infrastructure/IPlugin.cs`

### 2. 数据库迁移插件

当前已内置 `MigrationPlugin`，用于管理 EF Core Migration 相关操作。

主要功能包括：

- 管理多个数据库连接配置
- 支持 `SqlServer`、`PostgreSQL`、`MySQL`
- 浏览迁移列表
- 新增迁移
- 执行 `database update`
- 删除最后一条迁移（基于 `dotnet ef migrations remove`）
- 查看与编辑迁移文件内容
- 输出日志与状态反馈

相关文件：

- `Tranbok.Tools/Plugins/Migration/MigrationPlugin.cs`
- `Tranbok.Tools/Plugins/Migration/ViewModels/MigrationViewModel.cs`
- `Tranbok.Tools/Plugins/Migration/Services/MigrationService.cs`
- `Tranbok.Tools/Plugins/Migration/Views/MigrationView.xaml`

### 3. 主题与界面风格

项目实现了一套统一的暗色主题资源，覆盖了：

- 颜色体系
- 文本样式
- 按钮样式
- 输入框样式
- 下拉框样式
- 滚动条样式
- 列表与状态色

主题资源位于：

- `Tranbok.Tools/Themes/Dark.xaml`

## 技术栈

- **.NET 10**
- **WPF**
- **MVVM 风格 ViewModel 组织方式**
- **EF Core CLI 集成**
- **本地文件系统工作区生成与迁移文件扫描**

项目文件：

- `Tranbok.Tools/Tranbok.Tools.csproj`

关键配置：

- `TargetFramework`: `net10.0-windows`
- `UseWPF`: `true`
- `Nullable`: `enable`

## 项目结构

```text
TranbokTools/
├─ Tranbok.Tools/
│  ├─ Infrastructure/          # 插件系统、命令、基础 MVVM 支持
│  ├─ Plugins/
│  │  └─ Migration/            # 数据库迁移插件
│  ├─ Themes/                  # 主题资源
│  ├─ ViewModels/              # 主程序视图模型
│  ├─ Views/                   # 主程序页面
│  ├─ MainWindow.xaml*         # 主窗口
│  └─ App.xaml*                # 应用入口
├─ Tranbok.Tools.slnx
└─ README.md
```

## 迁移插件工作方式

数据库迁移插件当前采用“工具工作区”模式运行 EF Core 命令。

大体流程是：

1. 读取当前选中的数据库配置
2. 构造工作区项目与 DesignTimeFactory
3. 调用 `dotnet ef` 执行 `migrations list / add / remove / database update`
4. 扫描并展示迁移文件
5. 将迁移内容加载到右侧编辑区供查看或修改

这套模式的优势是：

- 不直接污染业务项目
- 可以为不同数据库类型与不同 `DbContext` 提供隔离环境
- 便于统一管理迁移输出与日志

## 已实现的界面模块

当前主界面主要包含：

- 左侧菜单导航
- 插件主区域切换
- 插件管理页
- 设置页
- 数据库迁移页

其中迁移页包含：

- 配置列表
- 数据库类型展示区
- 迁移文件选择区
- 文件编辑区
- 日志 / 状态区

## 适用场景

TranbokTools 目前更适合以下场景：

- Tranbok 相关项目的数据库迁移维护
- 多数据库配置切换与迁移验证
- 作为内部开发工具平台继续扩展更多插件

## 运行方式

### 环境要求

- Windows
- .NET 10 SDK
- 可用的 `dotnet ef`
- 对应项目可正常构建

### 启动项目

```bash
dotnet build Tranbok.Tools.slnx
```

或直接使用 Visual Studio / Rider 打开解决方案运行 `Tranbok.Tools`。

## 设计特点

### 插件视图缓存

插件视图创建后会被缓存，避免频繁切换时重复构建界面。

### 工具型 UI 风格

整体界面偏向内部开发工具，而不是面向外部用户的营销型产品，强调：

- 信息密度
- 操作直达
- 日志可见性
- 暗色主题下的长时间使用体验

### 可扩展性优先

当前虽然只有一个主要插件，但整体结构已经具备继续扩展的基础：

- 插件注册机制已存在
- 插件管理界面已存在
- 主题系统已统一
- 主程序容器结构已稳定

## 当前状态评估

从代码结构看，这个项目已经具备一个**可持续演进的内部工具平台雏形**，但仍处在以迁移插件为主的阶段。

当前优势：

- 主体结构清晰
- 插件机制已经成型
- 数据库迁移场景覆盖较完整
- UI 风格已有统一基础

当前仍值得继续完善的方向：

- 工作区生成与 EF 迁移命名空间冲突处理
- 迁移工作区生命周期管理
- 配置持久化与异常处理细节
- 插件扩展规范文档
- 更多工具插件落地

## 后续可以扩展的方向

- SQL 脚本生成与导出
- 迁移差异预览
- 多环境连接管理
- 项目级配置模板
- 代码生成类工具插件
- API 调试 / 配置查看类插件

## 许可证

本仓库包含 `LICENSE` 文件，请以仓库中的许可证内容为准。

## 代码参考入口

如果你想快速理解项目，建议按下面顺序阅读：

1. `Tranbok.Tools/MainWindow.xaml.cs`
2. `Tranbok.Tools/Infrastructure/IPlugin.cs`
3. `Tranbok.Tools/Infrastructure/PluginManager.cs`
4. `Tranbok.Tools/Plugins/Migration/MigrationPlugin.cs`
5. `Tranbok.Tools/Plugins/Migration/ViewModels/MigrationViewModel.cs`
6. `Tranbok.Tools/Plugins/Migration/Services/MigrationService.cs`
7. `Tranbok.Tools/Themes/Dark.xaml`

---

如果后续你计划把 TranbokTools 作为长期维护的工具平台，建议下一步优先补齐：

- 插件开发规范
- 工作区/迁移命名约束
- README 中的截图与使用示例
- 发布与版本管理流程
