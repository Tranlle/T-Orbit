# 创建插件

本文以创建一个带 UI 页面、环境变量声明和页头按钮的完整插件为例，分步说明接入流程。

---

## 前置条件

- 已克隆并能构建 TranbokTools 仓库
- 了解 C# 与 Avalonia 基础（MVVM、UserControl）

---

## 第一步：创建项目

在解决方案目录下新建类库项目：

```bash
cd TranbokTools
dotnet new classlib -n Tranbok.Tools.Plugin.MyPlugin -f net10.0
```

编辑 `Tranbok.Tools.Plugin.MyPlugin.csproj`：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Tranbok.Tools.Plugin.Core\Tranbok.Tools.Plugin.Core.csproj" />
    <ProjectReference Include="..\Tranbok.Tools.Core\Tranbok.Tools.Core.csproj" />
    <ProjectReference Include="..\Tranbok.Tools.Designer\Tranbok.Tools.Designer.csproj" />
  </ItemGroup>
</Project>
```

将项目加入解决方案：

```bash
dotnet sln Tranbok.Tools.slnx add Tranbok.Tools.Plugin.MyPlugin/Tranbok.Tools.Plugin.MyPlugin.csproj
```

---

## 第二步：声明元数据

```csharp
// MyPluginMetadata.cs
using Tranbok.Tools.Plugin.Core.Base;
using Tranbok.Tools.Plugin.Core.Models;

namespace Tranbok.Tools.Plugin.MyPlugin;

public sealed class MyPluginMetadata : PluginBaseMetadata
{
    public static MyPluginMetadata Instance { get; } = new();

    // 必须遵循反向域名格式，违反则在 CreateDescriptor 时立即抛异常
    public override string Id      => "com.example.my-plugin";
    public override string Name    => "我的插件";
    public override string Version => "1.0.0";
    public override string Author  => "Your Name";
    public override string Icon    => "Star";           // Material Icon 名称
    public override string Tags    => "example,tools";

    // 可选：声明插件所需的环境变量（在设置页的「插件变量管理」中展示为建议键）
    public override IReadOnlyList<PluginVariableDefinition> VariableDefinitions =>
    [
        new PluginVariableDefinition(
            Key:         "MY_API_ENDPOINT",
            DefaultValue: "https://api.example.com",
            DisplayName: "API 地址",
            Description: "调用外部服务的基础 URL"),
        new PluginVariableDefinition(
            Key:         "MY_API_KEY",
            DefaultValue: "",
            DisplayName: "API 密钥",
            Description: "鉴权密钥，留空则跳过鉴权"),
    ];
}
```

---

## 第三步：实现 ViewModel

```csharp
// ViewModels/MyViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tranbok.Tools.Core.Services;

namespace Tranbok.Tools.Plugin.MyPlugin.ViewModels;

public sealed partial class MyViewModel : ObservableObject
{
    private readonly IPluginVariableService _variableService;

    [ObservableProperty]
    private string apiEndpoint = string.Empty;

    [ObservableProperty]
    private string statusMessage = "就绪";

    public IRelayCommand RefreshCommand { get; }

    public MyViewModel(IPluginVariableService variableService)
    {
        _variableService = variableService;

        // 读取插件变量（用户配置值优先，回退到元数据 DefaultValue）
        apiEndpoint = _variableService.GetValue("com.example.my-plugin", "MY_API_ENDPOINT")
                      ?? "https://api.example.com";

        RefreshCommand = new AsyncRelayCommand(DoRefreshAsync);
    }

    private async Task DoRefreshAsync()
    {
        StatusMessage = "加载中...";
        await Task.Delay(500); // 替换为实际逻辑
        StatusMessage = "完成";
    }
}
```

---

## 第四步：创建 View

```xml
<!-- Views/MyView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:Tranbok.Tools.Plugin.MyPlugin.ViewModels"
             xmlns:designer="using:Tranbok.Tools.Designer.Controls"
             x:Class="Tranbok.Tools.Plugin.MyPlugin.Views.MyView"
             x:DataType="vm:MyViewModel">
  <designer:ToolPageLayout>
    <designer:ToolPageLayout.BodyContent>
      <ScrollViewer>
        <StackPanel Spacing="20">

          <designer:SectionCard Title="我的插件"
                                Description="这是一个示例插件页面。">
            <designer:SectionCard.Body>
              <StackPanel Spacing="12">
                <designer:FormField Label="API 地址">
                  <designer:FormField.Body>
                    <TextBox Classes="form-control"
                             Text="{Binding ApiEndpoint, Mode=TwoWay}" />
                  </designer:FormField.Body>
                </designer:FormField>

                <TextBlock Classes="caption-muted"
                           Text="{Binding StatusMessage}" />
              </StackPanel>
            </designer:SectionCard.Body>
          </designer:SectionCard>

        </StackPanel>
      </ScrollViewer>
    </designer:ToolPageLayout.BodyContent>
  </designer:ToolPageLayout>
</UserControl>
```

```csharp
// Views/MyView.axaml.cs
using Avalonia.Markup.Xaml;

namespace Tranbok.Tools.Plugin.MyPlugin.Views;

public partial class MyView : Avalonia.Controls.UserControl
{
    public MyView() => AvaloniaXamlLoader.Load(this);
}
```

---

## 第五步：实现插件类

```csharp
// MyPlugin.cs
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Tranbok.Tools.Core.Services;
using Tranbok.Tools.Designer.Abstractions;
using Tranbok.Tools.Plugin.Core;
using Tranbok.Tools.Plugin.Core.Abstractions;
using Tranbok.Tools.Plugin.Core.Base;
using Tranbok.Tools.Plugin.MyPlugin.ViewModels;
using Tranbok.Tools.Plugin.MyPlugin.Views;

namespace Tranbok.Tools.Plugin.MyPlugin;

public sealed class MyPlugin : BasePlugin, IVisualPlugin, IPluginHeaderActionsProvider
{
    private MyView? _view;
    private MyViewModel? _viewModel;
    private IReadOnlyList<PluginHeaderAction>? _headerActions;

    public override PluginDescriptor Descriptor { get; } =
        CreateDescriptor<MyPlugin>(MyPluginMetadata.Instance);

    protected override ValueTask OnStartAsync(CancellationToken ct = default)
    {
        EnsureView();
        return ValueTask.CompletedTask;
    }

    public override Control GetMainView()
    {
        EnsureView();
        return _view!;
    }

    public IReadOnlyList<PluginHeaderAction> GetHeaderActions()
    {
        EnsureView();
        return _headerActions ?? [];
    }

    private void EnsureView()
    {
        if (_viewModel is not null) return;

        var services = Context.Services ?? throw new InvalidOperationException("Services unavailable.");
        var variableService = services.GetRequiredService<IPluginVariableService>();

        _viewModel = new MyViewModel(variableService);
        _headerActions =
        [
            new PluginHeaderAction("刷新", _viewModel.RefreshCommand, IsPrimary: true),
        ];
        _view = new MyView { DataContext = _viewModel };
    }

    protected override ValueTask OnDisposeAsync()
    {
        _view = null;
        _viewModel = null;
        _headerActions = null;
        return ValueTask.CompletedTask;
    }
}
```

---

## 第六步：接入宿主

### 方式 A：内置插件（源码注册）

在 `App.axaml.cs` 的 `ConfigureServices` 中注册：

```csharp
services.AddSingleton<MyPlugin.MyPlugin>();
```

在 `RegisterBuiltInPluginsAsync` 中初始化并注册：

```csharp
var myPlugin = services.GetRequiredService<MyPlugin.MyPlugin>();
var myContext = new PluginContext(
    MyPluginMetadata.Instance.Id,
    AppContext.BaseDirectory,
    services,
    hostEnvironment,
    PluginIsolationMode.None,
    new Dictionary<string, object?>());
await myPlugin.InitializeAsync(myContext, ct);
await myPlugin.StartAsync(ct);
catalog.Register(myPlugin, sort: 10);
```

### 方式 B：目录插件（推荐用于独立功能）

在 `.csproj` 中配置输出目录，构建后由宿主自动发现：

```xml
<PropertyGroup>
  <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
  <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
  <OutputPath>../Tranbok.Tools.App/bin/$(Configuration)/net10.0/plugins/MyPlugin/</OutputPath>
</PropertyGroup>
```

---

## 完整检查清单

- [ ] 元数据 `Id` 遵循反向域名格式（`dotnet build` 通过即确认）
- [ ] `PluginDescriptor` 通过 `CreateDescriptor<TPlugin>(Metadata.Instance)` 创建
- [ ] `EnsureView()` 是懒加载的（先判空再初始化）
- [ ] `OnDisposeAsync` 中清理了 `_view` / `_viewModel` / `_headerActions`
- [ ] 需要环境变量时，通过 `IPluginVariableService.GetValue(pluginId, key)` 读取
- [ ] 接入方式已选择（内置 or 目录），并完成对应配置
