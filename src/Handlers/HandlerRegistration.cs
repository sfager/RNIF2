using Microsoft.Extensions.DependencyInjection;
using RNIF2.Core.Interfaces;

namespace RNIF2.Handlers;

public static class HandlerRegistration
{
    public static IServiceCollection AddRnifHandler<THandler>(this IServiceCollection services)
        where THandler : class, IMessageHandler
    {
        services.AddSingleton<IMessageHandler, THandler>();
        return services;
    }

    public static IServiceCollection AddDefaultRnifHandlers(this IServiceCollection services)
    {
        services.AddSingleton<IMessageHandler, DefaultReceiptHandler>();
        return services;
    }
}
