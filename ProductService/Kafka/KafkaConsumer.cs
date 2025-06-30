using Confluent.Kafka;
using Confluent.Kafka.Admin;
using MainEcommerceService.Models.dbMainEcommer;
using MainEcommerceService.Models.Kafka;
using System.Text.Json;

public class KafkaConsumerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly string _bootstrapServers = "kafka:29092";

    public KafkaConsumerService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
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
            .Build();

        try
        {
            consumer.Subscribe(topic);

            while (!stoppingToken.IsCancellationRequested)
            {

                    // 🔥 SỬA: Sử dụng timeout ngắn hơn và check cancellation token
                    var result = consumer.Consume(TimeSpan.FromSeconds(1));
                    
                    if (result?.Message != null)
                    {

                        await using var scope = _scopeFactory.CreateAsyncScope();
                        await processor(scope.ServiceProvider, result.Message.Value);
                        
                        // 🔥 SỬA: Commit sau khi xử lý thành công
                        consumer.Commit(result);
                    }
                    
                    // 🔥 THÊM: Yield control để tránh blocking
                    await Task.Delay(10, stoppingToken);
            }
        }
        catch (Exception ex)
        {
        }
        finally
        {
                consumer.Unsubscribe();
                consumer.Close();

        }
    }

    private async Task ProcessSellerMessage(IServiceProvider serviceProvider, string messageValue)
    {
   
            var message = JsonSerializer.Deserialize<SellerProfileVM>(messageValue);
            
            if (message?.IsDeleted == true)
            {
                var productService = serviceProvider.GetRequiredService<IProdService>();
                await productService.DeleteProductsBySellerId(message.SellerId);
            }

    }

    private async Task ProcessOrderMessage(IServiceProvider serviceProvider, string messageValue)
    {

            var productService = serviceProvider.GetRequiredService<IProdService>();
            var orderMessage = JsonSerializer.Deserialize<OrderCreatedMessage>(messageValue);
            
            if (orderMessage == null) 
            {
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

    }

    private async Task ProcessOrderCancelledMessage(IServiceProvider serviceProvider, string messageValue)
    {

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
            }

    }

    private async Task SendResultAsync(string orderKey, object result)
    {
        var config = new ProducerConfig { BootstrapServers = _bootstrapServers };
        using var producer = new ProducerBuilder<string, string>(config).Build();

            var message = new Message<string, string>
            {
                Key = orderKey,
                Value = JsonSerializer.Serialize(result)
            };

            await producer.ProduceAsync("product-update-result", message);

    }

    private async Task SendOrderCancelledResultAsync(string orderKey, ProductUpdateMessage result)
    {
        var config = new ProducerConfig { BootstrapServers = _bootstrapServers };
        using var producer = new ProducerBuilder<string, string>(config).Build();

            var message = new Message<string, string>
            {
                Key = orderKey,
                Value = JsonSerializer.Serialize(result)
            };

            await producer.ProduceAsync("order-cancelled-result", message);

    }

    private async Task EnsureTopicsExistAsync()
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
            }


    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
    }
}