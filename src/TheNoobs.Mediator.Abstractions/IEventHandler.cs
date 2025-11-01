using TheNoobs.Results;

namespace TheNoobs.Mediator.Abstractions;

public interface IEventHandler<TEvent>
where TEvent : notnull
{
    ValueTask<Result<Results.Types.Void>> HandleAsync(TEvent @event, CancellationToken cancellationToken);
}
