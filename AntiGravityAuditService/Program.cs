using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
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
            
            // Registrando a Conexão com o Broker RabbitMQ
            builder.Services.AddSingleton<IConnection>(sp =>
            {
                var rabbitUri = builder.Configuration["RabbitMQ:ConnectionString"] 
                                ?? "amqp://guest:guest@rabbitmq-broker:5672/";
                var factory = new ConnectionFactory() { Uri = new Uri(rabbitUri) };
                return factory.CreateConnection();
            });

            // Registrando o Worker de Auditoria Espacial
            builder.Services.AddHostedService<AuditorEspacialWorker>();

            var host = builder.Build();
            host.Run();
        }
    }

    // ============================================================================
    // ⚙️ WORKER DE AUDITORIA DE COLISÕES (BACKGROUND CONSUMER COM QoS)
    // ============================================================================
    public class AuditorEspacialWorker : BackgroundService
    {
        private readonly IConnection _connection;
        private readonly ILogger<AuditorEspacialWorker> _logger;
        private IModel? _channel;
        private const string FilaNome = "fila-colisoes-criticas";

        public AuditorEspacialWorker(IConnection connection, ILogger<AuditorEspacialWorker> logger)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 [AUDITORIA] Inicializando canal de recepção do RabbitMQ...");

            _channel = _connection.CreateModel();

            // Declaração de Fila Durável (Durable: true) contra quedas orbitais
            _channel.QueueDeclare(queue: FilaNome,
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            // ============================================================================
            // 🎛️ CONFIGURAÇÃO DE QoS (DISTRIBUIÇÃO JUSTA DE ENERGIA)
            // ============================================================================
            // prefetchCount de 1 garante distribuição justa (fair dispatch): o broker não
            // entrega nova mensagem até que a anterior tenha sido processada e feito o ack.
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                try
                {
                    using (var doc = JsonDocument.Parse(message))
                    {
                        var root = doc.RootElement;
                        string nomeId = root.TryGetProperty("nomeId", out var idProp) ? idProp.GetString() ?? "desconhecido" : "desconhecido";
                        double velocidadeY = root.TryGetProperty("velocidadeY", out var velProp) ? velProp.GetDouble() : 0.0;

                        if (Math.Abs(velocidadeY) > 5.0)
                        {
                            _logger.LogWarning("🚨 [AUDITORIA CRÍTICA] Impacto de alta velocidade! Objeto: '{Nome}'. Velocidade Y: {Vel} m/s", nomeId, velocidadeY);
                        }
                        else
                        {
                            _logger.LogInformation("ℹ️ [AUDITORIA LEVE] Impacto sob controle. Objeto: '{Nome}'. Velocidade Y: {Vel} m/s", nomeId, velocidadeY);
                        }
                    }

                    // Acknowledge (Aviso de recebimento) manual para garantir resiliência
                    _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "💥 [AUDITORIA] Erro ao processar payload. Recompondo mensagem na fila...");
                    _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            // Inicia o consumo na fila com autoAck desativado (Ack manual)
            _channel.BasicConsume(queue: FilaNome,
                                 autoAck: false,
                                 consumer: consumer);

            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            _channel?.Close();
            _channel?.Dispose();
            base.Dispose();
        }
    }
}
