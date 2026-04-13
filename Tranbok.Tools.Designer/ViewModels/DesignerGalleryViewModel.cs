using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using Tranbok.Tools.Designer.Models;

namespace Tranbok.Tools.Designer.ViewModels;

public partial class DesignerGalleryViewModel : ObservableObject
{
    [ObservableProperty] private string textValue = "Tranbok Tools";
    [ObservableProperty] private string? selectedEnvironment = "开发";
    [ObservableProperty] private DateTimeOffset? selectedDate = DateTimeOffset.Now;
    [ObservableProperty] private bool featureEnabled = true;
    [ObservableProperty] private object? selectedMode;
    [ObservableProperty] private ObservableCollection<object> selectedTags = ["Avalonia", ".NET 10"];
    [ObservableProperty] private string selectSearchText = string.Empty;

    public ObservableCollection<string> EnvironmentOptions { get; } = ["开发", "测试", "生产"];
    public ObservableCollection<DesignerOptionItem> ModeOptions { get; } =
    [
        new DesignerOptionItem { Key = "std", Label = "标准模式", Description = "默认业务模式", Value = "标准模式" },
        new DesignerOptionItem { Key = "pro", Label = "增强模式", Description = "打开更多��力", Value = "增强模式" },
        new DesignerOptionItem { Key = "ro", Label = "只读模式", Description = "仅查看，不允许变更", Value = "只读模式" }
    ];
    public ObservableCollection<object> TagOptions { get; } = ["Avalonia", ".NET 10", "MVVM", "Plugin", "Designer"];
    public ObservableCollection<string> ValidationMessages { get; } = ["项目名称不能为空", "至少选择一个标签"];
    public ObservableCollection<DesignerOptionItem> RadioOptions { get; } =
    [
        new DesignerOptionItem { Key = "std", Label = "标准模式", Description = "默认业务模式", Value = "标准模式" },
        new DesignerOptionItem { Key = "pro", Label = "增强模式", Description = "打开更多能力", Value = "增强模式" },
        new DesignerOptionItem { Key = "ro", Label = "只读模式", Description = "仅查看，不允许变更", Value = "只读模式" }
    ];

    public ObservableCollection<DesignerControlStateItem> ControlStateMatrix { get; } =
    [
        new() { Group = "输入", Name = "DesignerTextBox", State = "默认", Description = "支持前后缀、说明、清空按钮和校验提示" },
        new() { Group = "输入", Name = "DesignerSelect", State = "搜索/空状态", Description = "支持搜索入口、空结果文案和键盘下拉" },
        new() { Group = "选择", Name = "DesignerRadioGroup", State = "模板项", Description = "支持自定义 ItemTemplate 渲染" },
        new() { Group = "选择", Name = "DesignerMultiSelect", State = "标签移除", Description = "已选项支持单独移除" },
        new() { Group = "对话框", Name = "DesignerConfirmDialog", State = "危险态", Description = "支持图标、说明和危险操作确认" }
    ];

    public ObservableCollection<string> GalleryNotes { get; } =
    [
        "所有控件都继承统一主题 Token，可全局切换深浅主题。",
        "输入和选择控件统一支持 Label/Description/ValidationMessage。",
        "对话框体系统一使用结果模型返回，不再裸返回 bool/string。"
    ];

    public DesignerGalleryViewModel()
    {
        selectedMode = ModeOptions[0];
    }
}

public sealed class DesignerControlStateItem
{
    public string Group { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}
