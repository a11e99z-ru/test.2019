using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Microservices.Shared.Domain.Configs;

/*
 * Listens Bybit' WebSocket market-data and sends:
 *  - Trades to Postgres
 *  - Klines to Kafka
 *  - Levels10 to gRPC
 */

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .Configure<AppConfig>(builder.Configuration);

//TODO Serilog+Loki
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    //logging.SetMinimumLevel(LogLevel.Information);
});

/*var cfgConsul = builder.Configuration.GetSection("Consul").Get<ConsulOptions>()!;
builder.Services
    .AddConsulInfrastructure(cfgConsul);*/

/*var cfgDb = builder.Configuration
    .GetSection("DB").Get<AppConfig.DbConfig>()!;
builder.Services
    .AddDbWriterFor<TradeEventEntity, TradeEntity>(cfgDb.Trades.With(cfgDb));

var cfgKafka = builder.Configuration.GetSection("Kafka").Get<AppConfig.KafkaConfig>()!;
builder.Services
    .AddKafkaServicesFor<TradeEventEntity>(cfgKafka.Trades.With(cfgKafka))
    .AddKafkaServicesFor<PositionEventEntity>(cfgKafka.Positions.With(cfgKafka))
    .AddKafkaServicesFor<OrderEntry>(cfgKafka.Orders.With(cfgKafka))
    .AddKafkaServicesFor<SLTPOrderEntry>(cfgKafka.SltpOrders.With(cfgKafka));

var host = builder.Build();
//logger.LogInformation("Microservices.Gateway starting...");
await host.RunAsync();*/
Console.WriteLine("FIN.");
