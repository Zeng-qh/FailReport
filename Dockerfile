# 构建阶段：仅操作单个项目，不依赖解决方案
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# 复制项目文件并还原依赖（仅针对单个项目）
COPY FailReport.csproj ./
RUN dotnet restore "FailReport.csproj"  # 明确指定项目文件，避免解决方案干扰

# 复制所有源代码
COPY . .

# 发布阶段：直接发布（不依赖构建阶段的产物，避免路径问题）
FROM build AS publish
# 移除 --no-build，强制发布时重新构建，确保产物生成
RUN dotnet publish "FailReport.csproj" -c Release -o /app/publish

# 运行阶段
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 80
ENTRYPOINT ["dotnet", "FailReport.dll"]  # 确保 DLL 名称与项目输出一致
