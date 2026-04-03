namespace Order.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
