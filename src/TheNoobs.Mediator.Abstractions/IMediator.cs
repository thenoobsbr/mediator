using TheNoobs.Results;
using Void = TheNoobs.Results.Types.Void;

namespace TheNoobs.Mediator.Abstractions;

public interface IMediator
{
    ValueTask<Result<TResult>> SendAsync<TCommand, TResult>(
        TCommand command,
        CancellationToken cancellationToken
    )
        where TCommand : notnull
        where TResult : notnull;

    ValueTask<Result<Void>> PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken)
        where TEvent : notnull;
}
