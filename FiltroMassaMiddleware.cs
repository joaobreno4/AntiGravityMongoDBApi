using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System;

namespace AntiGravityMongoDBApi
{
    // ============================================================================
    // 🛡️ ESCUDO ORBITAL DE PROTEÇÃO: FILTRO SELETIVO POR MASSA FISICA
    // ============================================================================
    // Quando a estação cliente sofre uma chuva de destroços, este middleware impede
    // que o driver do MongoDB sofra sobrecarga.
    // Detritos de baixa massa são interceptados e mandados para uma fila na memória,
    // enquanto corpos maciços (como a barra de busca e logo) têm escrita imediata.
    public class ItemFilaAmortecimento
    {
        public string Payload { get; set; } = string.Empty;
        public DateTime DataEntrada { get; set; }
    }

    public class FiltroMassaMiddleware
    {
        private readonly RequestDelegate _next;
        
        // Limiar de corte físico: 50 gramas (Largura * Altura * 0.01)
        private const double LIMIAR_MASSA_CRITICA = 50.0;
        
        // Fila assíncrona em memória para amortecer detritos de massa leve
        public static readonly ConcurrentQueue<ItemFilaAmortecimento> FilaAmortecimentoDetritos = new();

        public FiltroMassaMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;

            // Filtra o tráfego que atinge a rota de colisão
            if (path.Contains("/colisao") && context.Request.Method == "POST")
            {
                context.Request.EnableBuffering();

                using (var reader = new StreamReader(context.Request.Body, leaveOpen: true))
                {
                    var body = await reader.ReadToEndAsync();
                    context.Request.Body.Position = 0; // Reseta a stream de leitura do body

                    try
                    {
                        using (var doc = JsonDocument.Parse(body))
                        {
                            var root = doc.RootElement;
                            double largura = 0.0;
                            double altura = 0.0;

                            // Tenta decodificar a geometria enviada pelo motor Box2D do frontend
                            if (root.TryGetProperty("largura", out var l)) largura = l.GetDouble();
                            if (root.TryGetProperty("altura", out var a)) altura = a.GetDouble();
                            
                            // Calcula a massa do elemento caótico
                            double massaEstimada = largura * altura * 0.01;

                            if (massaEstimada < LIMIAR_MASSA_CRITICA)
                            {
                                // DETRITO LEVE: Capturado e jogado na fila de espera cósmica para escrita amortecida
                                FilaAmortecimentoDetritos.Enqueue(new ItemFilaAmortecimento { Payload = body, DataEntrada = DateTime.UtcNow });
                                
                                context.Response.StatusCode = StatusCodes.Status202Accepted;
                                context.Response.ContentType = "application/json";
                                await context.Response.WriteAsync("{\"status\":\"Detrito leve desviado para fila de amortecimento orbital para escrita assíncrona.\"}");
                                return; // Interrompe o pipeline de execução imediata
                            }
                        }
                    }
                    catch
                    {
                        // Se o JSON estiver quebrado ou incompleto, permite seguir para validação no Controller
                    }
                }
            }

            await _next(context);
        }
    }
}
