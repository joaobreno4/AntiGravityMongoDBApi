using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AntiGravityMongoDBApi;

// ============================================================================
// 🧲 SISTEMA DE ANCORAGEM INICIAL (INICIALIZAÇÃO DO CONTEXTO DE GRAVIDADE ZERO)
// ============================================================================
// Atenção: O 'builder' precisa ser instanciado e travado rápido na memória do sistema,
// antes que suas variáveis comecem a flutuar para fora da RAM do servidor!
var builder = WebApplication.CreateBuilder(args);

// --- CONFIGURAÇÃO DA STRING DE CONEXÃO ORBITAL ---
// Lê a connection string de IConfiguration (appsettings / variável de ambiente).
// Nunca armazene credenciais no código-fonte — use secrets ou variáveis de ambiente.
var connectionString = builder.Configuration.GetConnectionString("MongoDatabase")
                       ?? "mongodb://localhost:27017/AntiGravityDB";

// --- INJEÇÃO DE DEPENDÊNCIAS (CABOS DE AÇO) ---
// O contêiner de DI do .NET segura os serviços em seus devidos lugares.
// Registramos o MongoClient como Singleton para que a mesma conexão seja 
// compartilhada mesmo que os servidores rodem em 360 graus.
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    try
    {
        Console.WriteLine("🛰️ [SISTEMA] Disparando âncora de conexão em direção ao MongoDB Atlas...");
        return new MongoClient(connectionString);
    }
    catch (Exception ex)
    {
        // Se a gravidade falhar totalmente e o sinal do cluster sumir no espaço profundo:
        Console.WriteLine($"💥 [COLISÃO CÓSMICA] Falha catastrófica ao prender o cabo ao MongoDB: {ex.Message}");
        throw;
    }
});

// Registra a referência do Banco de Dados flutuante
builder.Services.AddSingleton(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    // Conectando ao banco de dados espacial 'EstacaoDerivaDb'
    return client.GetDatabase("EstacaoDerivaDb");
});

// Prender os Controllers orbitais para escaneamento
builder.Services.AddControllers();

// Registrando o worker orbital de escoamento de detritos em lote
builder.Services.AddHostedService<AntiGravityMongoDBApi.Services.MotorPurgaService>();

var app = builder.Build();

// --- SHIELD DE CONTROLE DE FLUXO E PROTEÇÃO ---
// Ativa o filtro seletivo por massa antes de bater nas rotas e Controllers
app.UseMiddleware<FiltroMassaMiddleware>();

// ============================================================================
// 📡 ROTAS DE COMUNICAÇÃO INTERESTELAR (ENDPOINTS DA API)
// ============================================================================

// Rota 1: Diagnóstico de Flutuação da Estação (Liveness Probe)
app.MapGet("/status", () => new
{
    Estacao = "Estação Científica Orbital Antigravity",
    Módulo = "Núcleo de APIs .NET Core",
    GravidadeDetectada = 0.0,
    StatusSistemas = "Flutuando perfeitamente",
    Alerta = "Aviso: Cuidado com colisões de variáveis!"
});

// Rota 2: Listar destroços capturados (Buscar do MongoDB Atlas)
app.MapGet("/destrocos", async (IMongoDatabase db) =>
{
    // O MongoDB Atlas guarda tudo, não importa se o objeto está girando ou de ponta cabeça.
    var colecao = db.GetCollection<ItemFlutuante>("Destrocos");
    
    try
    {
        // Buscando tudo o que está flutuando no vácuo (filtro vazio {} encontra tudo à deriva)
        var destrocos = await colecao.Find(new BsonDocument()).ToListAsync();
        return Results.Ok(destrocos);
    }
    catch (MongoException ex)
    {
        return Results.Problem($"O Cluster derivou para fora de alcance: {ex.Message}");
    }
});

// Rota 3: Lançar novo objeto na órbita (Inserir documento com Schema flexível)
app.MapPost("/destrocos", async (IMongoDatabase db, ItemFlutuante novoItem) =>
{
    var colecao = db.GetCollection<ItemFlutuante>("Destrocos");
    
    // Ancorando o ID para que não flutue nulo no vácuo de serialização BSON
    if (string.IsNullOrEmpty(novoItem.Id))
    {
        novoItem.Id = Guid.NewGuid().ToString();
    }
    
    try
    {
        // Insere o documento flutuante no cluster
        await colecao.InsertOneAsync(novoItem);
        return Results.Created($"/destrocos/{novoItem.Id}", novoItem);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Erro ao ejetar objeto no banco de dados: {ex.Message}");
    }
});

// Roteando os Controllers que foram ancorados
app.MapControllers();

// Inicializando o servidor HTTP espacial.
// Segure-se firme nas amarras do console!
app.Run();

// ============================================================================
// 📦 MODELOS ORBITAIS (SCHEMAS FLEXÍVEIS NO VÁCUO)
// ============================================================================
// Em ambientes tradicionais com gravidade, definiríamos um esquema de tabela fixo.
// Mas aqui, se o astronauta soltar uma ferramenta (Ex: "Chave Inglesa") e ela 
// se fundir fisicamente com um "Parafuso" no ar, a flexibilidade do NoSQL brilha!
public class ItemFlutuante
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string? Id { get; set; }

    [BsonElement("nome")]
    public string Nome { get; set; } = "Destroço Espacial Indefinido";

    [BsonElement("altitudeFlutuacaoMetros")]
    public double AltitudeFlutuacaoMetros { get; set; }

    [BsonElement("velocidadeDeriva")]
    public double VelocidadeDeriva { get; set; }

    // O MongoDB usa o decorator [BsonExtraElements] para capturar qualquer elemento 
    // adicional que tenha colidido com o objeto no espaço e grudado nele.
    // Isso evita erros de desserialização quando os schemas flutuam e se fundem!
    [BsonExtraElements]
    public Dictionary<string, object>? DetritosAcoplados { get; set; }
}
