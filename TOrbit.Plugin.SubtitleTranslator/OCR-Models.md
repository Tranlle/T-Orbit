# OCR 模型说明

`Subtitle Translator` 当前的本地 OCR 原型依赖 `RapidOCR.Net` 包。

注意：

- 该 NuGet 包不是 RapidAI 官方发布包
- 它是基于 RapidOCR 技术路线的独立发布版本
- 当前项目参考的是其“本地 ONNX OCR 接入方式”，不是其代码风格

参考来源：

- NuGet: https://www.nuget.org/packages/RapidOCR.Net
- RapidAI RapidOCR: https://github.com/RapidAI/RapidOCR
- RapidOcrNet: https://github.com/BobLd/RapidOcrNet

## 当前原型约束

当前 `LocalOcrService` 支持两种初始化方式：

- `RapidOcr`
- `InitModels(int)`
- `InitModels(string det, string cls, string rec, string keys, int intraOpThreads)`
- `Detect(SKBitmap, RapidOcrOptions.Default)`

这意味着当前原型既支持：

1. 使用库自己的默认模型发现逻辑
2. 显式传入四个模型文件路径

后者更适合本项目首版，因为模型来源、语言和部署目录都需要可控。

## 模型文件

根据 `RapidOcrNet` 说明，至少需要以下四类资源：

1. 检测模型
2. 方向分类模型
3. 识别模型
4. 字典文件

常见组合示例：

- `ch_PP-OCRv5_mobile_det.onnx`
- `ch_ppocr_mobile_v2.0_cls_infer.onnx`
- `latin_PP-OCRv5_rec_mobile_infer.onnx`
- `ppocrv5_latin_dict.txt`

## 下一步

当前更推荐的接法：

1. 在插件中约定一个固定模型目录
2. 显式将 det / cls / rec / dict 四个文件路径传给 `LocalOcrService`
3. 避免依赖第三方包的隐式模型发现机制

在 `M1-04` 之后需要进一步明确：

1. 模型文件最终放在仓库内、用户目录还是插件输出目录
2. 首版默认随插件提供哪组模型
3. 首版默认语言模型选型是中英混合还是以英文字幕优先

如果后续确认该包的模型加载能力不够清晰，建议保留 `LocalOcrService` 抽象层，并替换为更可控的本地 OCR 封装实现。
