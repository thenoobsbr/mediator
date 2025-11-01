using TheNoobs.Results;

namespace TheNoobs.Mediator.Abstractions;

public interface IHandlerPipeline<TCommand, TResult>
where TCommand : notnull
where TResult : notnull
{
    ValueTask<Result<TResult>> HandleAsync(
        TCommand command,
        Func<TCommand, CancellationToken, ValueTask<Result<TResult>>> next,
        CancellationToken cancellationToken
    );
}
