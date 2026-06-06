using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AntiGravityAuditService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            builder.Services.AddSingleton<ConnectionFactory>(sp =>
            {
                var rabbitUri = builder.Configuration["RabbitMQ:ConnectionString"]
                                ?? "amqp://guest:guest@rabbitmq-broker:5672/";
                return new ConnectionFactory() { Uri = new Uri(rabbitUri) };
            });

            builder.Services.AddHostedService<AuditorEspacialWorker>();

            var host = builder.Build();
            host.Run();
        }
    }

    public class AuditorEspacialWorker : BackgroundService
    {
        private readonly ConnectionFactory _factory;
        private readonly ILogger<AuditorEspacialWorker> _logger;
        private const string FilaNome = "fila-colisoes-criticas";

        public AuditorEspacialWorker(ConnectionFactory factory, ILogger<AuditorEspacialWorker> logger)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            IConnection? connection = null;
            IModel? channel = null;

            // Retry até o broker estar pronto — evita crash no startup quando o
            // container sobe antes do AMQP aceitar conexões.
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    connection = _factory.CreateConnection();
                    channel = connection.CreateModel();
                    break;
                }
                catch (BrokerUnreachableException ex)
                {
                    _logger.LogWarning("[BROKER] RabbitMQ ainda não disponível: {Msg}. Tentando novamente em 3s...", ex.Message);
                    await Task.Delay(3000, stoppingToken);
                }
            }

            if (channel == null || stoppingToken.IsCancellationRequested) return;

            _logger.LogInformation("🚀 [AUDITORIA] Inicializando canal de recepção do RabbitMQ...");

            channel.QueueDeclare(queue: FilaNome,
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            // prefetchCount: 1 garante fair dispatch entre instâncias do worker
            channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                try
                {
                    using var doc = JsonDocument.Parse(message);
                    var root = doc.RootElement;
                    string nomeId = root.TryGetProperty("nomeId", out var idProp) ? idProp.GetString() ?? "desconhecido" : "desconhecido";
                    double velocidadeY = root.TryGetProperty("velocidadeY", out var velProp) ? velProp.GetDouble() : 0.0;

                    if (Math.Abs(velocidadeY) > 5.0)
                        _logger.LogWarning("🚨 [AUDITORIA CRÍTICA] Impacto de alta velocidade! Objeto: '{Nome}'. Velocidade Y: {Vel} m/s", nomeId, velocidadeY);
                    else
                        _logger.LogInformation("ℹ️ [AUDITORIA LEVE] Impacto sob controle. Objeto: '{Nome}'. Velocidade Y: {Vel} m/s", nomeId, velocidadeY);

                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "💥 [AUDITORIA] Erro ao processar payload. Recompondo mensagem na fila...");
                    channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            channel.BasicConsume(queue: FilaNome, autoAck: false, consumer: consumer);

            // Mantém o worker vivo até o host solicitar cancelamento
            await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);

            channel.Close();
            channel.Dispose();
            connection.Close();
            connection.Dispose();
        }
    }
}
