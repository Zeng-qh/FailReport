# 构建基础阶段（仅用于还原依赖，利用缓存加速）
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS base
WORKDIR /src
# 仅复制项目文件，还原依赖（单独成层，修改代码不触发重新下载）
COPY ["FailReport.csproj", "./"]
RUN dotnet restore "FailReport.csproj"

# 构建并发布阶段（确保构建和发布的连贯性）
FROM base AS publish
WORKDIR /src
# 复制剩余源代码（项目文件已在base阶段复制，这里避免重复）
COPY . .
# 显式构建后再发布（即使publish隐含build，显式指定更稳妥）
# 先构建项目
RUN dotnet build "FailReport.csproj" -c Release --no-restore \
    # 验证构建输出
    && echo "构建产物验证：" \
    && ls -la /src/bin/Release/net9.0/ \
    # 发布项目（不使用--no-build，让publish确保所有必要文件都被包含）
    && dotnet publish "FailReport.csproj" -c Release -o /app/publish \
    # 验证发布输出
    && echo "发布产物验证：" \
    && ls -la /app/publish

# 运行阶段（精简镜像，仅保留运行时依赖）
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
# 从发布阶段复制产物到运行目录
COPY --from=publish /app/publish .
# 暴露容器端口（根据实际应用监听端口调整，默认80）
EXPOSE 80
# 入口点（确保DLL名称与项目输出一致，区分大小写）
ENTRYPOINT ["dotnet", "FailReport.dll"]
