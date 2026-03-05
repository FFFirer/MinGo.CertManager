# 第一阶段：构建前端
FROM node:20 AS frontend-build

# 安装 pnpm
RUN npm install -g pnpm

# 设置工作目录
WORKDIR /app

# 复制 Web 目录
COPY src/MinGo.CertManager.Web .

# 还原 npm 包
RUN pnpm install

# 构建 tailwindcss 脚本到 wwwroot 目录
RUN pnpm run build

# 第二阶段：构建 .NET 应用
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend-build

# 设置工作目录
WORKDIR /app

# 复制 .Net 的项目文件及解决方案文件
COPY *.sln .
COPY src/MinGo.CertManager.Web/MinGo.CertManager.Web.csproj src/MinGo.CertManager.Web/
COPY src/MinGo.CertManager.Application/MinGo.CertManager.Application.csproj src/MinGo.CertManager.Application/
COPY src/MinGo.CertManager.Core/MinGo.CertManager.Core.csproj src/MinGo.CertManager.Core/
COPY src/MinGo.CertManager.Infrastructure/MinGo.CertManager.Infrastructure.csproj src/MinGo.CertManager.Infrastructure/

# 还原 nuget 包
RUN dotnet restore

# 复制全部项目文件
COPY . .

# 复制 node 构建镜像中生成的 tailwindcss 相关文件
COPY --from=frontend-build /app/wwwroot ./src/MinGo.CertManager.Web/wwwroot

# 使用 Release 编译项目
RUN dotnet build --configuration Release

# 发布 Web 站点
RUN dotnet publish src/MinGo.CertManager.Web --configuration Release --no-build --output /app/publish

# 第三阶段：发布
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# 设置工作目录
WORKDIR /app

# 复制发布文件
COPY --from=backend-build /app/publish .

# 创建数据目录
RUN mkdir -p /app/data

# 暴露端口
EXPOSE 8080

# 设置环境变量
ENV ASPNETCORE_URLS=http://+:8080

# 运行应用
ENTRYPOINT ["dotnet", "MinGo.CertManager.Web.dll"]
