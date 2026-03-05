---
alwaysApply: true
---

# 技术框架

- 使用ASP.NET Core Blazor，渲染模式为Auto
- 样式使用TailwindCSS v4
  - tailwind.config.js定义了要监听的文件范围，需要应用在index.css中
  - theme配置放在index.css中
- 移除Bootstrap
- 使用Vite.AspNetCore提供开发时提供集成vite的功能
  - 无需手动执行tailwind生成，开发时自动监听文件变化，自动刷新
  
# 文件结构

- docs/         所有文档
  - 任务名/      每个任务的文档
- src/          所有源代码
- test/         所有测试代码
- demos/        所有演示代码

# 开发规范

- dotnet项目，目标框架使用.NET 10
- 使用git管理仓库代码变更
- 重点功能使用单元测试进行功能覆盖测试
  - 测试框架使用xUnit
  - 测试数据使用工具生成
- 前端使用Blazor进行构建
- 使用解决方案文件组织
- 代码格式使用editorconfig
- 重复实现需要重构成单一方法，避免重复实现相同逻辑
- 不使用魔数，定义常量
- 必要代码需要注释
- JSON序列化使用System.Text.Json
- 先理解需求，再规划技术方案，拆解实现任务，逐步实现，重点逻辑需要单元测试，统计开发进度，回顾并改进，直至实现需求目标
- 补全项目必要文件
- 日式默认使用Serilog Console输出
- 代码实现要考虑跨平台兼容性
- 优先使用最新稳定版本的包及框架
- 将必要的配置项添加在appsettings.json中，并配上必要的提示
- 修改实体类型后需要添加新的迁移
- 优先根据git文件变更判断变化的内容
- 遇到错误时，一方面在线搜索解决方案，一方面根据相关项目的文档，代码仓库，理解实际的用法
- 运行项目前，确保npm包已使用pnpm正确还原

# 实现规范

## 遵循整洁架构/洋葱架构

- 实现层次为更具体的实现依赖更抽象的逻辑
- 分为业务Core、Application、Infrastructure、Web
  - Core：实体，DTO，共享工具类，基础设施能力的抽象，不依赖
  - Application: 业务逻辑实现，依赖Core，不会直接与外部交互
  - Infrastructure: 仅依赖Core，与外部基础设施，如数据库、文件系统、网络等
  - Web: 依赖Application和Infrastructure，实现与外部系统的交互，如Web API、Blazor等

## 其他

- 使用模板创建项目时，默认代码或实现都需要移除，保持代码整洁

# 技术框架

- EntityFrameworkCore
- sqlite
- acme.sh
- Blazor
- TailwindCSS v4
- js/css
- Quartz.NET
