namespace SoloDevBoard.Domain;

/// <summary>Marker interface for domain aggregate roots.</summary>
public interface IAggregate
{
    int Id { get; }
}
