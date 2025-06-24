using Confluent.Kafka;
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

        public KafkaProducerService()
        {
            var config = new ProducerConfig
            {
                BootstrapServers = "kafka:29092",
                Acks = Acks.All,
                MessageTimeoutMs = 10000,
                RequestTimeoutMs = 10000,
                EnableIdempotence = true,
                MaxInFlight = 1,
                Partitioner = Partitioner.Murmur2Random
            };

            _producer = new ProducerBuilder<string, string>(config).Build();
        }

        public async Task SendMessageAsync<T>(string topic, string key, T message)
        {
            var json = JsonSerializer.Serialize(message);
            
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

            await _producer.ProduceAsync(topic, kafkaMessage);
        }

        public void Dispose()
        {
            _producer?.Flush(TimeSpan.FromSeconds(10));
            _producer?.Dispose();
        }
    }
}