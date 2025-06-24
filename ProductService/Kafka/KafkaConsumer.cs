using Confluent.Kafka;
using Confluent.Kafka.Admin;
using MainEcommerceService.Models.dbMainEcommer;
using MainEcommerceService.Models.Kafka;
using System.Text.Json;

public class KafkaConsumerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KafkaConsumerService> _logger;
    private readonly string _bootstrapServers = "kafka:29092";

    public KafkaConsumerService(IServiceScopeFactory scopeFactory, ILogger<KafkaConsumerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 ProductService KafkaConsumer starting...");
        
        try
        {
            await EnsureTopicsExistAsync();
            
            // 🔥 SỬA: Chạy các consumer trong separate tasks
            var tasks = new[]
            {
                Task.Run(() => ConsumeAsync("seller-events", ProcessSellerMessage, stoppingToken), stoppingToken),
                Task.Run(() => ConsumeAsync("order-created", ProcessOrderMessage, stoppingToken), stoppingToken),
                Task.Run(() => ConsumeAsync("order-cancelled", ProcessOrderCancelledMessage, stoppingToken), stoppingToken)
            };
            
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("🛑 ProductService KafkaConsumer stopped due to cancellation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ ProductService KafkaConsumer failed");
            throw;
        }
    }

    private async Task ConsumeAsync(string topic, Func<IServiceProvider, string, Task> processor, CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            GroupId = $"product-service-{topic}-consumer",
            BootstrapServers = _bootstrapServers,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            // 🔥 THÊM: Timeout configurations
            SessionTimeoutMs = 30000,
            HeartbeatIntervalMs = 10000,
            MaxPollIntervalMs = 300000
        };

        using var consumer = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, e) => _logger.LogError("❌ Kafka consumer error for topic {Topic}: {Error}", topic, e.Reason))
            .Build();

        try
        {
            consumer.Subscribe(topic);
            _logger.LogInformation("✅ Subscribed to topic: {Topic}", topic);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // 🔥 SỬA: Sử dụng timeout ngắn hơn và check cancellation token
                    var result = consumer.Consume(TimeSpan.FromSeconds(1));
                    
                    if (result?.Message != null)
                    {
                        _logger.LogDebug("📨 Received message from topic {Topic}: {Key}", topic, result.Message.Key);

                        await using var scope = _scopeFactory.CreateAsyncScope();
                        await processor(scope.ServiceProvider, result.Message.Value);
                        
                        // 🔥 SỬA: Commit sau khi xử lý thành công
                        consumer.Commit(result);
                        _logger.LogDebug("✅ Processed and committed message from topic {Topic}", topic);
                    }
                    
                    // 🔥 THÊM: Yield control để tránh blocking
                    await Task.Delay(10, stoppingToken);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "❌ Error consuming from topic {Topic}", topic);
                    await Task.Delay(1000, stoppingToken); // Wait before retry
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("🛑 Consumer for topic {Topic} cancelled", topic);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Unexpected error in consumer for topic {Topic}", topic);
                    await Task.Delay(5000, stoppingToken); // Wait before retry
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Fatal error in consumer for topic {Topic}", topic);
        }
        finally
        {
            try
            {
                consumer.Unsubscribe();
                consumer.Close();
                _logger.LogInformation("🔒 Consumer for topic {Topic} closed", topic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error closing consumer for topic {Topic}", topic);
            }
        }
    }

    private async Task ProcessSellerMessage(IServiceProvider serviceProvider, string messageValue)
    {
        try
        {
            _logger.LogDebug("🔄 Processing seller message: {Message}", messageValue);
            
            var message = JsonSerializer.Deserialize<SellerProfileVM>(messageValue);
            
            if (message?.IsDeleted == true)
            {
                var productService = serviceProvider.GetRequiredService<IProdService>();
                await productService.DeleteProductsBySellerId(message.SellerId);
                _logger.LogInformation("✅ Deleted products for seller {SellerId}", message.SellerId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing seller message: {Message}", messageValue);
            throw; // Re-throw để consumer có thể handle
        }
    }

    private async Task ProcessOrderMessage(IServiceProvider serviceProvider, string messageValue)
    {
        try
        {
            _logger.LogDebug("🔄 Processing order message: {Message}", messageValue);
            
            var productService = serviceProvider.GetRequiredService<IProdService>();
            var orderMessage = JsonSerializer.Deserialize<OrderCreatedMessage>(messageValue);
            
            if (orderMessage == null) 
            {
                _logger.LogWarning("⚠️ Received null order message");
                return;
            }

            var result = await productService.ProcessOrderItems(orderMessage);
            var updateMessage = result?.Data ?? new ProductUpdateMessage
            {
                RequestId = orderMessage.RequestId ?? Guid.NewGuid().ToString(),
                OrderId = orderMessage.OrderId,
                Success = false,
                ErrorMessage = "Processing failed",
                UpdatedProducts = new List<ProductUpdateResult>()
            };

            await SendResultAsync(orderMessage.OrderId.ToString(), updateMessage);
            _logger.LogInformation("✅ Processed order {OrderId}, Success: {Success}", orderMessage.OrderId, updateMessage.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing order message: {Message}", messageValue);
            throw;
        }
    }

    private async Task ProcessOrderCancelledMessage(IServiceProvider serviceProvider, string messageValue)
    {
        try
        {
            _logger.LogDebug("🔄 Processing order cancelled message: {Message}", messageValue);
            
            var productService = serviceProvider.GetRequiredService<IProdService>();
            var orderMessage = JsonSerializer.Deserialize<OrderCreatedMessage>(messageValue);

            if (orderMessage != null)
            {
                var restoreResult = await productService.RestoreProductStockAsync(orderMessage);
                
                var restoreMessage = new ProductUpdateMessage
                {
                    RequestId = orderMessage.RequestId ?? Guid.NewGuid().ToString(),
                    OrderId = orderMessage.OrderId,
                    Success = restoreResult?.Success ?? false,
                    ErrorMessage = restoreResult?.Success == true ? null : "Stock restore failed",
                };

                await SendOrderCancelledResultAsync(orderMessage.OrderId.ToString(), restoreMessage);
                _logger.LogInformation("✅ Processed order cancellation {OrderId}, Success: {Success}", orderMessage.OrderId, restoreMessage.Success);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing order cancelled message: {Message}", messageValue);
            throw;
        }
    }

    private async Task SendResultAsync(string orderKey, object result)
    {
        var config = new ProducerConfig { BootstrapServers = _bootstrapServers };
        using var producer = new ProducerBuilder<string, string>(config).Build();

        try
        {
            var message = new Message<string, string>
            {
                Key = orderKey,
                Value = JsonSerializer.Serialize(result)
            };

            await producer.ProduceAsync("product-update-result", message);
            _logger.LogDebug("📤 Sent result for order {OrderKey} to product-update-result", orderKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error sending result for order {OrderKey}", orderKey);
            throw;
        }
    }

    private async Task SendOrderCancelledResultAsync(string orderKey, ProductUpdateMessage result)
    {
        var config = new ProducerConfig { BootstrapServers = _bootstrapServers };
        using var producer = new ProducerBuilder<string, string>(config).Build();

        try
        {
            var message = new Message<string, string>
            {
                Key = orderKey,
                Value = JsonSerializer.Serialize(result)
            };

            await producer.ProduceAsync("order-cancelled-result", message);
            _logger.LogDebug("📤 Sent cancelled result for order {OrderKey} to order-cancelled-result", orderKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error sending cancelled result for order {OrderKey}", orderKey);
            throw;
        }
    }

    private async Task EnsureTopicsExistAsync()
    {
        try
        {
            var config = new AdminClientConfig { BootstrapServers = _bootstrapServers };
            using var adminClient = new AdminClientBuilder(config).Build();

            var topics = new[] { "seller-events", "order-created", "product-update-result", "order-cancelled", "order-cancelled-result" };
            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
            var existing = metadata.Topics.Select(t => t.Topic).ToHashSet();
            var toCreate = topics.Where(t => !existing.Contains(t));

            if (toCreate.Any())
            {
                var specs = toCreate.Select(t => new TopicSpecification { Name = t, NumPartitions = 1, ReplicationFactor = 1 });
                await adminClient.CreateTopicsAsync(specs);
                _logger.LogInformation("✅ Created Kafka topics: {Topics}", string.Join(", ", toCreate));
            }
            else
            {
                _logger.LogInformation("✅ All required Kafka topics already exist");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error ensuring Kafka topics exist");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🛑 ProductService KafkaConsumer stopping...");
        await base.StopAsync(cancellationToken);
        _logger.LogInformation("✅ ProductService KafkaConsumer stopped");
    }
}