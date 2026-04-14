# Tranbok.Tools Wiki

欢迎来到 Tranbok.Tools 的 Wiki。本 Wiki 覆盖架构设计、插件开发、功能配置等方面的详细说明。

---

## 文档索引

### 核心概念

| 文档 | 说明 |
|---|---|
| [架构概览](./Architecture.md) | 分层结构、各模块职责、服务依赖关系与数据流 |
| [插件系统](./Plugin-System.md) | 插件契约、生命周期状态机、加载机制与两种接入方式 |

### 功能指南

| 文档 | 说明 |
|---|---|
| [插件变量管理](./Plugin-Variable-Management.md) | 如何声明变量、在设置页管理键值、在插件中读取 |
| [自定义主题](./Theme-Customization.md) | JSON 主题格式、全部配色字段参考、`themes/` 目录约定 |
| [迁移插件指南](./Migration-Plugin.md) | EF Core 迁移插件的完整使用说明与 DesignSettings 格式 |

### 开发参考

| 文档 | 说明 |
|---|---|
| [创建插件](./Creating-a-Plugin.md) | 从零创建一个完整插件的分步教程 |

---

## 快速导航

- **我想运行项目** → 见 [README 快速开始](../../README.md#快速开始)
- **我想写一个新插件** → 见 [创建插件](./Creating-a-Plugin.md)
- **我想了解插件 ID 命名规则** → 见 [插件系统 · 命名约定](./Plugin-System.md#id-命名约定)
- **我想给插件配置环境变量** → 见 [插件变量管理](./Plugin-Variable-Management.md)
- **我想做一个自定义配色主题** → 见 [自定义主题](./Theme-Customization.md)
- **我想使用 EF Core 迁移功能** → 见 [迁移插件指南](./Migration-Plugin.md)
