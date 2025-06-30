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
    private readonly string _bootstrapServers = "kafka:29092";

    public KafkaConsumerService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await EnsureTopicsExistAsync();
        
        var tasks = new[]
        {
            Task.Run(() => ConsumeSellerRequestAsync(stoppingToken), stoppingToken),
            Task.Run(() => ConsumeProductUpdateResultAsync(stoppingToken), stoppingToken),
            Task.Run(() => ConsumeOrderCancelledAsync(stoppingToken), stoppingToken)
        };
        
        await Task.WhenAll(tasks);
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

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        using var producer = new ProducerBuilder<string, string>(producerConfig).Build();
        
        consumer.Subscribe("seller-request");

        while (!stoppingToken.IsCancellationRequested)
        {
            var result = consumer.Consume(TimeSpan.FromSeconds(1));
            
            if (result?.Message != null)
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                await ProcessSellerRequest(scope.ServiceProvider, producer, result.Message.Value);
                consumer.Commit(result);
            }
            
            await Task.Delay(10, stoppingToken);
        }
        
        consumer.Unsubscribe();
        consumer.Close();
    }

    private async Task ProcessSellerRequest(IServiceProvider serviceProvider, IProducer<string, string> producer, string messageValue)
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

        using var consumer = new ConsumerBuilder<string, string>(config).Build();

        consumer.Subscribe("product-update-result");

        while (!stoppingToken.IsCancellationRequested)
        {
            var result = consumer.Consume(TimeSpan.FromSeconds(1));
            
            if (result?.Message != null)
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                await ProcessProductUpdateResult(scope.ServiceProvider, result.Message.Value);
                consumer.Commit(result);
            }
            
            await Task.Delay(10, stoppingToken);
        }
        
        consumer.Unsubscribe();
        consumer.Close();
    }

    private async Task ProcessProductUpdateResult(IServiceProvider serviceProvider, string messageValue)
    {
        if (string.IsNullOrWhiteSpace(messageValue)) return;

        var updateResult = JsonSerializer.Deserialize<ProductUpdateMessage>(messageValue);
        if (updateResult == null) return;

        var orderService = serviceProvider.GetRequiredService<IOrderService>();
        var hubContext = serviceProvider.GetRequiredService<IHubContext<NotificationHub>>();

        string newStatus = updateResult.Success ? "Confirmed" : "Cancelled";
        await Task.Delay(5000);
        await orderService.UpdateOrderStatusByName(updateResult.OrderId, newStatus);
        await hubContext.Clients.All.SendAsync("YourOrderStatusChanged", updateResult.OrderId,newStatus,$"Your order status has been updated to {newStatus}");
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

        using var consumer = new ConsumerBuilder<string, string>(config).Build();

        consumer.Subscribe("order-cancelled-result");

        while (!stoppingToken.IsCancellationRequested)
        {
            var result = consumer.Consume(TimeSpan.FromSeconds(1));
            
            if (result?.Message != null)
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                await ProcessOrderCancelledResult(scope.ServiceProvider, result.Message.Value);
                consumer.Commit(result);
            }
            
            await Task.Delay(10, stoppingToken);
        }
        
        consumer.Unsubscribe();
        consumer.Close();
    }

    private async Task ProcessOrderCancelledResult(IServiceProvider serviceProvider, string messageValue)
    {
        if (string.IsNullOrWhiteSpace(messageValue)) return;

        var updateResult = JsonSerializer.Deserialize<ProductUpdateMessage>(messageValue);
        if (updateResult == null) return;

        var orderService = serviceProvider.GetRequiredService<IOrderService>();
        var hubContext = serviceProvider.GetRequiredService<IHubContext<NotificationHub>>();

        string newStatus = updateResult.Success ? "Cancelled" : "Confirmed";
        await Task.Delay(5000);
        await orderService.UpdateOrderStatusByName(updateResult.OrderId, newStatus);
        await hubContext.Clients.All.SendAsync("YourOrderStatusChanged", updateResult.OrderId,newStatus,$"Your order status has been updated to {newStatus}");
    }

    private async Task EnsureTopicsExistAsync()
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
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
    }
}