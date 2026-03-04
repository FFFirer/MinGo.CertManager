# 使用 .NET 10 SDK 作为构建镜像
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

# 设置工作目录
WORKDIR /app

# 复制项目文件
COPY . .

# 安装 Node.js
RUN curl -fsSL https://deb.nodesource.com/setup_20.x | bash -
RUN apt-get install -y nodejs

# 构建前端
WORKDIR /app/src/MinGo.CertManager.Web
RUN npm install
RUN npm run build

# 构建 .NET 应用
WORKDIR /app
RUN dotnet restore
RUN dotnet build --configuration Release
RUN dotnet publish src/MinGo.CertManager.Web --configuration Release --output /app/publish

# 使用 .NET 10 运行时作为最终镜像
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# 设置工作目录
WORKDIR /app

# 复制发布文件
COPY --from=build /app/publish .

# 创建数据目录
RUN mkdir -p /app/data

# 暴露端口
EXPOSE 8080

# 设置环境变量
ENV ASPNETCORE_URLS=http://+:8080

# 运行应用
ENTRYPOINT ["dotnet", "MinGo.CertManager.Web.dll"]
