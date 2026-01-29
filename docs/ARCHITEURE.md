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

# 技术框架

- EntityFrameworkCore
- sqlite
- acme.sh
- Blazor
- Bootstrap
- js/css
- Quartz.NET

# 项目结构

- docs/ 文档目录
- src/ 源码目录
- test/ 测试目录
- main.slnx 解决方案

## docs/

- ARCHITEURE.md 项目整体架构描述
- REQUIREMENTS.md 项目需求清单
- TASKS.md 开发任务

## src/

- 主要实现逻辑

## test/

- 单元测试代码

## main.slnx

- 解决方案
