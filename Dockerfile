# 构建基础阶段（仅用于还原依赖，避免重复下载）
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS base
WORKDIR /src
COPY FailReport.csproj ./
RUN dotnet restore "FailReport.csproj"  # 仅还原依赖，单独成层加速缓存

# 构建并发布阶段（合并构建和发布，确保产物生成）
FROM base AS publish
WORKDIR /src
COPY . .  # 复制所有源代码
# 直接发布（不使用 --no-build，强制重新构建，确保产物生成）
RUN dotnet publish "FailReport.csproj" -c Release -o /app/publish \
    && echo "发布完成，验证产物文件：" \
    && ls -la /app/publish  # 调试：列出发布目录文件，确认是否存在所需文件

# 运行阶段
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 80
ENTRYPOINT ["dotnet", "FailReport.dll"]  # 确保 DLL 名称正确（区分大小写）
