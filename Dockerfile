# 构建阶段
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# 先复制项目文件，单独还原依赖（依赖不变时可缓存）
COPY *.csproj ./
RUN dotnet restore

# 再复制其他代码文件（代码修改不影响依赖层缓存）
COPY . .
RUN dotnet build -c Release -o /app/build --no-restore  # 加上 --no-restore 跳过重复还原

# 发布阶段（使用 --no-build 跳过重复构建）
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish --no-build

# 运行阶段（保持不变）
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 80
ENTRYPOINT ["dotnet", "FailReport.dll"]  # 确认 DLL 名称正确
