# EF Core 迁移插件

迁移插件（`tranbok.migration`）提供图形化的 EF Core 迁移管理界面，支持多数据库类型与多连接 Profile 配置。

---

## 前置要求

| 要求 | 说明 |
|---|---|
| `dotnet` CLI | 已安装并在 PATH 中 |
| `dotnet ef` 全局工具 | `dotnet tool install --global dotnet-ef` |
| 目标 Domain 项目 | 包含 `DbContext` 的 `.csproj` |
| `DesignSettings.json` | 位于 Domain 项目目录下，描述设计时依赖 |

---

## DesignSettings.json 格式

每个 Domain 项目目录下需要有一个 `DesignSettings.json`，告知迁移插件在临时 Workspace 工程中需要安装哪些包：

```json
{
  "dotnetVersion": "net10.0",
  "packages": [
    {
      "packageName": "Microsoft.EntityFrameworkCore.Design",
      "version": "10.0.0"
    },
    {
      "packageName": "Microsoft.EntityFrameworkCore.SqlServer",
      "version": "10.0.0"
    }
  ]
}
```

**数据库 Provider 包对照**

| 数据库 | Provider 包 |
|---|---|
| SQL Server | `Microsoft.EntityFrameworkCore.SqlServer` |
| PostgreSQL | `Npgsql.EntityFrameworkCore.PostgreSQL` |
| MySQL | `Pomelo.EntityFrameworkCore.MySql` |

> 若 `packages` 中缺少对应的 Provider 包，插件会根据 `Microsoft.EntityFrameworkCore.Design` 的版本号自动补全必要依赖。

---

## 配置流程

### 1. 新建连接 Profile

在迁移页面顶部点击「新建配置」，填写：

| 字段 | 说明 |
|---|---|
| Profile 名称 | 自定义标识，如 `开发环境`、`测试数据库` |
| 数据库类型 | SQL Server / PostgreSQL / MySQL |
| DbContext 名称 | 不含命名空间，如 `AppDbContext` |
| 连接字符串 | 完整连接字符串 |

### 2. 选择 Domain 项目

在 Profile 配置中选择包含 `DbContext` 的 `.csproj` 文件路径。该目录下须存在 `DesignSettings.json`。

### 3. 刷新迁移列表

点击「刷新」按钮，插件会扫描 Domain 项目目录并列出所有迁移文件。

---

## 操作说明

| 操作 | 说明 |
|---|---|
| **刷新列表** | 重新扫描迁移文件目录 |
| **新增迁移** | 输入迁移名称后执行 `dotnet ef migrations add <Name>` |
| **执行更新** | 执行 `dotnet ef database update`，将未应用迁移应用到数据库 |
| **删除最后一条** | 执行 `dotnet ef migrations remove`，删除最新迁移（仅限未应用） |
| **查看/编辑文件** | 在右侧面板查看迁移文件内容，可直接编辑后保存 |

---

## 连接字符串配置方式

迁移插件通过环境变量向 `dotnet ef` 传递连接字符串，无需修改业务项目代码。

### 推荐方式：插件变量管理

在**设置 → 插件变量管理**中配置 `tranbok.migration` 插件的变量：

| 键名 | 说明 |
|---|---|
| `TRANBOK_DB_CONNECTION` | 迁移时使用的数据库连接字符串 |

插件在执行 `dotnet ef` 命令前会将此值注入到子进程的环境变量中。

### 直接在 Profile 中配置

也可直接在 Profile 的「连接字符串」字段填写，该值会在运行时直接传递给命令进程。

---

## 工作原理

迁移插件采用 **Workspace 模式**，不在业务项目中直接执行操作，而是：

1. 在 `<AppDir>/plugins/Migration/WorkSpace/` 下生成临时工程
2. 将 Domain 项目作为 ProjectReference 引入
3. 自动生成 `DesignTimeFactory.cs`：

```csharp
public sealed class WorkspaceDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("TRANBOK_DB_CONNECTION")
            ?? throw new InvalidOperationException("No connection string configured.");

        var options = new DbContextOptionsBuilder<AppDbContext>();
        // 按数据库类型配置 Provider...
        return new AppDbContext(options.Options);
    }
}
```

4. 执行 `dotnet restore` → `dotnet build` → `dotnet ef <操作>`
5. 将生成的迁移文件复制到 `<AppDir>/plugins/Migration/Output/`

---

## 配置文件与目录

迁移插件读写以下路径（均相对于应用目录）：

| 路径 | 内容 |
|---|---|
| `plugins/Migration/.tranbok-tools.json` | 主配置文件（Profile 列表） |
| `plugins/Migration/setting.json` | 备用配置文件 |
| `plugins/Migration/WorkSpace/` | 临时 Workspace 工程（运行时生成） |
| `plugins/Migration/Output/` | 迁移输出文件 |

> `WorkSpace/` 和 `Output/` 均为运行期产物，建议加入 `.gitignore`。

**旧路径兼容**（仅读取，不写入）：

- `<Domain项目目录>/.tranbok-tools.json`
- `<Domain项目目录>/Migration/setting.json`

---

## 常见问题

**Q: 执行迁移时报错 `dotnet ef not found`**

确认已安装全局工具：

```bash
dotnet tool install --global dotnet-ef
```

并确认 `~/.dotnet/tools`（或等效路径）已加入 PATH。

**Q: 报错 `DesignSettings.json not found`**

在 Domain 项目目录下创建 `DesignSettings.json`，至少包含 `dotnetVersion` 与 `packages`（含 `Microsoft.EntityFrameworkCore.Design`）。

**Q: PostgreSQL / MySQL 构建失败，提示缺少 Provider**

在 `DesignSettings.json` 的 `packages` 中补充对应 Provider 包，或让插件自动补全（只需确保 `Microsoft.EntityFrameworkCore.Design` 已在列表中）。

**Q: 迁移执行成功但文件没有出现在业务项目中**

迁移插件当前将输出写入 `plugins/Migration/Output/`，需手动或通过脚本将文件复制到业务项目的 Migrations 目录。
