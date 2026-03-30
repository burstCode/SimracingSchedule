namespace SimRacingSchedule.Core.ValueObjects;

/// <summary>
/// Value Object для должности.
/// </summary>
public class Position : IEquatable<Position>
{
    public string Title { get; }

    public Position(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Требуется указать имя должности", nameof(value));

        Title = value.Trim();
    }

    public override string ToString()
    {
        return Title;
    }

    public bool Equals(Position? other)
    {
        if (other is null)
            return false;

        return Title == other.Title;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Position);
    }

    public override int GetHashCode()
    {
        return Title.GetHashCode();
    }
}
