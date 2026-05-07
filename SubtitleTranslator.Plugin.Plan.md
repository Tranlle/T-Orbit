# Subtitle Translator 插件开发规划

## 1. 文档目标

本文档用于指导 T-Orbit 新插件 `Subtitle Translator` 的设计与开发。  
插件目标来源于 [111.md](C:\Users\tangj\Desktop\project\T-Orbit\111.md) 中提出的方案，即：

- 基于屏幕选区抓取视频字幕区域
- 使用本地 OCR 持续识别画面中已经出现的字幕
- 对 OCR 结果进行去重与稳定化处理
- 将识别文本实时翻译并在工作区中显示

本文档会明确首版功能边界、模块拆分、技术路线、UI 方向、变量设计、里程碑与风险控制，作为后续实现的开发基线。

## 2. 插件定位

### 2.1 名称与标识

- 插件名称：`Subtitle Translator`
- 建议项目目录：`TOrbit.Plugin.SubtitleTranslator`
- 建议插件 ID：`torbit.subtitle-translator`
- 插件类型：`Visual`

### 2.2 产品定位

`Subtitle Translator` 是一个面向视频字幕场景的实时辅助工具，而不是通用 OCR 工具，也不是音频转字幕工具。

它的职责非常明确：

- 用户手动框选字幕所在的屏幕区域
- 插件周期性抓取该区域画面
- 插件使用本地 OCR 识别字幕文本
- 插件对文本进行去重与稳定化
- 插件将稳定文本翻译为目标语言
- 插件在自身工作区中实时显示原文和译文

### 2.3 首版边界

首版只做最小闭环，不提前扩张到复杂特性。

首版包含：

- 手动框选字幕区域
- 周期性截取选区画面
- 本地 OCR 识别
- OCR 前图像预处理
- 字幕去重与稳定逻辑
- 原文实时显示
- 译文实时显示
- 开始、暂停、重新选区
- 基本运行日志与错误提示

首版不包含：

- 音频转字幕
- 自动识别字幕区域
- 全局悬浮窗
- 快捷键控制
- 字幕历史时间轴
- 多窗口协同
- 复杂模型管理界面

## 3. 技术路线

### 3.1 OCR 路线

OCR 实现方式参考 `RapidOCRCSharp` 的技术路径，但不继承其代码风格、代码组织、命名习惯和页面设计。

可借鉴的点：

- 使用本地离线 OCR 模型
- 模型初始化后对单张图像执行检测与识别
- 检测与识别参数可调
- 更适合“频繁处理小区域截图”的字幕场景

不参考的点：

- WinForms 风格页面布局
- 松散的示例型代码结构
- 偏练手性质的命名与工程组织

参考来源：

- https://github.com/RapidAI/RapidOCRCSharp

### 3.2 推荐实现原则

1. OCR 引擎与页面层彻底解耦。
2. 截图、OCR、翻译必须拆成异步流水线。
3. 字幕稳定化必须独立成服务，不允许直接拿 OCR 原始输出刷新 UI。
4. 参数必须进入插件变量系统，而不是散落在 ViewModel 中。
5. 首版优先保证识别质量和刷新稳定性，而不是视觉花活。

## 4. 整体架构

### 4.1 处理流水线

插件运行时采用如下流水线：

1. `Region Selection`
   用户手动选择字幕区域，保存矩形坐标。
2. `Capture Loop`
   定时抓取选区图像，推荐间隔 300ms 到 800ms。
3. `Image Preprocess`
   对图像进行灰度化、对比度增强、二值化、降噪、缩放与 padding。
4. `OCR Engine`
   调用本地 OCR 模型识别文本。
5. `Subtitle Stabilizer`
   对识别结果做清洗、相似度比较、去重与抖动抑制。
6. `Translation Adapter`
   将稳定后的字幕送入翻译层。
7. `Workspace Output`
   在插件工作区内显示最新原文、译文、状态与日志。

### 4.2 分层目标

- 采样层只关心图像来源
- OCR 层只关心图像识别
- 稳定层只关心文本质量
- 翻译层只关心语言转换
- 页面层只关心状态展示与用户操作

这样的边界有两个直接收益：

- 后续可以替换 OCR 实现，不影响页面层
- 后续可以替换翻译实现，不影响截图与稳定逻辑

## 5. 项目结构规划

建议新增项目：

- `TOrbit.Plugin.SubtitleTranslator`

建议目录结构：

```text
TOrbit.Plugin.SubtitleTranslator/
├── Models/
├── Services/
├── ViewModels/
├── Views/
├── SubtitleTranslatorPlugin.cs
├── SubtitleTranslatorPluginMetadata.cs
├── plugin.json
└── TOrbit.Plugin.SubtitleTranslator.csproj
```

## 6. 模块拆分

### 6.1 插件入口层

#### `SubtitleTranslatorPlugin.cs`

职责：

- 继承 `BasePlugin`
- 实现 `IVisualPlugin`
- 负责描述信息注册
- 负责变量注入
- 负责视图与 ViewModel 装配
- 负责页头动作输出

建议附加接口：

- `IPluginVariableReceiver`
- `IPluginPageHeaderProvider`
- `IPluginHeaderActionsProvider`
- `IPluginDisplayInfoProvider`

#### `SubtitleTranslatorPluginMetadata.cs`

职责：

- 定义插件 ID、名称、版本、作者、图标、标签
- 定义默认能力声明
- 定义默认变量清单

### 6.2 ViewModel 层

#### `SubtitleTranslatorViewModel.cs`

职责：

- 管理运行状态
- 管理选区状态
- 管理当前原文与译文
- 管理最后更新时间
- 管理错误提示与运行日志
- 暴露开始、暂停、重新选区、复制结果等命令

建议状态字段：

- `IsRegionSelected`
- `IsRunning`
- `IsPaused`
- `CurrentSourceText`
- `CurrentTranslatedText`
- `LastUpdatedAt`
- `StatusText`
- `LastError`
- `RecentLogs`

### 6.3 模型层

建议模型：

#### `SubtitleTranslatorVariables.cs`

用于绑定插件变量。

#### `SubtitleRegion.cs`

用于描述选区：

- `X`
- `Y`
- `Width`
- `Height`

#### `OcrFrameResult.cs`

用于描述单次 OCR 输出：

- 原始识别文本
- 置信信息
- 识别耗时
- 图像时间戳

#### `SubtitleFrameState.cs`

用于描述经过稳定化处理后的字幕状态：

- 稳定原文
- 是否为新字幕
- 相似度值
- 是否应触发翻译

#### `TranslationResult.cs`

用于描述翻译结果：

- 原文
- 译文
- 目标语言
- 翻译耗时

### 6.4 服务层

#### `ScreenCaptureService.cs`

职责：

- 按选区抓图
- 输出标准图像对象
- 控制采样频率
- 支持取消和安全停止

#### `ImagePreprocessService.cs`

职责：

- 灰度化
- 对比度增强
- 二值化
- 降噪
- 适度放大
- 边缘 padding

#### `LocalOcrService.cs`

职责：

- 初始化本地 OCR 模型
- 提供同步或异步识别入口
- 封装 OCR 参数
- 统一异常与资源释放

实现要求：

- OCR 引擎封装不得泄漏到 ViewModel
- 后续允许替换为其他本地 OCR 实现

#### `SubtitleStabilizerService.cs`

职责：

- 去掉空白与噪声字符
- 与上一帧字幕做相似度比较
- 识别逐字出现与局部抖动
- 控制最短刷新间隔
- 判断是否触发 UI 更新与翻译

#### `TranslationService.cs`

职责：

- 接收稳定后的字幕文本
- 调用目标翻译实现
- 返回统一翻译结果

要求：

- 翻译层单独抽象
- 允许后续替换不同翻译引擎
- 当翻译不可用时允许降级为“只显示原文”

#### `SubtitlePipelineService.cs`

职责：

- 串联抓图、预处理、OCR、稳定化、翻译
- 负责整体运行控制
- 负责生命周期管理
- 负责错误传播与状态事件

这是首版实现中最核心的业务服务。

## 7. 插件变量规划

以下变量建议纳入插件变量系统，由设置页统一管理。

### 7.1 基础运行变量

- `SUBTITLE_TARGET_LANGUAGE`
- `SUBTITLE_CAPTURE_INTERVAL_MS`
- `SUBTITLE_SHOW_SOURCE_TEXT`
- `SUBTITLE_SHOW_TRANSLATED_TEXT`

### 7.2 OCR 调参变量

- `SUBTITLE_OCR_PADDING`
- `SUBTITLE_OCR_MAX_SIDE_LEN`
- `SUBTITLE_OCR_BOX_SCORE_THRESH`
- `SUBTITLE_OCR_BOX_THRESH`
- `SUBTITLE_OCR_UNCLIP_RATIO`
- `SUBTITLE_OCR_DO_ANGLE`
- `SUBTITLE_OCR_MOST_ANGLE`

### 7.3 稳定化变量

- `SUBTITLE_SIMILARITY_THRESHOLD`
- `SUBTITLE_MIN_REFRESH_INTERVAL_MS`
- `SUBTITLE_STABILIZATION_WINDOW_MS`

### 7.4 翻译变量

- `SUBTITLE_TRANSLATION_PROVIDER`
- `SUBTITLE_TRANSLATION_ENDPOINT`
- `SUBTITLE_TRANSLATION_API_KEY`

说明：

- 其中 API Key 需要走加密变量
- 默认值应尽量可跑通本地流程
- 没有翻译配置时应允许仅显示 OCR 原文

## 8. UI 设计方向

本插件的页面设计遵循 `frontend-skill` 的约束，不采用传统后台卡片堆砌，不参考 `RapidOCRCSharp` 的 WinForms 设计。

### 8.1 视觉方向

视觉 thesis：

> 冷静、精密、低干扰，像一个实时字幕实验台，而不是配置页拼盘。

页面关键词：

- editor-like
- focused
- low-noise
- real-time
- restrained

### 8.2 内容结构

content plan：

1. 主工作区：当前原文与当前译文
2. 支撑区：采样状态、选区状态、识别时间与置信提示
3. 细节区：OCR 参数摘要与最近日志
4. 动作区：开始、暂停、重新选区、复制结果

### 8.3 交互方向

interaction thesis：

1. 运行开始时，结果区轻量渐入，状态灯切换为活跃态。
2. 新字幕到达时，原文和译文采用短距离滑移替换，避免整块闪屏。
3. 运行中状态条做低频脉冲反馈，表达“采样中”而不是用强打扰 loading。

### 8.4 布局建议

推荐页面布局：

- 左侧为主工作区，聚焦字幕内容
- 右侧为窄控制轨，放状态、参数摘要、操作
- 底部为细日志区，展示最近几条流水线事件

页面不建议做成：

- 传统三栏大表单
- 多层边框包裹
- 卡片瀑布流
- 大量小组件拼盘

### 8.5 首版 UI 重点

首版 UI 重点不是炫技，而是：

- 快速理解当前运行状态
- 快速看到当前字幕原文和译文
- 明确知道插件是否正在工作
- 出问题时能知道阻塞在哪一层

## 9. 页头与动作规划

建议页头显示内容：

- 当前状态：未选择区域 / 待启动 / 运行中 / 已暂停 / 错误
- 上次更新时间
- 当前目标语言

建议页头动作：

- `Start`
- `Pause`
- `Select Region`
- `Copy Translation`

如果首版动作过多导致页头拥挤，则保留：

- `Start`
- `Pause`
- `Select Region`

复制类动作可以放在主工作区内。

## 10. 开发里程碑

### Milestone 1：原型验证

目标：

- 打通选区截图
- 打通本地 OCR
- 在控制台或日志区输出 OCR 结果

验收：

- 能持续处理用户选中的字幕区域
- 能对常见字幕截图输出可读文本

### Milestone 2：MVP 闭环

目标：

- 加入图像预处理
- 加入字幕稳定逻辑
- 接入页面实时显示
- 接入翻译层

验收：

- 相同字幕不会持续抖动刷新
- 普通视频字幕场景下，1 到 2 秒内可看到译文

### Milestone 3：可用性优化

目标：

- 降低 CPU 占用
- 优化刷新策略
- 补错误提示和状态反馈
- 调整参数默认值

验收：

- 长时间运行不明显卡顿
- OCR 不可用或翻译失败时有清晰反馈

### Milestone 4：增强能力

目标：

- 自动识别字幕区
- 双语显示切换
- 历史字幕记录
- 快捷键控制
- 悬浮显示模式

说明：

这些内容不进入首版开发范围。

## 11. 风险与应对

### 11.1 OCR 准确率不足

风险来源：

- 字幕字体小
- 背景复杂
- 描边、阴影、发光影响识别
- 视频分辨率低

应对方式：

- 预处理服务独立可调
- OCR 参数进入变量系统
- 用户可重新选区
- 首版先针对常见字幕场景调优

### 11.2 CPU 占用过高

风险来源：

- 截图频率太高
- OCR 推理太频繁
- 翻译阻塞主流程

应对方式：

- 控制采样间隔
- 画面变化小则跳过 OCR
- OCR 与翻译异步化
- 限制日志刷新频率

### 11.3 文本抖动严重

风险来源：

- OCR 结果逐帧微抖
- 同一句字幕逐字出现
- 同一字幕连续多版本输出

应对方式：

- 文本相似度去重
- 最短刷新间隔
- 稳定窗口聚合
- 翻译前只处理稳定文本

### 11.4 翻译不稳定

风险来源：

- API 延迟
- 网络波动
- 翻译模型响应慢

应对方式：

- 翻译层单独抽象
- 支持降级为仅显示原文
- 明确区分“识别成功”和“翻译成功”状态

## 12. 首版验收标准

满足以下条件可视为首版可用：

- 用户可手动选择字幕区域
- 插件可稳定截取该区域画面
- 插件能识别该区域中的字幕文本
- 相同字幕不会频繁重复刷新
- 插件能在 1 到 2 秒内显示译文
- 识别失败、翻译失败、未配置等场景有明确提示
- 页面在持续运行时无明显卡顿或界面阻塞

## 13. 实施建议

推荐开发顺序：

1. 创建插件骨架与基本页面
2. 实现选区与截图能力
3. 接入本地 OCR
4. 加入图像预处理
5. 加入稳定化逻辑
6. 接入翻译服务
7. 优化 UI 状态与日志表现

这个顺序的原因很直接：

- 最大不确定性在 OCR，不在 UI
- 最大用户价值在闭环，不在增强项
- 最大维护价值在分层，不在一次性堆功能

## 14. 结论

`Subtitle Translator` 适合作为 T-Orbit 中一个独立的复杂工具型插件来实现。

从当前仓库结构看，这个插件完全可以沿用现有插件加载机制、变量系统和工作台式 UI 架构实现。  
从功能风险看，真正需要优先解决的是 OCR 识别稳定性、异步流水线控制和文本去重，而不是高级 UI 特性。

因此，首版开发应坚持以下原则：

- 先打通闭环
- 先保证稳定
- 先做可替换的 OCR/翻译抽象
- 先做可维护的模块边界

在此基础上，再逐步扩展自动选区、悬浮窗、快捷键与历史记录，整体路线会更稳。
