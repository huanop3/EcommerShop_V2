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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 MainEcommerce KafkaConsumer starting...");
        
        try
        {
            await EnsureTopicsExistAsync();
            
            // 🔥 SỬA: Chạy các consumer trong separate tasks
            var tasks = new[]
            {
                Task.Run(() => ConsumeSellerRequestAsync(stoppingToken), stoppingToken),
                Task.Run(() => ConsumeProductUpdateResultAsync(stoppingToken), stoppingToken),
                Task.Run(() => ConsumeOrderCancelledAsync(stoppingToken), stoppingToken)
            };
            
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("🛑 MainEcommerce KafkaConsumer stopped due to cancellation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ MainEcommerce KafkaConsumer failed");
            throw;
        }
    }

    private async Task ConsumeSellerRequestAsync(CancellationToken stoppingToken)
    {
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = "main-ecommerce-seller-request",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            SessionTimeoutMs = 30000,
            HeartbeatIntervalMs = 10000,
            MaxPollIntervalMs = 300000
        };

        var producerConfig = new ProducerConfig { BootstrapServers = _bootstrapServers };

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig)
            .SetErrorHandler((_, e) => _logger.LogError("❌ Seller request consumer error: {Error}", e.Reason))
            .Build();
        using var producer = new ProducerBuilder<string, string>(producerConfig).Build();
        
        try
        {
            consumer.Subscribe("seller-request");
            _logger.LogInformation("✅ Subscribed to seller-request topic");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // 🔥 SỬA: Timeout ngắn hơn
                    var result = consumer.Consume(TimeSpan.FromSeconds(1));
                    
                    if (result?.Message != null)
                    {
                        _logger.LogDebug("📨 Received seller request: {Key}", result.Message.Key);
                        
                       await using var scope = _scopeFactory.CreateAsyncScope();
                        await ProcessSellerRequest(scope.ServiceProvider, producer, result.Message.Value);
                        consumer.Commit(result);
                        
                        _logger.LogDebug("✅ Processed seller request: {Key}", result.Message.Key);
                    }
                    
                    // 🔥 THÊM: Yield control
                    await Task.Delay(10, stoppingToken);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "❌ Error consuming seller request");
                    await Task.Delay(1000, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("🛑 Seller request consumer cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Unexpected error in seller request consumer");
                    await Task.Delay(5000, stoppingToken);
                }
            }
        }
        finally
        {
            try
            {
                consumer.Unsubscribe();
                consumer.Close();
                _logger.LogInformation("🔒 Seller request consumer closed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error closing seller request consumer");
            }
        }
    }

    private async Task ProcessSellerRequest(IServiceProvider serviceProvider, IProducer<string, string> producer, string messageValue)
    {
        try
        {
            var request = JsonSerializer.Deserialize<SellerRequestMessage>(messageValue);
            if (request?.Action != "GET_SELLER_BY_USER_ID") return;

            var sellerService = serviceProvider.GetRequiredService<ISellerProfileService>();
            var sellerResponse = await sellerService.GetSellerProfileByUserId(request.UserId);

            var response = new SellerResponseMessage
            {
                RequestId = request.RequestId,
                Success = sellerResponse.Success,
                Data = sellerResponse.Success ? new SellerProfileVM
                {
                    SellerId = sellerResponse.Data.SellerId,
                    StoreName = sellerResponse.Data.StoreName,
                    UserId = sellerResponse.Data.UserId
                } : null,
                ErrorMessage = sellerResponse.Success ? null : sellerResponse.Message
            };

            await producer.ProduceAsync("seller-response", new Message<string, string>
            {
                Key = response.RequestId,
                Value = JsonSerializer.Serialize(response)
            });
            
            _logger.LogInformation("✅ Processed seller request for user {UserId}", request.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing seller request: {Message}", messageValue);
            throw;
        }
    }

    private async Task ConsumeProductUpdateResultAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = "main-ecommerce-product-update-consumer",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            SessionTimeoutMs = 30000,
            HeartbeatIntervalMs = 10000,
            MaxPollIntervalMs = 300000
        };

        using var consumer = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, e) => _logger.LogError("❌ Product update consumer error: {Error}", e.Reason))
            .Build();

        try
        {
            consumer.Subscribe("product-update-result");
            _logger.LogInformation("✅ Subscribed to product-update-result topic");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // 🔥 SỬA: Timeout ngắn hơn
                    var result = consumer.Consume(TimeSpan.FromSeconds(1));
                    
                    if (result?.Message != null)
                    {
                        _logger.LogDebug("📨 Received product update result: {Key}", result.Message.Key);

                        await using var scope = _scopeFactory.CreateAsyncScope();
                        await ProcessProductUpdateResult(scope.ServiceProvider, result.Message.Value);
                        consumer.Commit(result);
                        
                        _logger.LogDebug("✅ Processed product update result: {Key}", result.Message.Key);
                    }
                    
                    // 🔥 THÊM: Yield control
                    await Task.Delay(10, stoppingToken);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "❌ Error consuming product update result");
                    await Task.Delay(1000, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("🛑 Product update consumer cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Unexpected error in product update consumer");
                    await Task.Delay(5000, stoppingToken);
                }
            }
        }
        finally
        {
            try
            {
                consumer.Unsubscribe();
                consumer.Close();
                _logger.LogInformation("🔒 Product update consumer closed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error closing product update consumer");
            }
        }
    }

    private async Task ProcessProductUpdateResult(IServiceProvider serviceProvider, string messageValue)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(messageValue)) return;

            var updateResult = JsonSerializer.Deserialize<ProductUpdateMessage>(messageValue);
            if (updateResult == null) return;

            var orderService = serviceProvider.GetRequiredService<IOrderService>();
            var hubContext = serviceProvider.GetRequiredService<IHubContext<NotificationHub>>();

            string newStatus = updateResult.Success ? "Confirmed" : "Cancelled";
            await Task.Delay(5000); // Simulate some processing delay
            await orderService.UpdateOrderStatusByName(updateResult.OrderId, newStatus);
            await hubContext.Clients.All.SendAsync("YourOrderStatusChanged", updateResult.OrderId,newStatus,$"Your order status has been updated to {newStatus}");

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing product update result: {Message}", messageValue);
            throw;
        }
    }

    private async Task ConsumeOrderCancelledAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = "main-ecommerce-order-cancelled-consumer",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            SessionTimeoutMs = 30000,
            HeartbeatIntervalMs = 10000,
            MaxPollIntervalMs = 300000
        };

        using var consumer = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, e) => _logger.LogError("❌ Order cancelled consumer error: {Error}", e.Reason))
            .Build();

        try
        {
            consumer.Subscribe("order-cancelled-result");
            _logger.LogInformation("✅ Subscribed to order-cancelled-result topic");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(TimeSpan.FromSeconds(1));
                    
                    if (result?.Message != null)
                    {
                        _logger.LogDebug("📨 Received order cancelled result: {Key}", result.Message.Key);

                        await using var scope = _scopeFactory.CreateAsyncScope();
                        await ProcessOrderCancelledResult(scope.ServiceProvider, result.Message.Value);
                        consumer.Commit(result);
                        
                        _logger.LogDebug("✅ Processed order cancelled result: {Key}", result.Message.Key);
                    }
                    
                    await Task.Delay(10, stoppingToken);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "❌ Error consuming order cancelled result");
                    await Task.Delay(1000, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("🛑 Order cancelled consumer cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Unexpected error in order cancelled consumer");
                    await Task.Delay(5000, stoppingToken);
                }
            }
        }
        finally
        {
            try
            {
                consumer.Unsubscribe();
                consumer.Close();
                _logger.LogInformation("🔒 Order cancelled consumer closed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error closing order cancelled consumer");
            }
        }
    }

    private async Task ProcessOrderCancelledResult(IServiceProvider serviceProvider, string messageValue)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(messageValue)) return;

            var updateResult = JsonSerializer.Deserialize<ProductUpdateMessage>(messageValue);
            if (updateResult == null) return;

            var orderService = serviceProvider.GetRequiredService<IOrderService>();
            var hubContext = serviceProvider.GetRequiredService<IHubContext<NotificationHub>>();

            string newStatus = updateResult.Success ? "Cancelled" : "Confirmed";
            await Task.Delay(5000); // Simulate some processing delay
            await orderService.UpdateOrderStatusByName(updateResult.OrderId, newStatus);
            await hubContext.Clients.All.SendAsync("YourOrderStatusChanged", updateResult.OrderId,newStatus,$"Your order status has been updated to {newStatus}");

            _logger.LogInformation("✅ Updated order {OrderId} status to {Status} after cancellation result", updateResult.OrderId, newStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing order cancelled result: {Message}", messageValue);
            throw;
        }
    }

    private async Task EnsureTopicsExistAsync()
    {
        try
        {
            var config = new AdminClientConfig { BootstrapServers = _bootstrapServers };
            using var adminClient = new AdminClientBuilder(config).Build();
            
            var topics = new[] { "seller-request", "seller-response", "order-created-result", "product-update-result", "order-cancelled-result" };
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
        _logger.LogInformation("🛑 MainEcommerce KafkaConsumer stopping...");
        await base.StopAsync(cancellationToken);
        _logger.LogInformation("✅ MainEcommerce KafkaConsumer stopped");
    }
}