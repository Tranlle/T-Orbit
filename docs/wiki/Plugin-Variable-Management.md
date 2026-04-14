# 插件变量管理

插件变量管理是设置插件内置的一项功能，用于集中配置各插件所需的环境变量键值对。**宿主与设置插件不做任何校验**——声明、读取、校验与错误处理全部由各插件自行负责。

---

## 概念

| 概念 | 说明 |
|---|---|
| **变量定义** | 插件在元数据中声明的变量模板：键名、默认值、显示名、说明。来自 `PluginVariableDefinition`，存储于 `PluginDescriptor.VariableDefinitions` |
| **变量条目** | 用户在设置页实际配置的键值对。来自 `PluginVariableEntry`，持久化到 `plugin-variables.json` |
| **默认值回退** | 调用 `GetValue` 时，若用户未配置该条目，自动回退到插件元数据中声明的 `DefaultValue` |

---

## 插件侧：声明变量

在元数据类中重写 `VariableDefinitions`：

```csharp
public sealed class MyPluginMetadata : PluginBaseMetadata
{
    public override string Id => "com.example.my-plugin";
    // ...

    public override IReadOnlyList<PluginVariableDefinition> VariableDefinitions =>
    [
        new PluginVariableDefinition(
            Key:          "MY_API_ENDPOINT",
            DefaultValue: "https://api.example.com",
            DisplayName:  "API 地址",
            Description:  "调用外部服务的基础 URL"),

        new PluginVariableDefinition(
            Key:          "MY_API_KEY",
            DefaultValue: "",
            DisplayName:  "API 密钥",
            Description:  "鉴权密钥，留空则跳过鉴权"),
    ];
}
```

**字段说明**

| 字段 | 类型 | 说明 |
|---|---|---|
| `Key` | `string` | 变量键名，惯例使用全大写下划线格式（`MY_KEY`） |
| `DefaultValue` | `string` | 用户未配置时的回退值；可为空字符串 |
| `DisplayName` | `string` | 在设置页"快速填入"按钮上显示的名称；留空则显示 Key |
| `Description` | `string` | 在设置页变量列表中显示的说明文字 |

> **宿主不对声明做任何校验**。格式错误、必填检查等逻辑完全由插件自己在读取后处理。

---

## 插件侧：读取变量值

通过 `IPluginVariableService` 读取，在 `EnsureView` 或业务逻辑中通过 `Context.Services` 获取服务：

```csharp
var variableService = Context.Services.GetRequiredService<IPluginVariableService>();

// 优先返回用户存储的值，不存在时回退到 DefaultValue，均无则返回 null
string? endpoint = variableService.GetValue("com.example.my-plugin", "MY_API_ENDPOINT");
string? apiKey   = variableService.GetValue("com.example.my-plugin", "MY_API_KEY");

// 插件自行处理空值与校验
if (string.IsNullOrWhiteSpace(apiKey))
{
    // 提示用户、禁用功能、或使用无鉴权模式……
}
```

**`GetValue` 查找顺序**

```
1. plugin-variables.json 中是否有 PluginId == id && Key == key 的条目
         ↓ 有 → 返回用户存储的 Value
         ↓ 无
2. IPluginCatalogService 中找到该插件的 Descriptor.VariableDefinitions
         ↓ 找到匹配 Key → 返回 DefaultValue
         ↓ 未找到
3. 返回 null
```

---

## 用户侧：在设置页管理变量

打开**设置 → 插件变量管理**，可执行以下操作：

### 新建变量

点击右上角「新建变量」按钮，展开创建面板：

1. **所属插件**：从下拉列表中选择目标插件
2. **变量键**：手动输入键名；若所选插件已声明了变量，页面会显示快速填入建议按钮，点击可自动填入键名和默认值
3. **变量值**：输入实际值
4. 点击「添加」

> 若输入的 `插件 + 键名` 组合已存在，添加操作会更新该条目的值而非重复创建。

### 编辑已有变量

在变量列表中直接编辑「当前值」文本框，修改后点击顶部「保存设置」生效。

### 删除变量

点击变量行右侧的「×」按钮，删除后点击「保存设置」写入磁盘。

### 保存

插件变量的修改（新增、编辑、删除）在点击**「保存设置」**时统一写入 `plugin-variables.json`，与其他设置项一同保存。

---

## 持久化格式

变量存储在应用目录下的 `plugin-variables.json`：

```json
{
  "entries": [
    {
      "pluginId": "tranbok.migration",
      "key": "TRANBOK_DB_CONNECTION",
      "value": "Server=localhost;Database=MyDb;..."
    },
    {
      "pluginId": "com.example.my-plugin",
      "key": "MY_API_KEY",
      "value": "sk-xxxxxxxxxxxxxxxx"
    }
  ]
}
```

> **注意**：该文件以明文存储，请勿将含有生产密钥的 `plugin-variables.json` 提交到版本控制。建议在 `.gitignore` 中添加此文件。

---

## 注意事项

- 变量声明（`VariableDefinitions`）只是提示，不约束用户。用户可以为任何插件创建任意键
- 插件 ID 变更后（如从旧命名迁移到反向域名），已存储的条目因 ID 不匹配而失效，需在设置页手动重建
- `GetValue` 每次调用都从磁盘读取最新存档，适合低频配置读取；频繁调用的场景建议在插件初始化时一次性缓存所需值
