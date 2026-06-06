# ============================================================================
# 🐳 DOCKERFILE MULTI-STAGE: EMPACOTAÇÃO ORBITAL DO CONTÊINER
# ============================================================================

# Estágio 1: Compilação e Restauração de Dependências no Vácuo
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar arquivos de projeto e restaurar cabos de DI (pacotes nuget)
COPY ["AntiGravityMongoDBApi.csproj", "./"]
RUN dotnet restore "AntiGravityMongoDBApi.csproj"

# Copiar o restante do código espacial e compilar
COPY . .
RUN dotnet build "AntiGravityMongoDBApi.csproj" -c Release -o /app/build

# Estágio 2: Publicação dos Binários Consolidados
FROM build AS publish
RUN dotnet publish "AntiGravityMongoDBApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Estágio 3: Runtime de Produção (Imagem Mínima Sem SDK)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Variáveis de ambiente padrão para Kestrel orbitar na porta 8080
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "AntiGravityMongoDBApi.dll"]
