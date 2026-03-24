using Microservices.Shared.Domain;
using Microservices.Shared.Domain.MarketData;
using Microservices.Shared.Domain.Persistence;
using Microservices.Shared.Infrastructure.Kafka;
using Microservices.Shared.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Microservices.Shared.Infrastructure;

public static class ServiceExtensions
{
    public static IServiceCollection AddDbReader<T, P>(this IServiceCollection services)
        where T : unmanaged
        where P : class, ICopyable<T>, new()
    {
        return services
            .AddSingleton<DbReader<T, P>>()
            //.AddSingleton<IAsyncPublisher<T[]>>(sp => sp.GetRequiredService<DbReader<T, P>>())
            .AddHostedService(sp => sp.GetRequiredService<DbReader<T, P>>());
    }
    
    public static IServiceCollection AddDbWriter<T, P>(this IServiceCollection services)
        where T : unmanaged
        where P : class, ICopyable<T>, new()
    {
        return services
            .AddSingleton<DbWriter<T, P>>()
            .AddSingleton<IAsyncPublisher<T[]>>(sp => sp.GetRequiredService<DbWriter<T, P>>())
            .AddHostedService(sp => sp.GetRequiredService<DbWriter<T, P>>());
    }

    public static IServiceCollection AddDistributor<T>(this IServiceCollection services)
        where T : unmanaged
    {
        return services
            .AddSingleton<Distributor<T>>()
            .AddSingleton<IAsyncDistributor<T>>(sp => sp.GetRequiredService<Distributor<T>>())
            .AddHostedService(sp => sp.GetRequiredService<Distributor<T>>());
    }

    public static IServiceCollection AddKafkaWriter<T>(this IServiceCollection services)
        where T : unmanaged
    {
        return services
            .AddSingleton<StructsWriter<T>>()
            .AddSingleton<IAsyncPublisher<T[]>>(sp => sp.GetRequiredService<StructsWriter<T>>())
            .AddHostedService(sp => sp.GetRequiredService<StructsWriter<T>>());
    }
    
    public static IServiceCollection AddTradeSender(this IServiceCollection services)
    {
        // Distributor => DbWriter => Postgres
        return services
            .AddDistributor<TradeInfo>()
            .AddDbWriter<TradeInfo, TradeEntity>();
    }

    public static IServiceCollection AddLevelsSender(this IServiceCollection services)
    {
        // Distributor => StructsWriter => Kafka
        return services
            .AddDistributor<Levels10Info>()
            .AddKafkaWriter<Levels10Info>();
    }
    
    public static IServiceCollection AddKlineSender(this IServiceCollection services)
    {
        // Distributor => gRPC
        return services
            .AddDistributor<KlineInfo>()
            //.AddKafkaWriter<KlineInfo>()
            ;
    }
}
