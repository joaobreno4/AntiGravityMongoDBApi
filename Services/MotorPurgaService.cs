using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AntiGravityMongoDBApi.Controllers;

namespace AntiGravityMongoDBApi.Services
{
    // ============================================================================
    // ⚙️ MOTOR PURGA DE DETRITOS ORBITAL (BACKGROUND WORKER)
    // ============================================================================
    // Este worker consome as mensagens da FilaAmortecimentoDetritos de forma assíncrona.
    // Ele executa escritas em lote no MongoDB Atlas usando BulkWriteAsync para evitar 
    // overhead de rádio (I/O de rede) com o Cluster, batendo nos limites de 5 segundos 
    // ou 500 elementos.
    public class MotorPurgaService : BackgroundService
    {
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<ElementoFlutuante> _collection;
        private readonly ILogger<MotorPurgaService> _logger;

        private static readonly TimeSpan IntervaloPurga = TimeSpan.FromSeconds(5);
        private const int LimiteLote = 500;

        public MotorPurgaService(IMongoDatabase database, ILogger<MotorPurgaService> logger)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _collection = _database.GetCollection<ElementoFlutuante>("ElementosDOM");
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 [SISTEMA SRE] Motor de Purga de Detritos orbital inicializado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Aguarda 5 segundos antes do próximo ciclo de purga.
                    // Em caso de rajada alta, o worker pode purgar mais rápido se avaliado.
                    await Task.Delay(IntervaloPurga, stoppingToken);

                    if (FiltroMassaMiddleware.FilaAmortecimentoDetritos.IsEmpty)
                    {
                        continue;
                    }

                    await ProcessarFilaAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("⚠️ [SISTEMA SRE] Operação cancelada. Salvando buffers orbitais finais...");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "💥 [ERRO SRE] Falha no loop principal do Motor de Purga.");
                }
            }
        }

        private async Task ProcessarFilaAsync(CancellationToken stoppingToken)
        {
            var modelosParaInserir = new List<WriteModel<ElementoFlutuante>>();
            int contador = 0;

            // Coleta até 500 itens do buffer orbital
            while (contador < LimiteLote && FiltroMassaMiddleware.FilaAmortecimentoDetritos.TryDequeue(out var itemFila))
            {
                try
                {
                    var elemento = JsonSerializer.Deserialize<ElementoFlutuante>(itemFila.Payload, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (elemento != null)
                    {
                        if (string.IsNullOrEmpty(elemento.Id))
                        {
                            elemento.Id = Guid.NewGuid().ToString();
                        }
                        
                        // Envelopa a operação de inserção
                        modelosParaInserir.Add(new InsertOneModel<ElementoFlutuante>(elemento));
                        contador++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "⚠️ [SRE] Detrito com payload corrompido ejetado da fila de escrita.");
                }
            }

            if (modelosParaInserir.Count > 0)
            {
                _logger.LogInformation("🛰️ [SRE] Escoando lote de {Quantidade} detritos leves para o MongoDB Atlas...", modelosParaInserir.Count);

                try
                {
                    // BulkWriteAsync envia uma única requisição de rede para gravar múltiplos itens no Atlas,
                    // otimizando consideravelmente o uso de conexões de rádio.
                    var resultado = await _collection.BulkWriteAsync(modelosParaInserir, new BulkWriteOptions { IsOrdered = false }, stoppingToken);
                    _logger.LogInformation("✅ [SRE] Lote de {Inseridos} itens ancorado no Atlas. Falhas: {Erros}", resultado.InsertedCount, resultado.WriteErrors.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "💥 [CRÍTICO SRE] Falha de conexão interestelar durante BulkWrite! Recolocando itens no buffer...");
                    
                    // Em caso de falha de conexão (ex: drift orbital do nó primário), reinserimos os elementos no buffer 
                    // para preservar os dados e evitar perda de rastreabilidade física.
                    foreach (var writeModel in modelosParaInserir)
                    {
                        if (writeModel is InsertOneModel<ElementoFlutuante> insertModel)
                        {
                            var reJson = JsonSerializer.Serialize(insertModel.Document);
                            FiltroMassaMiddleware.FilaAmortecimentoDetritos.Enqueue(new ItemFilaAmortecimento 
                            { 
                                Payload = reJson, 
                                DataEntrada = DateTime.UtcNow 
                            });
                        }
                    }
                }
            }
        }
    }
}
