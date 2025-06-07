using Confluent.Kafka;
using Confluent.Kafka.Admin;
using MainEcommerceService.Hubs;
using MainEcommerceService.Models.dbMainEcommer;
using MainEcommerceService.Models.Kafka;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;

public class KafkaConsumerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KafkaConsumerService> _logger;
    private readonly string _bootstrapServers = "localhost:9092";

    public KafkaConsumerService(IServiceScopeFactory scopeFactory, ILogger<KafkaConsumerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 MainEcommerce: KafkaConsumerService ExecuteAsync STARTING");
        
        return Task.Run(async () =>
        {
            try
            {
                _logger.LogInformation("🔧 MainEcommerce: About to ensure topics exist");
                await EnsureTopicsExistAsync();
                
                _logger.LogInformation("🔧 MainEcommerce: Starting consumer tasks");
                
                // ✅ Chạy riêng từng consumer với error handling
                var task1 = Task.Run(async () =>
                {
                    try
                    {
                        _logger.LogInformation("🔧 MainEcommerce: Starting seller-request consumer...");
                        await ConsumeAsync("seller-request", stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ MainEcommerce: Error in seller-request consumer");
                    }
                }, stoppingToken);
                
                var task2 = Task.Run(async () =>
                {
                    try
                    {
                        _logger.LogInformation("🔧 MainEcommerce: Starting product-update-result consumer...");
                        await ConsumeProductUpdateResultAsync("product-update-result", stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ MainEcommerce: Error in product-update-result consumer");
                    }
                }, stoppingToken);
                
                _logger.LogInformation("🔧 MainEcommerce: Both consumer tasks started");
                
                await Task.WhenAll(task1, task2);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ MainEcommerce: FATAL ERROR in ExecuteAsync");
                throw;
            }
        }, stoppingToken);
    }

    private async Task ConsumeAsync(string topic, CancellationToken stoppingToken)
    {
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = "main-ecommerce-seller-request",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            SessionTimeoutMs = 6000,
            HeartbeatIntervalMs = 2000
        };

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _bootstrapServers,
            Acks = Acks.All
        };

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig)
            .SetErrorHandler((_, e) => _logger.LogError("Consumer error: {Error}", e.Reason))
            .Build();
        using var producer = new ProducerBuilder<string, string>(producerConfig)
            .SetErrorHandler((_, e) => _logger.LogError("Producer error: {Error}", e.Reason))
            .Build();

        // Retry subscription
        var retryCount = 0;
        const int maxRetries = 5;
        
        while (retryCount < maxRetries && !stoppingToken.IsCancellationRequested)
        {
            try
            {
                consumer.Subscribe(topic);
                _logger.LogInformation("✅ MainEcommerce: Successfully subscribed to {Topic}", topic);
                break;
            }
            catch (Exception ex)
            {
                retryCount++;
                _logger.LogWarning(ex, "⚠️ MainEcommerce: Failed to subscribe to {Topic}. Retry {RetryCount}/{MaxRetries}", topic, retryCount, maxRetries);
                
                if (retryCount < maxRetries)
                {
                    await Task.Delay(2000, stoppingToken);
                }
            }
        }

        if (retryCount >= maxRetries)
        {
            _logger.LogError("❌ MainEcommerce: Failed to subscribe to {Topic} after {MaxRetries} attempts", topic, maxRetries);
            return;
        }

        _logger.LogInformation("🔄 MainEcommerce: Starting seller request listener for topic {Topic}...", topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = consumer.Consume(TimeSpan.FromMilliseconds(1000));
                    
                    if (consumeResult != null)
                    {
                        _logger.LogInformation("📨 MainEcommerce: Received request: Key={Key}, Value={Value}", 
                            consumeResult.Message.Key, consumeResult.Message.Value);

                        // ✅ FIX: Proper scope management
                        var scope = _scopeFactory.CreateScope();
                        try
                        {
                            await ProcessMessageAsync(scope.ServiceProvider, producer, consumeResult.Message.Value);
                            consumer.Commit(consumeResult);
                            _logger.LogInformation("✅ MainEcommerce: Successfully processed and committed message");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "❌ MainEcommerce: Error processing message - will not commit");
                        }
                        finally
                        {
                            // ✅ Safe disposal
                            await DisposeServiceScopeAsync(scope);
                        }
                    }
                }
                catch (ConsumeException ex) when (ex.Error.Code == ErrorCode.UnknownTopicOrPart)
                {
                    _logger.LogWarning("⚠️ MainEcommerce: Topic '{Topic}' not found. Waiting...", topic);
                    await Task.Delay(5000, stoppingToken);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "❌ MainEcommerce: Error consuming Kafka message from {Topic}", topic);
                    await Task.Delay(1000, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ MainEcommerce: Unexpected error in consumer loop");
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("🛑 MainEcommerce: Kafka consumer service is stopping.");
        }
        finally
        {
            consumer.Close();
        }
    }

    private async Task ProcessMessageAsync(IServiceProvider serviceProvider, IProducer<string, string> producer, string messageValue)
    {
        try
        {
            _logger.LogInformation("🔍 MainEcommerce: Processing message: {Message}", messageValue);
            
            var request = JsonSerializer.Deserialize<SellerRequestMessage>(messageValue);
            
            if (request == null)
            {
                _logger.LogWarning("⚠️ MainEcommerce: Failed to deserialize request message");
                return;
            }

            _logger.LogInformation("📋 MainEcommerce: Parsed request - RequestId={RequestId}, Action={Action}, UserId={UserId}", 
                request.RequestId, request.Action, request.UserId);
            
            if (request.Action == "GET_SELLER_BY_USER_ID")
            {
                var sellerProfileService = serviceProvider.GetRequiredService<ISellerProfileService>();

                _logger.LogInformation("🔍 MainEcommerce: Getting seller for UserId={UserId}", request.UserId);

                var sellerResponse = await sellerProfileService.GetSellerProfileByUserId(request.UserId);

                var responseMessage = new SellerResponseMessage
                {
                    RequestId = request.RequestId,
                    Success = sellerResponse.Success
                };

                if (sellerResponse.Success && sellerResponse.Data != null)
                {
                    responseMessage.Data = new SellerProfileVM
                    {
                        SellerId = sellerResponse.Data.SellerId,
                        StoreName = sellerResponse.Data.StoreName,
                        UserId = sellerResponse.Data.UserId,
                    };
                    responseMessage.ErrorMessage = null;
                    
                    _logger.LogInformation("✅ MainEcommerce: Found seller - SellerId={SellerId}, StoreName={StoreName}", 
                        sellerResponse.Data.SellerId, sellerResponse.Data.StoreName);
                }
                else
                {
                    responseMessage.Data = null;
                    responseMessage.ErrorMessage = sellerResponse.Message ?? $"Seller not found for UserId: {request.UserId}";
                    
                    _logger.LogWarning("⚠️ MainEcommerce: Seller not found for UserId={UserId}: {Message}", 
                        request.UserId, sellerResponse.Message);
                }

                await SendResponseAsync(producer, responseMessage);

                _logger.LogInformation("✅ MainEcommerce: Processed request {RequestId} for user {UserId} with success={Success}", 
                    request.RequestId, request.UserId, responseMessage.Success);
            }
            else
            {
                _logger.LogWarning("⚠️ MainEcommerce: Unknown action: {Action}", request.Action);
                
                var errorResponse = new SellerResponseMessage
                {
                    RequestId = request.RequestId,
                    Success = false,
                    Data = null,
                    ErrorMessage = $"Unknown action: {request.Action}"
                };
                
                await SendResponseAsync(producer, errorResponse);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ MainEcommerce: Error processing message: {Message}", messageValue);
            throw;
        }
    }

    private async Task SendResponseAsync(IProducer<string, string> producer, SellerResponseMessage response)
    {
        try
        {
            var responseJson = JsonSerializer.Serialize(response);
            
            _logger.LogInformation("📤 MainEcommerce: Sending response: RequestId={RequestId}, Success={Success}, Json={Json}", 
                response.RequestId, response.Success, responseJson);

            var message = new Message<string, string>
            {
                Key = response.RequestId,
                Value = responseJson
            };

            var result = await producer.ProduceAsync("seller-response", message);
            
            _logger.LogInformation("✅ MainEcommerce: Response sent successfully - RequestId={RequestId}, Topic={Topic}, Partition={Partition}, Offset={Offset}", 
                response.RequestId, result.Topic, result.Partition.Value, result.Offset.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ MainEcommerce: Failed to send response for request {RequestId}", response.RequestId);
            throw;
        }
    }

    private async Task ConsumeProductUpdateResultAsync(string topic, CancellationToken stoppingToken)
    {
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = "main-ecommerce-product-update-consumer",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            SessionTimeoutMs = 6000,
            HeartbeatIntervalMs = 2000,
            EnablePartitionEof = false,
            AllowAutoCreateTopics = true
        };

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig)
            .SetErrorHandler((_, e) => 
            {
                _logger.LogError("❌ MainEcommerce ProductUpdate Consumer error: {Error}", e.Reason);
            })
            .SetLogHandler((_, log) =>
            {
                _logger.LogDebug("📋 MainEcommerce ProductUpdate Consumer log: {Level} - {Message}", log.Level, log.Message);
            })
            .Build();

        // Retry subscription logic
        var retryCount = 0;
        const int maxRetries = 10;
        
        while (retryCount < maxRetries && !stoppingToken.IsCancellationRequested)
        {
            try
            {
                consumer.Subscribe(topic);
                _logger.LogInformation("✅ MainEcommerce: Successfully subscribed to {Topic} (attempt {Attempt})", topic, retryCount + 1);
                break;
            }
            catch (Exception ex)
            {
                retryCount++;
                _logger.LogWarning(ex, "⚠️ MainEcommerce: Failed to subscribe to {Topic}. Retry {RetryCount}/{MaxRetries}", topic, retryCount, maxRetries);
                
                if (retryCount < maxRetries)
                {
                    await Task.Delay(3000, stoppingToken);
                }
            }
        }

        if (retryCount >= maxRetries)
        {
            _logger.LogError("❌ MainEcommerce: Failed to subscribe to {Topic} after {MaxRetries} attempts", topic, maxRetries);
            return;
        }

        _logger.LogInformation("🚀 MainEcommerce: Starting product update result listener for topic {Topic}...", topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = consumer.Consume(TimeSpan.FromMilliseconds(5000));
                    
                    if (consumeResult != null && consumeResult.Message != null)
                    {
                        _logger.LogInformation("📨 MainEcommerce: Received product update result - Topic={Topic}, Partition={Partition}, Offset={Offset}, Key={Key}", 
                            consumeResult.Topic, consumeResult.Partition, consumeResult.Offset, consumeResult.Message.Key);

                        _logger.LogDebug("📋 MainEcommerce: Message content: {Value}", consumeResult.Message.Value);

                        // ✅ FIX: Proper scope management
                        var scope = _scopeFactory.CreateScope();
                        try
                        {
                            await ProcessProductUpdateResultAsync(scope.ServiceProvider, consumeResult.Message.Value);
                            consumer.Commit(consumeResult);
                            _logger.LogInformation("✅ MainEcommerce: Successfully processed and committed product update result - Offset={Offset}", consumeResult.Offset);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "❌ MainEcommerce: Error processing product update result - will not commit. Offset={Offset}", consumeResult.Offset);
                        }
                        finally
                        {
                            // ✅ Safe disposal
                            await DisposeServiceScopeAsync(scope);
                        }
                    }
                    else
                    {
                        _logger.LogDebug("🔍 MainEcommerce: No new messages in topic {Topic}", topic);
                    }
                }
                catch (ConsumeException ex) when (ex.Error.Code == ErrorCode.UnknownTopicOrPart)
                {
                    _logger.LogWarning("⚠️ MainEcommerce: Topic '{Topic}' not found. Waiting...", topic);
                    await Task.Delay(5000, stoppingToken);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "❌ MainEcommerce: Error consuming product update result from {Topic} - Error Code: {ErrorCode}", topic, ex.Error.Code);
                    await Task.Delay(2000, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("🛑 MainEcommerce: Product update consumer cancellation requested");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ MainEcommerce: Unexpected error in product update consumer loop");
                    await Task.Delay(2000, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("🛑 MainEcommerce: Product update consumer service is stopping.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ MainEcommerce: Fatal error in product update consumer");
        }
        finally
        {
            try
            {
                consumer.Close();
                _logger.LogInformation("🔒 MainEcommerce: Product update consumer closed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ MainEcommerce: Error closing product update consumer");
            }
        }
    }

    // ✅ FIX: Cải thiện ProcessProductUpdateResultAsync
    private async Task ProcessProductUpdateResultAsync(IServiceProvider serviceProvider, string messageValue)
    {
        var maxRetries = 3;
        var retryCount = 0;
        
        while (retryCount < maxRetries)
        {
            try
            {
                _logger.LogInformation("🔍 MainEcommerce: Processing product update result (attempt {Attempt}): {MessageLength} characters", 
                    retryCount + 1, messageValue?.Length ?? 0);

                if (string.IsNullOrWhiteSpace(messageValue))
                {
                    _logger.LogWarning("⚠️ MainEcommerce: Received empty or null product update result");
                    return;
                }

                ProductUpdateMessage updateResult;
                try
                {
                    updateResult = JsonSerializer.Deserialize<ProductUpdateMessage>(messageValue);
                    if (updateResult == null)
                    {
                        _logger.LogWarning("⚠️ MainEcommerce: Deserialized product update result is null");
                        return;
                    }
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "❌ MainEcommerce: Failed to deserialize product update result: {Message}", messageValue);
                    throw new InvalidOperationException($"Invalid JSON format: {jsonEx.Message}", jsonEx);
                }

                _logger.LogInformation("📋 MainEcommerce: Parsed update result - RequestId={RequestId}, OrderId={OrderId}, Success={Success}, ErrorMessage={ErrorMessage}", 
                    updateResult.RequestId, updateResult.OrderId, updateResult.Success, updateResult.ErrorMessage ?? "None");

                var orderService = serviceProvider.GetRequiredService<IOrderService>();

                // ✅ Cập nhật trạng thái order
                string newStatus = updateResult.Success ? "Confirmed" : "Cancelled";
                var result = await orderService.UpdateOrderStatusByName(updateResult.OrderId, newStatus);

                // ✅ Update processing status
                // await orderService.UpdateOrderProcessingStatus(
                //     updateResult.OrderId, 
                //     updateResult.Success, 
                //     updateResult.Success ? "Inventory updated successfully" : updateResult.ErrorMessage);

                // ✅ TEMPORARY: Log thay vì call method
                _logger.LogInformation("📋 MainEcommerce: Would update processing status for order {OrderId}: Success={Success}, Message={Message}", 
                    updateResult.OrderId, updateResult.Success, updateResult.Success ? "Inventory updated successfully" : updateResult.ErrorMessage);

                if (result.Success)
                {
                    _logger.LogInformation("✅ MainEcommerce: Updated order {OrderId} status to {Status}", 
                        updateResult.OrderId, newStatus);

                    // ✅ Gửi thông báo SignalR
                    var hubContext = serviceProvider.GetRequiredService<IHubContext<NotificationHub>>();
                    
                    if (updateResult.Success)
                    {
                        // ✅ Send to specific user and all users
                        await hubContext.Clients.All.SendAsync("OrderConfirmed", updateResult.OrderId);
                        await hubContext.Clients.Group($"User_{updateResult.OrderId}")
                            .SendAsync("OrderConfirmed", updateResult.OrderId);
                        
                        _logger.LogInformation("📡 MainEcommerce: Sent OrderConfirmed SignalR notification for order {OrderId}", updateResult.OrderId);
                    }
                    else
                    {
                        await hubContext.Clients.All.SendAsync("OrderCancelled", updateResult.OrderId, updateResult.ErrorMessage);
                        await hubContext.Clients.Group($"User_{updateResult.OrderId}")
                            .SendAsync("OrderCancelled", updateResult.OrderId, updateResult.ErrorMessage);
                        
                        _logger.LogInformation("📡 MainEcommerce: Sent OrderCancelled SignalR notification for order {OrderId}", updateResult.OrderId);
                    }
                    
                    return; // Success - exit retry loop
                }
                else
                {
                    if (retryCount < maxRetries - 1)
                    {
                        retryCount++;
                        _logger.LogWarning("⚠️ MainEcommerce: Failed to update order status, retrying {RetryCount}/{MaxRetries}: {Message}", 
                            retryCount, maxRetries, result.Message);
                        await Task.Delay(1000 * retryCount);
                        continue;
                    }
                    
                    _logger.LogError("❌ MainEcommerce: Failed to update order {OrderId} status after {MaxRetries} attempts: {Message}", 
                        updateResult.OrderId, maxRetries, result.Message);
                    throw new InvalidOperationException($"Failed to update order status: {result.Message}");
                }
            }
            catch (Exception ex) when (retryCount < maxRetries - 1)
            {
                retryCount++;
                _logger.LogWarning(ex, "⚠️ MainEcommerce: Error processing product update result (attempt {Attempt}/{MaxRetries}): {Message}", 
                    retryCount, maxRetries, ex.Message);
                await Task.Delay(1000 * retryCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ MainEcommerce: Error processing product update result after {MaxRetries} attempts: {Message}", 
                    maxRetries, messageValue);
                throw;
            }
        }
    }

    // ✅ ADD: Safe disposal helper
    private async Task DisposeServiceScopeAsync(IServiceScope scope)
    {
        try
        {
            if (scope is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else
            {
                scope.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ MainEcommerce: Error disposing service scope");
        }
    }

    private async Task EnsureTopicsExistAsync()
    {
        var config = new AdminClientConfig { BootstrapServers = _bootstrapServers };
        
        using var adminClient = new AdminClientBuilder(config).Build();
        
        var requiredTopics = new[] { 
            "seller-request", 
            "seller-response",
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
                _logger.LogInformation("✅ MainEcommerce: Created topics: {Topics}", string.Join(", ", topicsToCreate));
            }
            else
            {
                _logger.LogInformation("✅ MainEcommerce: All required topics already exist: {Topics}", string.Join(", ", requiredTopics));
            }
        }
        catch (CreateTopicsException ex)
        {
            foreach (var result in ex.Results)
            {
                if (result.Error.Code != ErrorCode.TopicAlreadyExists)
                {
                    _logger.LogError("❌ MainEcommerce: Failed to create topic {Topic}: {Error}", result.Topic, result.Error.Reason);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ MainEcommerce: Error ensuring topics exist");
        }
    }
}