using System.Text.Json;
using System.Text.Json.Nodes;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AvailabilityService.Api.Configuration;
using AvailabilityService.Api.Events.Tenant;
using AvailabilityService.Api.Events.Booking;
using AvailabilityService.Api.Services.Interfaces;

namespace AvailabilityService.Api.Services;

public class KafkaConsumerService(
    ILogger<KafkaConsumerService> logger,
    IServiceProvider serviceProvider,
    IOptions<KafkaSettings> kafkaSettings)
    : BackgroundService
{
    private readonly KafkaSettings _kafkaSettings = kafkaSettings.Value;

    private ConsumerConfig CreateTenantConsumerConfig()
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _kafkaSettings.BootstrapServers,
            GroupId = $"{_kafkaSettings.ConsumerGroupId}-tenant-events",
            AutoOffsetReset = KafkaConstants.ParseAutoOffsetReset(_kafkaSettings.AutoOffsetReset),
            EnableAutoCommit = _kafkaSettings.EnableAutoCommit
        };

        if (string.IsNullOrEmpty(_kafkaSettings.SaslPassword)) return config;
        config.SecurityProtocol = ParseSecurityProtocol(_kafkaSettings.SecurityProtocol);
        config.SaslMechanism = ParseSaslMechanism(_kafkaSettings.SaslMechanism);
        config.SaslUsername = _kafkaSettings.SaslUsername;
        config.SaslPassword = _kafkaSettings.SaslPassword;

        return config;
    }

    private ConsumerConfig CreateBookingConsumerConfig()
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _kafkaSettings.BootstrapServers,
            GroupId = $"{_kafkaSettings.ConsumerGroupId}-booking-events",
            AutoOffsetReset = KafkaConstants.ParseAutoOffsetReset(_kafkaSettings.AutoOffsetReset),
            EnableAutoCommit = _kafkaSettings.EnableAutoCommit
        };

        if (string.IsNullOrEmpty(_kafkaSettings.SaslPassword)) return config;
        config.SecurityProtocol = ParseSecurityProtocol(_kafkaSettings.SecurityProtocol);
        config.SaslMechanism = ParseSaslMechanism(_kafkaSettings.SaslMechanism);
        config.SaslUsername = _kafkaSettings.SaslUsername;
        config.SaslPassword = _kafkaSettings.SaslPassword;

        return config;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Kafka Consumer Service starting...");

        _ = Task.Run(async () =>
        {
            try
            {
                await ConsumeTenantEventsAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Tenant events consumer failed unexpectedly.");
            }
        }, cancellationToken);

        _ = Task.Run(async () =>
        {
            try
            {
                await ConsumeBookingEventsAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Booking events consumer failed unexpectedly.");
            }
        }, cancellationToken);

        await Task.Delay(Timeout.Infinite, cancellationToken);
    }

    private async Task ConsumeTenantEventsAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting tenant events consumer for topic: {Topic}", _kafkaSettings.TenantEventsTopic);

        var consumerConfig = CreateTenantConsumerConfig();

        IConsumer<Ignore, string>? consumer = null;
        var retryCount = 0;
        var retryDelayMs = 2000;

        while (consumer == null && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();
                consumer.Subscribe(_kafkaSettings.TenantEventsTopic);
                logger.LogInformation("Successfully connected to Kafka and subscribed to topic: {Topic}", _kafkaSettings.TenantEventsTopic);
                break;
            }
            catch (Exception ex)
            {
                retryCount++;
                logger.LogWarning(ex, "Failed to initialize Kafka consumer (attempt {RetryCount}). Retrying in {Delay}ms...", retryCount, retryDelayMs);
                consumer?.Dispose();
                consumer = null;
                if (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(retryDelayMs, cancellationToken);
                }
            }
        }

        if (consumer == null)
        {
            logger.LogError("Failed to initialize Kafka consumer after cancellation. Stopping consumer.");
            return;
        }

        try
        {
            using (consumer)
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var consumeResult = consumer.Consume(cancellationToken);
                    if (consumeResult.Message is null) continue;

                    logger.LogInformation("Received tenant event: {EventType}", consumeResult.Message.Key);

                    try
                    {
                        var jsonNode = JsonNode.Parse(consumeResult.Message.Value);
                        var eventType = jsonNode?["eventType"]?.GetValue<string>();
                        if (string.IsNullOrEmpty(eventType))
                        {
                            logger.LogWarning("Event missing eventType field");
                            continue;
                        }

                        await ProcessTenantEventAsync(eventType, consumeResult.Message.Value);
                        consumer.Commit(consumeResult);
                    }
                    catch (JsonException jsonEx)
                    {
                        logger.LogError(jsonEx, "Failed to parse tenant event JSON: {Message}", consumeResult.Message.Value);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing tenant event: {EventType}", consumeResult.Message.Key);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Tenant events consumer stopped due to cancellation");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Tenant events consumer error");
        }
    }

    private async Task ConsumeBookingEventsAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting booking events consumer for topic: {Topic}", _kafkaSettings.BookingEventsTopic);

        var consumerConfig = CreateBookingConsumerConfig();

        IConsumer<Ignore, string>? consumer = null;
        var retryCount = 0;
        var retryDelayMs = 2000;

        while (consumer == null && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();
                consumer.Subscribe(_kafkaSettings.BookingEventsTopic);
                logger.LogInformation("Successfully connected to Kafka and subscribed to topic: {Topic}", _kafkaSettings.BookingEventsTopic);
                break;
            }
            catch (Exception ex)
            {
                retryCount++;
                logger.LogWarning(ex, "Failed to initialize Kafka consumer (attempt {RetryCount}). Retrying in {Delay}ms...", retryCount, retryDelayMs);
                consumer?.Dispose();
                consumer = null;
                if (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(retryDelayMs, cancellationToken);
                }
            }
        }

        if (consumer == null)
        {
            logger.LogError("Failed to initialize Kafka consumer after cancellation. Stopping consumer.");
            return;
        }

        try
        {
            using (consumer)
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var consumeResult = consumer.Consume(cancellationToken);
                    if (consumeResult.Message is null) continue;

                    logger.LogInformation("Received booking event: {EventType}", consumeResult.Message.Key);

                    try
                    {
                        var jsonNode = JsonNode.Parse(consumeResult.Message.Value);
                        var eventType = jsonNode?["eventType"]?.GetValue<string>();
                        if (string.IsNullOrEmpty(eventType))
                        {
                            logger.LogWarning("Event missing eventType field");
                            continue;
                        }

                        await ProcessBookingEventAsync(eventType, consumeResult.Message.Value);
                        consumer.Commit(consumeResult);
                    }
                    catch (JsonException jsonEx)
                    {
                        logger.LogError(jsonEx, "Failed to parse booking event JSON: {Message}", consumeResult.Message.Value);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing booking event: {EventType}", consumeResult.Message.Key);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Booking events consumer stopped due to cancellation");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Booking events consumer error");
        }
    }

    private async Task ProcessTenantEventAsync(string eventType, string eventJson)
    {
        using var scope = serviceProvider.CreateScope();
        var tenantEventService = scope.ServiceProvider.GetRequiredService<ITenantEventService>();

        try
        {
            switch (eventType)
            {
                case nameof(TenantCreatedEvent):
                    var tenantCreatedEvent = JsonSerializer.Deserialize<TenantCreatedEvent>(eventJson, KafkaConstants.JsonSerializerOptions);
                    if (tenantCreatedEvent != null)
                    {
                        logger.LogInformation("Processing TenantCreatedEvent for tenant {TenantId}", tenantCreatedEvent.TenantId);
                        await tenantEventService.HandleTenantCreatedEventAsync(tenantCreatedEvent);
                    }
                    break;

                case nameof(TenantUpdatedEvent):
                    var tenantUpdatedEvent = JsonSerializer.Deserialize<TenantUpdatedEvent>(eventJson, KafkaConstants.JsonSerializerOptions);
                    if (tenantUpdatedEvent != null)
                    {
                        logger.LogInformation("Processing TenantUpdatedEvent for tenant {TenantId}", tenantUpdatedEvent.TenantId);
                        await tenantEventService.HandleTenantUpdatedEventAsync(tenantUpdatedEvent);
                    }
                    break;

                default:
                    logger.LogWarning("Unknown tenant event type: {EventType}", eventType);
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing tenant event {EventType}", eventType);
            throw;
        }
    }

    private async Task ProcessBookingEventAsync(string eventType, string eventJson)
    {
        using var scope = serviceProvider.CreateScope();
        var bookingEventService = scope.ServiceProvider.GetRequiredService<IBookingEventService>();

        try
        {
            switch (eventType)
            {
                case nameof(BookingCreatedEvent):
                    var bookingCreatedEvent = JsonSerializer.Deserialize<BookingCreatedEvent>(eventJson, KafkaConstants.JsonSerializerOptions);
                    if (bookingCreatedEvent != null)
                    {
                        logger.LogInformation("Processing BookingCreatedEvent for booking {BookingId}", bookingCreatedEvent.BookingId);
                        await bookingEventService.HandleBookingCreatedEventAsync(bookingCreatedEvent);
                    }
                    break;

                case nameof(BookingCancelledEvent):
                    var bookingCancelledEvent = JsonSerializer.Deserialize<BookingCancelledEvent>(eventJson, KafkaConstants.JsonSerializerOptions);
                    if (bookingCancelledEvent != null)
                    {
                        logger.LogInformation("Processing BookingCancelledEvent for booking {BookingId}", bookingCancelledEvent.BookingId);
                        await bookingEventService.HandleBookingCancelledEventAsync(bookingCancelledEvent);
                    }
                    break;

                default:
                    logger.LogWarning("Unknown booking event type: {EventType}", eventType);
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing booking event {EventType}", eventType);
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Kafka Consumer Service stopping...");
        await base.StopAsync(cancellationToken);
    }

    private static SecurityProtocol ParseSecurityProtocol(string protocol)
        => Enum.TryParse<SecurityProtocol>(protocol, out var parsed)
            ? parsed
            : SecurityProtocol.SaslSsl;

    private static SaslMechanism ParseSaslMechanism(string mechanism)
        => Enum.TryParse<SaslMechanism>(mechanism, out var parsed)
            ? parsed
            : SaslMechanism.Plain;
}
