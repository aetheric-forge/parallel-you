using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ParallelYou.ThreadService;

public class ThreadCommandWorker(
    ILogger<ThreadCommandWorker> logger
) : IHostedService
{
    // constants
    private const string CommandsExchange = "parallel_you.commands";
    private const string EventsExchange = "parallel_you.events";
    private const string QueueName = "thread_commands";
    private const string RoutingKey = "thread.start";
    
    public ILogger<ThreadCommandWorker> Logger { get; } = logger;
    public string RabbitMqUrl { get; } = Environment.GetEnvironmentVariable("RABBITMQ_URL") 
                                         ?? throw new Exception("RABBITMQ_URL is not set");
    
    public IConnection? Connection { get; private set; }
    public IChannel? Channel { get; private set; }

    public async Task StartAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            Uri =  new Uri(RabbitMqUrl),    
            ConsumerDispatchConcurrency = 4,
        };
        
        Connection = await factory.CreateConnectionAsync(stoppingToken); 
        Channel = await Connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await Channel.ExchangeDeclareAsync(CommandsExchange, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
        await Channel.ExchangeDeclareAsync(EventsExchange, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);

        await Channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: stoppingToken);
        await Channel.QueueBindAsync(queue: QueueName, exchange: CommandsExchange, routingKey: RoutingKey, cancellationToken: stoppingToken);
        
        Logger.LogInformation("ThreadCommandWorker listening on {Queue} bound to {Exchange} with key {Key}", QueueName, CommandsExchange, RoutingKey);
        
        var consumer = new AsyncEventingBasicConsumer(Channel);
        consumer.ReceivedAsync += OnMessageAsync;
        
        await Channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);
    }

    public async Task StopAsync(CancellationToken stoppingToken)
    {
        await Channel!.CloseAsync(stoppingToken); 
        await Connection!.CloseAsync(stoppingToken);
    }

    private async Task OnMessageAsync(object sender, BasicDeliverEventArgs ea)
    {
        try
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            Logger.LogInformation("ThreadCommandWorker received message {Body}", json);

            var envelope = JsonSerializer.Deserialize<Envelope>(json);
            if (envelope is null)
            {
                Logger.LogWarning("Failed to deserialize envelope");
                await Channel!.BasicNackAsync(ea.DeliveryTag, false, false);
            }
            
            Logger.LogInformation("Command {RoutingKey} for session {SessionId}, correlation {CorrelationId}",
                envelope?.Meta.RoutingKey, envelope?.Meta.SessionId,  envelope?.Meta.CorrelationId);
            
            await PublishThreadStartedEventAsync(envelope);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error handling message");
            await Channel!.BasicNackAsync(ea.DeliveryTag, false, false);
        }
    }

    private async Task PublishThreadStartedEventAsync(Envelope cmdEnvelope)
    {
        if (Channel is null)
            throw new InvalidOperationException("Channel is not initialized");

        var evt = new
        {
            meta = cmdEnvelope.Meta,
            body = new
            {
                status = "started",
                at = DateTimeOffset.UtcNow,
                type = "thread.started"
            }
        };

        var json = JsonSerializer.Serialize(evt);
        var body = Encoding.UTF8.GetBytes(json);

        var props = new BasicProperties();
        props.ContentType = "application/json";
        props.CorrelationId = cmdEnvelope.Meta.CorrelationId;
        props.Persistent = true;

        string routingKey = $"{cmdEnvelope.Meta.ReplyChannel}.thread.started";

        await Channel.BasicPublishAsync(
                exchange: EventsExchange, 
                routingKey: routingKey, 
                mandatory: true,
                basicProperties: props, 
                body: body);
        
        Logger.LogInformation("Published event to {RoutingKey}", routingKey);
    }
}
