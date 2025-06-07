using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MainEcommerceService.Kafka
{
    public interface IKafkaProducerService
    {
        Task SendMessageAsync<T>(string topic, string key, T message);
    }

    public class KafkaProducerService : IKafkaProducerService, IDisposable
    {
        private readonly IProducer<string, string> _producer;
        private readonly ILogger<KafkaProducerService> _logger;

        public KafkaProducerService(ILogger<KafkaProducerService> logger)
        {
            _logger = logger;
            
            var config = new ProducerConfig
            {
                BootstrapServers = "localhost:9092",
                Acks = Acks.All,
                MessageTimeoutMs = 10000,
                RequestTimeoutMs = 10000,
                EnableIdempotence = true,
                MaxInFlight = 1,
                Partitioner = Partitioner.Murmur2Random
            };

            _producer = new ProducerBuilder<string, string>(config)
                .SetErrorHandler((_, e) => 
                {
                    _logger.LogError("❌ MainEcommerce Producer error: {Error}", e.Reason);
                })
                .SetLogHandler((_, log) =>
                {
                    _logger.LogDebug("📋 MainEcommerce Producer log: {Level} - {Message}", log.Level, log.Message);
                })
                .Build();
                
            _logger.LogInformation("🚀 MainEcommerce: Kafka Producer initialized");
        }

        public async Task SendMessageAsync<T>(string topic, string key, T message)
        {
            try
            {
                _logger.LogInformation("📤 MainEcommerce: Preparing to send message to topic '{Topic}' with key '{Key}'", topic, key);
                
                var json = JsonSerializer.Serialize(message, new JsonSerializerOptions 
                { 
                    WriteIndented = false 
                });
                
                _logger.LogDebug("📋 MainEcommerce: Message JSON: {Json}", json);
                
                var kafkaMessage = new Message<string, string>
                {
                    Key = key,
                    Value = json,
                    Headers = new Headers()
                    {
                        { "MessageType", System.Text.Encoding.UTF8.GetBytes(typeof(T).Name) },
                        { "Timestamp", System.Text.Encoding.UTF8.GetBytes(DateTimeOffset.UtcNow.ToString()) }
                    }
                };

                _logger.LogInformation("📡 MainEcommerce: Sending message to Kafka...");
                
                var result = await _producer.ProduceAsync(topic, kafkaMessage);

                _logger.LogInformation("✅ MainEcommerce: Message sent successfully - Topic: {Topic}, Partition: {Partition}, Offset: {Offset}, Key: {Key}", 
                    result.Topic, result.Partition.Value, result.Offset.Value, key);
            }
            catch (ProduceException<string, string> ex)
            {
                _logger.LogError(ex, "❌ MainEcommerce: Failed to produce message to topic '{Topic}' with key '{Key}' - Error: {Error}", 
                    topic, key, ex.Error.Reason);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ MainEcommerce: Unexpected error sending message to topic '{Topic}' with key '{Key}'", topic, key);
                throw;
            }
        }

        public void Dispose()
        {
            try
            {
                _producer?.Flush(TimeSpan.FromSeconds(10));
                _producer?.Dispose();
                _logger.LogInformation("🛑 MainEcommerce: Kafka Producer disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ MainEcommerce: Error disposing Kafka Producer");
            }
        }
    }
}