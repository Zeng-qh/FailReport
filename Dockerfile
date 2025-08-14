# 构建阶段
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# 复制项目文件并还原依赖
COPY *.csproj ./
RUN dotnet restore

# 复制所有文件并构建
COPY . .
RUN dotnet build -c Release -o /app/build

# 发布阶段
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

# 运行阶段
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# 暴露端口（根据你的应用修改，如 80 或 5000）
EXPOSE 80

# 启动命令
ENTRYPOINT ["dotnet", "failreport.dll"]  # 替换为你的输出 DLL 名称
