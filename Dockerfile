# 构建阶段：不指定输出路径（使用默认路径），避免解决方案冲突
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# 复制项目文件并还原依赖（针对单个项目）
COPY FailReport.csproj ./
RUN dotnet restore "FailReport.csproj"  # 明确指定项目文件

# 复制所有代码并构建（不指定 -o，使用默认输出到 bin/Release/net9.0）
COPY . .
RUN dotnet build "FailReport.csproj" -c Release --no-restore  # 明确指定项目文件，跳过重复还原

# 发布阶段：针对单个项目发布，使用默认构建路径的产物
FROM build AS publish
RUN dotnet publish "FailReport.csproj" -c Release -o /app/publish --no-build  # 明确指定项目文件

# 运行阶段
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 80
ENTRYPOINT ["dotnet", "FailReport.dll"]  # 确保 DLL 名称与项目输出一致（区分大小写）
