using Confluent.Kafka;
using Confluent.Kafka.Admin;
using MainEcommerceService.Models.dbMainEcommer;
using MainEcommerceService.Models.Kafka;
using System.Text.Json;

public class KafkaConsumerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KafkaConsumerService> _logger;
    private readonly string _bootstrapServers = "localhost:9092";

    public KafkaConsumerService(IServiceScopeFactory scopeFactory, ILogger<KafkaConsumerService> logger)
    {
        try
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("🚀 ProductService: KafkaConsumerService constructor SUCCESS");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ KafkaConsumerService constructor failed: {ex}");
            logger?.LogError(ex, "❌ ProductService: KafkaConsumerService constructor failed");
            throw;
        }
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 ProductService: KafkaConsumerService ExecuteAsync STARTING");
        
        return Task.Run(async () =>
        {
            try
            {
                _logger.LogInformation("🔧 ProductService: About to ensure topics exist");
                await EnsureTopicsExistAsync();
                
                _logger.LogInformation("🔧 ProductService: Starting consumer tasks independently");
                
                // ✅ Chạy riêng từng consumer với Task.Run độc lập
                var task1 = Task.Run(async () =>
                {
                    try
                    {
                        _logger.LogInformation("🔧 ProductService: Starting seller-events consumer...");
                        await ConsumeAsync("seller-events", stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ ProductService: Error in seller-events consumer");
                    }
                }, stoppingToken);
                
                var task2 = Task.Run(async () =>
                {
                    try
                    {
                        _logger.LogInformation("🔧 ProductService: Starting order-created consumer...");
                        await ConsumeOrderCreatedAsync("order-created", stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ ProductService: Error in order-created consumer");
                    }
                }, stoppingToken);
                
                _logger.LogInformation("🔧 ProductService: Both consumer tasks started independently");
                
                // Chờ cả hai task
                await Task.WhenAll(task1, task2);
                
                _logger.LogInformation("🔧 ProductService: All consumer tasks completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ ProductService: FATAL ERROR in ExecuteAsync");
                throw;
            }
        }, stoppingToken);
    }

    // 🔥 SỬA: Consumer cho order created - Fixed configuration
    private async Task ConsumeOrderCreatedAsync(string topic, CancellationToken stoppingToken)
    {
        // ✅ THÊM: Log ngay đầu method
        _logger.LogInformation("🚀🚀🚀 ProductService: ConsumeOrderCreatedAsync method CALLED for topic {Topic}", topic);
        
        try
        {
            _logger.LogInformation("🔧 ProductService: Creating consumer config for {Topic}", topic);
            
            var consumerConfig = new ConsumerConfig
            {
                GroupId = "product-service-order-consumer",
                BootstrapServers = _bootstrapServers,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false,
                SessionTimeoutMs = 10000,
                HeartbeatIntervalMs = 3000,
                EnablePartitionEof = false,
                AllowAutoCreateTopics = true,
                SecurityProtocol = SecurityProtocol.Plaintext
            };

            _logger.LogInformation("🔧 ProductService: Building consumer for {Topic}", topic);
            
            using var consumer = new ConsumerBuilder<string, string>(consumerConfig)
                .SetErrorHandler((_, e) => 
                {
                    _logger.LogError("❌ ProductService Order Consumer error: {Error} - Code: {Code}", e.Reason, e.Code);
                })
                .SetLogHandler((_, log) =>
                {
                    if (log.Level <= SyslogLevel.Warning)
                    {
                        _logger.LogInformation("📋 ProductService Order Consumer: {Level} - {Message}", log.Level, log.Message);
                    }
                })
                .SetPartitionsAssignedHandler((c, partitions) =>
                {
                    _logger.LogInformation("✅ ProductService: Order consumer assigned partitions: [{Partitions}]", 
                        string.Join(", ", partitions.Select(p => $"{p.Topic}[{p.Partition}]")));
                })
                .SetPartitionsRevokedHandler((c, partitions) =>
                {
                    _logger.LogInformation("🔄 ProductService: Order consumer revoked partitions: [{Partitions}]", 
                        string.Join(", ", partitions.Select(p => $"{p.Topic}[{p.Partition}]")));
                })
                .Build();

            _logger.LogInformation("✅ ProductService: Consumer created successfully for {Topic}", topic);

            _logger.LogInformation("🔧 ProductService: About to subscribe to {Topic}", topic);
            
            consumer.Subscribe(topic);
            _logger.LogInformation("✅ ProductService: Successfully subscribed to {Topic}", topic);

            _logger.LogInformation("🚀 ProductService: Starting message consumption loop for {Topic}", topic);
            
            // Trong vòng lặp consumption:
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = consumer.Consume(TimeSpan.FromSeconds(10));
                    
                    if (consumeResult != null && consumeResult.Message != null)
                    {
                        _logger.LogInformation("📨 ProductService: Received message from {Topic} - Key: {Key}, Size: {Size}bytes", 
                            topic, consumeResult.Message.Key, consumeResult.Message.Value?.Length ?? 0);
                        
                        _logger.LogInformation("🔄 ProductService: About to process message for key {Key}", consumeResult.Message.Key);
                        
                        // ✅ SỬA: Sử dụng AsyncServiceScope thay vì ServiceScope
                        await using var scope = _scopeFactory.CreateAsyncScope();
                        try
                        {
                            _logger.LogInformation("🔄 ProductService: Calling ProcessOrderMessageAsync...");
                            await ProcessOrderMessageAsync(scope.ServiceProvider, consumeResult.Message.Value, consumeResult.Message.Key);
                            
                            consumer.Commit(consumeResult);
                            _logger.LogInformation("✅ ProductService: Successfully processed and committed message - Key: {Key}", consumeResult.Message.Key);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "❌ ProductService: Error processing message - Key: {Key}", consumeResult.Message.Key);
                            // Không commit nếu có lỗi
                        }
                        // ✅ AsyncServiceScope sẽ tự động dispose async
                    }
                    else
                    {
                        // Log occasionally to avoid spam
                        if (DateTime.Now.Second % 30 == 0)
                        {
                            _logger.LogDebug("🔍 ProductService: Waiting for messages in {Topic}...", topic);
                        }
                    }
                }
                catch (ConsumeException ex) when (ex.Error.Code == ErrorCode.RequestTimedOut)
                {
                    _logger.LogDebug("🕐 ProductService: Consumer timeout - continuing...");
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "❌ ProductService: Error consuming message from {Topic}", topic);
                    await Task.Delay(2000, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ ProductService: Unexpected error in consumer loop");
                    await Task.Delay(2000, stoppingToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ ProductService: FATAL ERROR in ConsumeOrderCreatedAsync for topic {Topic}", topic);
            throw;
        }
        finally
        {
            _logger.LogInformation("🛑 ProductService: ConsumeOrderCreatedAsync method ENDED for topic {Topic}", topic);
        }
    }

    // Thay đổi signature của ProcessOrderMessageAsync
    private async Task ProcessOrderMessageAsync(IServiceProvider serviceProvider, string messageValue, string orderKey)
    {
        try
        {
            _logger.LogInformation("🔄 ProductService: ProcessOrderMessageAsync STARTED for key {OrderKey}", orderKey);
            
            // ✅ Sử dụng serviceProvider trực tiếp
            var productService = serviceProvider.GetRequiredService<IProdService>();
            _logger.LogInformation("✅ ProductService: IProdService resolved successfully");
            
            // ✅ Test deserialize
            _logger.LogInformation("🔄 ProductService: About to deserialize message...");
            var orderMessage = JsonSerializer.Deserialize<OrderCreatedMessage>(messageValue);
            
            if (orderMessage == null)
            {
                _logger.LogError("❌ ProductService: Deserialized message is null");
                return;
            }
            
            _logger.LogInformation("✅ ProductService: Message deserialized successfully - OrderId: {OrderId}, Items: {ItemCount}", 
                orderMessage.OrderId, orderMessage.OrderItems?.Count ?? 0);
            
            // ✅ Test ProcessOrderItems
            _logger.LogInformation("🔄 ProductService: About to call ProcessOrderItems...");
            var result = await productService.ProcessOrderItems(orderMessage);
            
            if (result?.Data != null)
            {
                _logger.LogInformation("✅ ProductService: ProcessOrderItems completed - Success: {Success}", result.Data.Success);
                
                // ✅ SỬA: Tạo producer để gửi kết quả
                _logger.LogInformation("🔄 ProductService: About to send product update result...");
                await SendProductUpdateResultDirectAsync(orderKey, result.Data);
                
                _logger.LogInformation("✅ ProductService: Successfully sent product update result for order {OrderKey}", orderKey);
            }
            else
            {
                _logger.LogError("❌ ProductService: ProcessOrderItems returned null for order {OrderKey}", orderKey);
                
                var errorResult = new ProductUpdateMessage
                {
                    RequestId = orderMessage?.RequestId ?? Guid.NewGuid().ToString(),
                    OrderId = orderMessage?.OrderId ?? 0,
                    Success = false,
                    ErrorMessage = "Internal processing error",
                    UpdatedProducts = new List<ProductUpdateResult>()
                };
                
                await SendProductUpdateResultDirectAsync(orderKey, errorResult);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ ProductService: Error in ProcessOrderMessageAsync for key {OrderKey}", orderKey);
            
            var errorResult = new ProductUpdateMessage
            {
                RequestId = Guid.NewGuid().ToString(),
                OrderId = 0,
                Success = false,
                ErrorMessage = $"Processing error: {ex.Message}",
                UpdatedProducts = new List<ProductUpdateResult>()
            };
            
            await SendProductUpdateResultDirectAsync(orderKey, errorResult);
        }
    }

    // ✅ THÊM: Method mới để gửi kết quả trực tiếp
    private async Task SendProductUpdateResultDirectAsync(string orderKey, object result)
    {
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _bootstrapServers,
            Acks = Acks.All,
            MessageTimeoutMs = 10000,
            RequestTimeoutMs = 5000
        };

        using var producer = new ProducerBuilder<string, string>(producerConfig)
            .SetErrorHandler((_, e) => 
            {
                _logger.LogError("❌ ProductService Producer error: {Error}", e.Reason);
            })
            .Build();

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

            var deliveryResult = await producer.ProduceAsync("product-update-result", message);
            
            _logger.LogInformation("✅ ProductService: Product update result sent successfully - Topic: {Topic}, Partition: {Partition}, Offset: {Offset}", 
                deliveryResult.Topic, deliveryResult.Partition.Value, deliveryResult.Offset.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ ProductService: Failed to send product update result for order {OrderKey}", orderKey);
            throw;
        }
    }

    // Keep existing SendProductUpdateResultAsync method unchanged
    private async Task SendProductUpdateResultAsync(IProducer<string, string> producer, ProductUpdateMessage updateResult)
    {
        var maxRetries = 3;
        var retryCount = 0;
        
        while (retryCount < maxRetries)
        {
            try
            {
                if (updateResult == null)
                {
                    _logger.LogError("❌ ProductService: Cannot send null ProductUpdateMessage");
                    return;
                }

                var resultJson = JsonSerializer.Serialize(updateResult, new JsonSerializerOptions 
                { 
                    WriteIndented = false 
                });
                
                _logger.LogInformation("📤 ProductService: Sending product update result (attempt {Attempt}): RequestId={RequestId}, OrderId={OrderId}, Success={Success}", 
                    retryCount + 1, updateResult.RequestId, updateResult.OrderId, updateResult.Success);

                _logger.LogDebug("📋 ProductService: Result JSON: {Json}", resultJson);

                var message = new Message<string, string>
                {
                    Key = updateResult.OrderId.ToString(),
                    Value = resultJson,
                    Headers = new Headers()
                    {
                        { "RequestId", System.Text.Encoding.UTF8.GetBytes(updateResult.RequestId ?? "unknown") },
                        { "OrderId", System.Text.Encoding.UTF8.GetBytes(updateResult.OrderId.ToString()) },
                        { "Success", System.Text.Encoding.UTF8.GetBytes(updateResult.Success.ToString()) },
                        { "Timestamp", System.Text.Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("O")) }
                    }
                };

                var result = await producer.ProduceAsync("product-update-result", message);
                
                _logger.LogInformation("✅ ProductService: Product update result sent successfully - OrderId={OrderId}, Topic={Topic}, Partition={Partition}, Offset={Offset}", 
                    updateResult.OrderId, result.Topic, result.Partition.Value, result.Offset.Value);
                
                return;
            }
            catch (Exception ex) when (retryCount < maxRetries - 1)
            {
                retryCount++;
                _logger.LogWarning(ex, "⚠️ ProductService: Failed to send product update result (attempt {Attempt}/{MaxRetries}) for order {OrderId}: {Message}", 
                    retryCount, maxRetries, updateResult?.OrderId, ex.Message);
                await Task.Delay(1000 * retryCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ ProductService: Failed to send product update result for order {OrderId} after {MaxRetries} attempts", 
                    updateResult?.OrderId, maxRetries);
                throw;
            }
        }
    }

    // Keep existing EnsureTopicsExistAsync method unchanged
    private async Task EnsureTopicsExistAsync()
    {
        var config = new AdminClientConfig { BootstrapServers = _bootstrapServers };
        
        using var adminClient = new AdminClientBuilder(config).Build();
        
        var requiredTopics = new[] { 
            "seller-events",
            "order-created",
            "product-update-result"
        };

        try
        {
            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
            var existingTopics = metadata.Topics.Select(t => t.Topic).ToHashSet();

            var topicsToCreate = requiredTopics.Where(topic => !existingTopics.Contains(topic)).ToList();

            if (topicsToCreate.Any())
            {
                var topicSpecs = topicsToCreate.Select(topic => new TopicSpecification
                {
                    Name = topic,
                    NumPartitions = 1,
                    ReplicationFactor = 1
                }).ToList();

                await adminClient.CreateTopicsAsync(topicSpecs);
                _logger.LogInformation("✅ ProductService: Created topics: {Topics}", string.Join(", ", topicsToCreate));
            }
            else
            {
                _logger.LogInformation("✅ ProductService: All required topics already exist: {Topics}", string.Join(", ", requiredTopics));
            }
        }
        catch (CreateTopicsException ex)
        {
            foreach (var result in ex.Results)
            {
                if (result.Error.Code != ErrorCode.TopicAlreadyExists)
                {
                    _logger.LogError("❌ ProductService: Failed to create topic {Topic}: {Error}", result.Topic, result.Error.Reason);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ ProductService: Error ensuring topics exist");
        }
    }

    // Keep existing ConsumeAsync method unchanged
    private async Task ConsumeAsync(string topic, CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            GroupId = "product-service-consumer",
            BootstrapServers = _bootstrapServers,
            AutoOffsetReset = AutoOffsetReset.Latest,
            EnableAutoCommit = false,
            SessionTimeoutMs = 6000,
            HeartbeatIntervalMs = 2000
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        
        var retryCount = 0;
        const int maxRetries = 5;
        
        while (retryCount < maxRetries && !stoppingToken.IsCancellationRequested)
        {
            try
            {
                consumer.Subscribe(topic);
                _logger.LogInformation("✅ ProductService: Successfully subscribed to {Topic}", topic);
                break;
            }
            catch (Exception ex)
            {
                retryCount++;
                _logger.LogWarning(ex, "Failed to subscribe to {Topic}. Retry {RetryCount}/{MaxRetries}", topic, retryCount, maxRetries);
                
                if (retryCount < maxRetries)
                {
                    await Task.Delay(2000, stoppingToken);
                }
            }
        }

        if (retryCount >= maxRetries)
        {
            _logger.LogError("Failed to subscribe to {Topic} after {MaxRetries} attempts", topic, maxRetries);
            return;
        }

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = consumer.Consume(TimeSpan.FromMilliseconds(1000));
                    
                    if (consumeResult != null)
                    {
                        using var scope = _scopeFactory.CreateScope();
                        await ProcessMessageAsync(scope, consumeResult.Message.Value);
                        consumer.Commit(consumeResult);
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming Kafka message from {Topic}", topic);
                    await Task.Delay(1000, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in consumer loop");
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Kafka consumer service is stopping.");
        }
        finally
        {
            consumer.Close();
        }
    }

    // Keep existing ProcessMessageAsync method unchanged
    private async Task ProcessMessageAsync(IServiceScope scope, string messageValue)
    {
        try
        {
            var message = JsonSerializer.Deserialize<SellerProfileVM>(messageValue);

            if (message?.IsDeleted == true)
            {
                var productService = scope.ServiceProvider.GetRequiredService<IProdService>();
                
                await productService.DeleteProductsBySellerId(message.SellerId);
                
                _logger.LogInformation("Successfully deleted products for seller {SellerId}", message.SellerId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing seller deleted message: {Message}", messageValue);
            throw;
        }
    }
}