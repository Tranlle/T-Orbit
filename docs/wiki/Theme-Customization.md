# 自定义主题

Tranbok.Tools 的主题系统分为两层：

- **内置调色板**：编译到 `Designer` 程序集中，始终可用
- **自定义调色板**：运行时从 `themes/*.json` 目录加载，支持热添加

---

## 快速创建主题

在应用目录下的 `themes/` 文件夹中创建一个 `.json` 文件（文件名即为加载标识）：

```json
{
  "key": "my-dark-theme",
  "label": "我的深色主题",
  "description": "自定义深绿色配色方案",
  "baseVariant": "Dark",

  "accentBrush":             "#36CFC9",
  "accentForegroundBrush":   "#081B1A",
  "accentSubtleBrush":       "#0D2928",
  "accentSubtleForegroundBrush": "#5AEDE7",

  "backgroundBrush":         "#0B1212",
  "surfaceBrush":            "#121A1A",
  "surfaceElevatedBrush":    "#182323",
  "borderBrush":             "#274240",

  "textPrimaryBrush":        "#F1FBFA",
  "textSecondaryBrush":      "#A7C7C4",
  "textMutedBrush":          "#6B8B88",

  "badgeSuccessBackgroundBrush":  "#1B3028",
  "badgeSuccessForegroundBrush":  "#5ADE97",
  "badgeWarningBackgroundBrush":  "#322A18",
  "badgeWarningForegroundBrush":  "#F0C878",
  "badgeDangerBackgroundBrush":   "#351D22",
  "badgeDangerForegroundBrush":   "#F47482"
}
```

保存后重启应用，在**设置 → 高级主题**中即可选择该主题。

---

## 字段参考

### 元数据字段

| 字段 | 类型 | 说明 |
|---|---|---|
| `key` | string | 主题唯一标识，建议使用短横线小写格式 |
| `label` | string | 在 UI 下拉列表中显示的名称 |
| `description` | string | 在 UI 中显示的简短描述 |
| `baseVariant` | `"Dark"` \| `"Light"` | 基础变体，影响 Avalonia 原生控件的默认主题 |

### 强调色（Accent）

| 字段 | 作用 |
|---|---|
| `accentBrush` | 主强调色，用于按钮、激活态边框、Section 左侧条 |
| `accentForegroundBrush` | 强调色背景上的前景色（文字/图标） |
| `accentSubtleBrush` | 淡强调色背景，用于选中列表项等 |
| `accentSubtleForegroundBrush` | 淡强调色背景上的前景色 |

### 背景与表面

| 字段 | 作用 |
|---|---|
| `backgroundBrush` | 最底层窗口背景色 |
| `surfaceBrush` | 卡片（`.card`）等主要内容面板背景 |
| `surfaceElevatedBrush` | 悬浮面板（`.panel`）、输入框背景等次级表面 |
| `borderBrush` | 所有边框、分割线 |

### 文字

| 字段 | 作用 |
|---|---|
| `textPrimaryBrush` | 主要文字、标题 |
| `textSecondaryBrush` | 次要文字、字段标签（`.field-label`） |
| `textMutedBrush` | 辅助说明文字（`.caption-muted`） |

### 语义徽章

每种语义色包含背景（Background）和前景（Foreground）两个字段：

| 语义 | 背景字段 | 前景字段 | 用途 |
|---|---|---|---|
| 成功 | `badgeSuccessBackgroundBrush` | `badgeSuccessForegroundBrush` | 成功状态、完成标记 |
| 警告 | `badgeWarningBackgroundBrush` | `badgeWarningForegroundBrush` | 警告提示、内置插件标记 |
| 危险 | `badgeDangerBackgroundBrush` | `badgeDangerForegroundBrush` | 错误、删除等危险操作 |

---

## 内置主题调色板

| Key | 名称 | 基础变体 |
|---|---|---|
| `tranbok-dark` | Tranbok Dark | Dark |
| *(其他内置板由 BuiltInThemePaletteProvider 提供)* | | |

---

## 外部主题目录

默认加载路径为应用目录下的 `themes/`，可在设置页的「高级主题 → 自定义主题目录」字段修改。

仓库中包含一个示例：`themes/emerald-dark.json`，可参考用于制作自己的主题。

---

## 注意事项

- `key` 字段必须全局唯一；若与内置主题 key 相同，自定义主题会被分配到「高级主题方案」下拉列表，不会覆盖内置项
- JSON 文件必须可被反序列化为 `ThemePalette`；字段名使用 camelCase（由 `JsonNamingPolicy.CamelCase` 处理）
- 颜色值支持标准 `#RRGGBB` 与 `#AARRGGBB` 格式
- 修改 `themes/` 目录中的文件后需重启应用才会重新加载
