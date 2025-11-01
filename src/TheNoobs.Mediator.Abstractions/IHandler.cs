using TheNoobs.Results;

namespace TheNoobs.Mediator.Abstractions;

public interface IHandler<TCommand, TResult>
where TCommand : notnull
where TResult : notnull
{
    ValueTask<Result<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
