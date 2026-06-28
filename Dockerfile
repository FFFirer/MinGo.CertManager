# 第一阶段：构建前端
FROM swr.cn-north-4.myhuaweicloud.com/ddn-k8s/docker.io/library/node:22-alpine AS frontend-build

# 安装 pnpm
ARG NPM_REGISTRY=https://registry.npmjs.org/

ENV COREPACK_NPM_REGISTRY=${NPM_REGISTRY}
ENV PNPM_HOME="/pnpm"
ENV PATH="$PNPM_HOME:$PATH"

RUN npm install -g corepack@latest

RUN corepack enable && corepack prepare pnpm@11.9.0 --activate

# 设置工作目录
WORKDIR /app

# 复制 pnpm 工作区配置文件（根级 package.json、workspace yaml、lockfile）
COPY package.json pnpm-workspace.yaml pnpm-lock.yaml ./

# 还原 npm 包（使用缓存）
RUN pnpm install --frozen-lockfile

# 复制 Web 源码目录
COPY src/MinGo.CertManager.Web src/MinGo.CertManager.Web

# 构建 tailwindcss 到 wwwroot 目录
RUN pnpm --filter mingo.certmanager.web run build

# 第二阶段：构建 .NET 应用
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend-build-base

RUN dotnet tool install -g dotnet-ef --version 10.0.4

FROM backend-build-base AS backend-build

# 设置工作目录
WORKDIR /app

# 复制 .Net 的项目文件及解决方案文件，用于缓存
COPY *.slnx .
COPY src/MinGo.CertManager.Web/MinGo.CertManager.Web.csproj src/MinGo.CertManager.Web/
COPY src/MinGo.CertManager.Application/MinGo.CertManager.Application.csproj src/MinGo.CertManager.Application/
COPY src/MinGo.CertManager.Core/MinGo.CertManager.Core.csproj src/MinGo.CertManager.Core/
COPY src/MinGo.CertManager.Infrastructure/MinGo.CertManager.Infrastructure.csproj src/MinGo.CertManager.Infrastructure/
COPY src/MinGo.CertManager.SDK/MinGo.CertManager.SDK.csproj src/MinGo.CertManager.SDK/
COPY test/MinGo.CertManager.SDK.Tests/MinGo.CertManager.SDK.Tests.csproj test/MinGo.CertManager.SDK.Tests/
COPY test/MinGo.CertManager.Tests/MinGo.CertManager.Tests.csproj test/MinGo.CertManager.Tests/

# 还原 nuget 包（使用缓存）
RUN ls 
RUN dotnet restore

# 复制全部项目文件
COPY . .

# 使用 Release 编译项目
RUN dotnet build --configuration Release

# 发布 Web 站点
RUN dotnet publish src/MinGo.CertManager.Web --configuration Release --no-build --output /app/publish

# 发布efbundle
WORKDIR /app/src/MinGo.CertManager.Infrastructure
ENV PATH="$PATH:/root/.dotnet/tools"
RUN dotnet tool list -g
RUN dotnet ef migrations bundle --configuration Release --no-build --output /app/publish/efbundle -f

# 第三阶段：发布
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# 设置工作目录
WORKDIR /app

# 复制发布文件
COPY --from=backend-build /app/publish .
# 复制 node 构建镜像中生成的 tailwindcss 相关文件
COPY --from=frontend-build /app/wwwroot ./wwwroot

# 创建数据目录
RUN mkdir -p /app/data

# 运行应用
ENTRYPOINT ["dotnet", "MinGo.CertManager.Web.dll"]
