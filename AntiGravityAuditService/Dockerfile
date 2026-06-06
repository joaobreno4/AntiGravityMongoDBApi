# Estágio 1: Compilação no SDK .NET
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["AntiGravityAuditService.csproj", "./"]
RUN dotnet restore "AntiGravityAuditService.csproj"
COPY . .
RUN dotnet build "AntiGravityAuditService.csproj" -c Release -o /app/build

# Estágio 2: Publicação dos binários
FROM build AS publish
RUN dotnet publish "AntiGravityAuditService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Estágio 3: Runtime leve do Worker (.NET Runtime apenas)
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AntiGravityAuditService.dll"]
