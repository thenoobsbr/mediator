using Microsoft.Extensions.DependencyInjection;
using TheNoobs.Mediator.Abstractions;
using TheNoobs.Results;
using TheNoobs.Results.Types;
using Void = TheNoobs.Results.Types.Void;

namespace TheNoobs.Mediator;

internal class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider =
            serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public ValueTask<Result<TResult>> SendAsync<TCommand, TResult>(
        TCommand command,
        CancellationToken cancellationToken)
        where TCommand : notnull
        where TResult : notnull
    {
        if (!typeof(TCommand).IsInterface)
        {
            return new ValueTask<Result<TResult>>(
                new InvalidInputFail("Command must be an interface")
            );
        }

        var handler = _serviceProvider.GetRequiredService<IHandler<TCommand, TResult>>();
        var pipelines = _serviceProvider
            .GetServices<IHandlerPipeline<TCommand, TResult>>()
            .Reverse()
            .ToList();
        
        if (!pipelines.Any())
        {
            return handler.HandleAsync(command, cancellationToken);
        }

        var next = handler.HandleAsync;
        foreach (var pipeline in pipelines)
        {
            var currentNext = next;
            next = (cmd, token) => pipeline.HandleAsync(cmd, currentNext, token);
        }

        return next(command, cancellationToken);
    }

    public ValueTask<Result<Void>> PublishAsync<TEvent>(
        TEvent @event,
        CancellationToken cancellationToken)
        where TEvent : notnull
    {
        var handler = _serviceProvider.GetService<IEventHandler<TEvent>>();

        if (handler is null)
        {
            return Void.Value;
        }

        return handler.HandleAsync(@event, cancellationToken);
    }
}
