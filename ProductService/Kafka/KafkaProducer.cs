using Confluent.Kafka;
using MainEcommerceService.Models.dbMainEcommer;
using System.Text.Json;

public interface IKafkaProducerService
{
    Task SendMessageAsync<T>(string topic, string key, T message);
    Task<SellerResponseMessage> GetSellerByUserIdAsync(int userId, int timeoutSeconds = 20);
    Task SendProductUpdateResultAsync(string orderKey, object result);
}

public class KafkaProducerService : IKafkaProducerService, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly Dictionary<string, TaskCompletionSource<SellerResponseMessage>> _pendingRequests;
    private readonly IConsumer<string, string> _responseConsumer;
    private readonly Task _responseListenerTask;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly object _lockObject = new object();

    public KafkaProducerService(IConfiguration configuration)
    {
        _pendingRequests = new Dictionary<string, TaskCompletionSource<SellerResponseMessage>>();
        _cancellationTokenSource = new CancellationTokenSource();

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = "kafka:29092",
            Acks = Acks.All,
            MessageSendMaxRetries = 3,
            RetryBackoffMs = 1000
        };

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = "kafka:29092",
            GroupId = "product-service-response-consumer",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true,
            SessionTimeoutMs = 10000,
            HeartbeatIntervalMs = 3000,
            MaxPollIntervalMs = 300000
        };

        _producer = new ProducerBuilder<string, string>(producerConfig)
            .Build();

        _responseConsumer = new ConsumerBuilder<string, string>(consumerConfig)
            .Build();

        _responseListenerTask = Task.Run(async () => await ListenForResponsesAsync(_cancellationTokenSource.Token));
    }

    public async Task SendMessageAsync<T>(string topic, string key, T message)
    {
        var serializedMessage = JsonSerializer.Serialize(message);
        var result = await _producer.ProduceAsync(topic, new Message<string, string>
        {
            Key = key,
            Value = serializedMessage
        });
    }

    public async Task<SellerResponseMessage> GetSellerByUserIdAsync(int userId, int timeoutSeconds = 20)
    {
        var requestId = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource<SellerResponseMessage>();

        lock (_lockObject)
        {
            _pendingRequests[requestId] = tcs;
        }

        var request = new SellerRequestMessage
        {
            Action = "GET_SELLER_BY_USER_ID",
            UserId = userId,
            RequestId = requestId
        };

        await SendMessageAsync("seller-request", requestId, request);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

        var result = await tcs.Task.WaitAsync(cts.Token);
        
        lock (_lockObject)
        {
            _pendingRequests.Remove(requestId);
        }
        
        return result;
    }

    public async Task SendProductUpdateResultAsync(string orderKey, object result)
    {
        var resultJson = JsonSerializer.Serialize(result);
        
        var message = new Message<string, string>
        {
            Key = orderKey,
            Value = resultJson,
            Headers = new Headers()
            {
                { "OrderId", System.Text.Encoding.UTF8.GetBytes(orderKey) },
                { "Timestamp", System.Text.Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("O")) }
            }
        };

        var deliveryResult = await _producer.ProduceAsync("product-update-result", message);
    }

    private async Task ListenForResponsesAsync(CancellationToken cancellationToken)
    {
        var subscribed = false;
        var retryCount = 0;
        const int maxRetries = 10;

        while (!subscribed && retryCount < maxRetries && !cancellationToken.IsCancellationRequested)
        {
            _responseConsumer.Subscribe("seller-response");
            subscribed = true;
            retryCount++;

            if (retryCount < maxRetries)
            {
                await Task.Delay(3000, cancellationToken);
            }
        }

        if (!subscribed)
        {
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            var result = _responseConsumer.Consume(TimeSpan.FromMilliseconds(2000));

            if (result != null)
            {
                var response = JsonSerializer.Deserialize<SellerResponseMessage>(result.Message.Value);

                if (response != null && !string.IsNullOrEmpty(response.RequestId))
                {
                    TaskCompletionSource<SellerResponseMessage> tcs = null;

                    lock (_lockObject)
                    {
                        _pendingRequests.TryGetValue(response.RequestId, out tcs);
                    }

                    if (tcs != null)
                    {
                        tcs.SetResult(response);

                        lock (_lockObject)
                        {
                            _pendingRequests.Remove(response.RequestId);
                        }
                    }
                }
            }
            
            await Task.Delay(2000, cancellationToken);
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Cancel();
        _responseListenerTask?.Wait(5000);
        _producer?.Dispose();
        _responseConsumer?.Dispose();
        _cancellationTokenSource?.Dispose();
    }
}