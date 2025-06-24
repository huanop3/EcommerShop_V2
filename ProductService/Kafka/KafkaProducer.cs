using Confluent.Kafka;
using MainEcommerceService.Models.dbMainEcommer;
using System.Text.Json; // 🔥 CHANGE: Đổi từ Newtonsoft sang System.Text.Json

public interface IKafkaProducerService
{
    Task SendMessageAsync<T>(string topic, string key, T message);
    Task<SellerResponseMessage> GetSellerByUserIdAsync(int userId, int timeoutSeconds = 20);
    Task SendProductUpdateResultAsync(string orderKey, object result); // ✅ Thêm method này
}

public class KafkaProducerService : IKafkaProducerService, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducerService> _logger;
    private readonly Dictionary<string, TaskCompletionSource<SellerResponseMessage>> _pendingRequests;
    private readonly IConsumer<string, string> _responseConsumer;
    private readonly Task _responseListenerTask;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly object _lockObject = new object();

    public KafkaProducerService(IConfiguration configuration, ILogger<KafkaProducerService> logger)
    {
        _logger = logger;
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
            GroupId = "product-service-response-consumer", // 🔥 FIX: Fixed group ID
            AutoOffsetReset = AutoOffsetReset.Earliest, // 🔥 FIX: Đọc từ đầu
            EnableAutoCommit = true,
            SessionTimeoutMs = 10000,
            HeartbeatIntervalMs = 3000,
            MaxPollIntervalMs = 300000
        };

        _producer = new ProducerBuilder<string, string>(producerConfig)
            .SetErrorHandler((_, e) => _logger.LogError("Producer error: {Error}", e.Reason))
            .Build();

        _responseConsumer = new ConsumerBuilder<string, string>(consumerConfig)
            .SetErrorHandler((_, e) => _logger.LogError("Response consumer error: {Error}", e.Reason))
            .Build();

        _responseListenerTask = Task.Run(async () => await ListenForResponsesAsync(_cancellationTokenSource.Token));
    }

    public async Task SendMessageAsync<T>(string topic, string key, T message)
    {
        try
        {
            // 🔥 CHANGE: Sử dụng System.Text.Json
            var serializedMessage = JsonSerializer.Serialize(message);
            var result = await _producer.ProduceAsync(topic, new Message<string, string>
            {
                Key = key,
                Value = serializedMessage
            });

            _logger.LogInformation("✅ Message sent to topic {Topic}, partition {Partition}, offset {Offset}",
                result.Topic, result.Partition.Value, result.Offset.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to send message to topic {Topic}", topic);
            throw;
        }
    }

    public async Task<SellerResponseMessage> GetSellerByUserIdAsync(int userId, int timeoutSeconds = 20)
    {
        var requestId = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource<SellerResponseMessage>();

        lock (_lockObject)
        {
            _pendingRequests[requestId] = tcs;
        }

        try
        {
            _logger.LogInformation("🔍 ProductService: Sending GetSellerByUserIdAsync request: RequestId={RequestId}, UserId={UserId}", requestId, userId);

            var request = new SellerRequestMessage
            {
                Action = "GET_SELLER_BY_USER_ID",
                UserId = userId,
                RequestId = requestId
            };

            await SendMessageAsync("seller-request", requestId, request);
            _logger.LogInformation("✅ ProductService: Request sent successfully: RequestId={RequestId}", requestId);

            // 🔥 ADD: Kiểm tra pending requests
            lock (_lockObject)
            {
                _logger.LogInformation("📊 ProductService: Pending requests count: {Count}", _pendingRequests.Count);
            }

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

            try
            {
                var result = await tcs.Task.WaitAsync(cts.Token);
                _logger.LogInformation("✅ ProductService: Response received: RequestId={RequestId}, Success={Success}", requestId, result.Success);
                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("⏰ ProductService: Request timeout: RequestId={RequestId}, Timeout={Timeout}s", requestId, timeoutSeconds);
                
                // 🔥 ADD: Debug info khi timeout
                lock (_lockObject)
                {
                    _logger.LogWarning("📊 ProductService: Pending requests at timeout: {Count}", _pendingRequests.Count);
                    foreach (var kvp in _pendingRequests)
                    {
                        _logger.LogWarning("📋 ProductService: Pending RequestId: {RequestId}", kvp.Key);
                    }
                }
                
                throw new TimeoutException($"GetSellerByUserIdAsync timed out after {timeoutSeconds} seconds for RequestId: {requestId}");
            }
        }
        finally
        {
            lock (_lockObject)
            {
                _pendingRequests.Remove(requestId);
            }
        }
    }

    // ✅ Thêm method để gửi product update result
    public async Task SendProductUpdateResultAsync(string orderKey, object result)
    {
        try
        {
            _logger.LogInformation("📤 ProductService: Sending product update result for order {OrderKey}", orderKey);
            
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
            
            _logger.LogInformation("✅ ProductService: Product update result sent successfully - Topic: {Topic}, Partition: {Partition}, Offset: {Offset}", 
                deliveryResult.Topic, deliveryResult.Partition.Value, deliveryResult.Offset.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ ProductService: Failed to send product update result for order {OrderKey}", orderKey);
            throw;
        }
    }

    private async Task ListenForResponsesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var subscribed = false;
            var retryCount = 0;
            const int maxRetries = 10; // 🔥 FIX: Tăng retry count

            while (!subscribed && retryCount < maxRetries && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _responseConsumer.Subscribe("seller-response");
                    subscribed = true;
                    _logger.LogInformation("✅ ProductService: Successfully subscribed to seller-response topic");
                }
                catch (Exception ex)
                {
                    retryCount++;
                    _logger.LogWarning("⚠️ ProductService: Failed to subscribe to seller-response topic. Retry {RetryCount}/{MaxRetries}: {Error}",
                        retryCount, maxRetries, ex.Message);

                    if (retryCount < maxRetries)
                    {
                        await Task.Delay(3000, cancellationToken); // 🔥 FIX: Tăng delay
                    }
                }
            }

            if (!subscribed)
            {
                _logger.LogError("❌ ProductService: Failed to subscribe to seller-response topic after {MaxRetries} retries", maxRetries);
                return;
            }

            _logger.LogInformation("🔄 ProductService: Starting response listener loop...");

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = _responseConsumer.Consume(TimeSpan.FromMilliseconds(2000)); // 🔥 FIX: Tăng consume timeout

                    if (result != null)
                    {
                        _logger.LogInformation("📨 ProductService: Raw response received: Key={Key}, Value={Value}",
                            result.Message.Key, result.Message.Value);

                        try
                        {
                            // 🔥 CHANGE: Sử dụng System.Text.Json
                            var response = JsonSerializer.Deserialize<SellerResponseMessage>(result.Message.Value);

                            if (response != null && !string.IsNullOrEmpty(response.RequestId))
                            {
                                _logger.LogInformation("📥 ProductService: Processing response: RequestId={RequestId}, Success={Success}, Data={HasData}",
                                    response.RequestId, response.Success, response.Data != null);

                                TaskCompletionSource<SellerResponseMessage> tcs = null;

                                lock (_lockObject)
                                {
                                    _pendingRequests.TryGetValue(response.RequestId, out tcs);
                                }

                                if (tcs != null)
                                {
                                    tcs.SetResult(response);
                                    _logger.LogInformation("✅ ProductService: Response delivered to waiting request: RequestId={RequestId}", response.RequestId);

                                    lock (_lockObject)
                                    {
                                        _pendingRequests.Remove(response.RequestId);
                                    }
                                }
                                else
                                {
                                    _logger.LogWarning("⚠️ ProductService: No pending request found for RequestId: {RequestId}", response.RequestId);
                                    
                                    // 🔥 ADD: Debug pending requests
                                    lock (_lockObject)
                                    {
                                        _logger.LogWarning("📊 ProductService: Current pending requests: {Count}", _pendingRequests.Count);
                                        foreach (var kvp in _pendingRequests)
                                        {
                                            _logger.LogWarning("📋 ProductService: Waiting RequestId: {RequestId}", kvp.Key);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                _logger.LogWarning("⚠️ ProductService: Invalid response format: {Response}", result.Message.Value);
                            }
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogError(ex, "❌ ProductService: Failed to deserialize response: {Value}", result.Message.Value);
                        }
                    }
                    else
                    {
                        // 🔥 ADD: Log khi không có message
                        _logger.LogDebug("🔍 ProductService: No message received in this poll cycle");
                    }
                }
                catch (ConsumeException ex) when (ex.Error.Code == ErrorCode.UnknownTopicOrPart)
                {
                    _logger.LogWarning("⚠️ ProductService: Topic 'seller-response' not found. Waiting...");
                    await Task.Delay(5000, cancellationToken);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "❌ ProductService: Error consuming response message: {Error}", ex.Error.Reason);
                    await Task.Delay(2000, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ ProductService: Unexpected error in response listener");
                    await Task.Delay(2000, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ ProductService: Fatal error in response listener");
        }
        finally
        {
            _logger.LogInformation("🛑 ProductService: Response listener stopped");
        }
    }

    public void Dispose()
    {
        _logger.LogInformation("🛑 ProductService: Disposing KafkaProducerService...");

        _cancellationTokenSource?.Cancel();

        try
        {
            _responseListenerTask?.Wait(5000);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ProductService: Error waiting for response listener to stop");
        }

        _producer?.Dispose();
        _responseConsumer?.Dispose();
        _cancellationTokenSource?.Dispose();

        _logger.LogInformation("✅ ProductService: KafkaProducerService disposed");
    }
}