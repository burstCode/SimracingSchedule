namespace SimRacingSchedule.Core.ValueObjects;

/// <summary>
/// Value Object для полного имени.
/// </summary>
public class FullName : IEquatable<FullName>
{
    public string FirstName { get; }
    public string LastName { get; }
    public string? Patronymic { get; } = string.Empty;

    public FullName(string firstName, string lastName, string? patronymic = null)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("Требуется указать имя", nameof(firstName));

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Требуется указать фамилию", nameof(lastName));

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Patronymic = patronymic?.Trim();
    }

    public bool Equals(FullName? other)
    {
        if (other is null)
            return false;

        return FirstName == other.FirstName &&
               LastName == other.LastName &&
               Patronymic == other.Patronymic;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as FullName);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(FirstName, LastName, Patronymic);
    }
}
