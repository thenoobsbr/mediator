using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using TheNoobs.Mediator.Abstractions;

namespace TheNoobs.Mediator.DependencyInjection;

public static class DependencyInjectionExtensions
{
   public static IServiceCollection AddMediator(this IServiceCollection services, params Assembly[] assemblies)
    {
        return services
            .AddHandlers(assemblies)
            .AddPipelines(assemblies)
            .AddEventHandlers(assemblies)
            .AddScoped<IMediator, Mediator>();
    }

    private static IServiceCollection AddEventHandlers(
        this IServiceCollection services,
        params Assembly[] assemblies
    )
    {
        var eventHandlers = assemblies
            .SelectMany(x => x.GetTypes())
            .Where(x =>
                    x.GetInterface(typeof(IEventHandler<>).Name) != null
                    && x is { IsClass: true, IsAbstract: false }
            )
            .ToList();
        foreach (var eventHandler in eventHandlers)
        {
            var interfaceTypes = eventHandler
                .GetInterfaces()
                .Where(x => x.Name == typeof(IEventHandler<>).Name)
                .ToList();
            foreach (var interfaceType in interfaceTypes)
            {
                services.AddScoped(interfaceType, eventHandler);
            }
        }

        return services;
    }

    private static IServiceCollection AddPipelines(
        this IServiceCollection services,
        params Assembly[] assemblies
    )
    {
        var pipelineTypes = assemblies
            .SelectMany(x => x.GetTypes())
            .Where(x =>
                x.GetInterface(typeof(IHandlerPipeline<,>).Name) != null
                && x is { IsClass: true, IsAbstract: false }
            )
            .ToList();
        foreach (var pipelineType in pipelineTypes)
        {
            services.AddScoped(typeof(IHandlerPipeline<,>), pipelineType);
        }

        return services;
    }

    private static IServiceCollection AddHandlers(
        this IServiceCollection services,
        params Assembly[] assemblies
    )
    {
        var handlerTypes = assemblies
            .SelectMany(x => x.GetTypes())
            .Where(x =>
                x.GetInterface(typeof(IHandler<,>).Name) != null
                && x is { IsClass: true, IsAbstract: false }
            )
            .ToList();

        foreach (var handlerType in handlerTypes)
        {
            services.AddScoped(handlerType);
            
            var interfaceHandlerType = handlerType
                .GetInterfaces()
                .Where(x => x.Name == typeof(IHandler<,>).Name)
                .ToList();
            
            foreach (var interfaceType in interfaceHandlerType)
            {
                services.AddScoped(interfaceType, handlerType);
            }
        }

        return services;
    }
}
