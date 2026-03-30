using System.Net.Mail;

namespace SimRacingSchedule.Core.ValueObjects;

/// <summary>
/// Value Object для адреса электронной почты.
/// </summary>
public class Email : IEquatable<Email>
{
    public string Value { get; }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Требуется указать Email", nameof(value));

        if (!IsValidEmail(value))
            throw new ArgumentException("Неверный формат Email", nameof(value));

        Value = value.ToLowerInvariant();
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            MailAddress address = new(email);
            return address.Address == email;
        }
        catch
        {
            return false;
        }
    }

    public override string ToString()
    {
        return Value;
    }

    public bool Equals(Email? other)
    {
        if (other is null)
            return false;

        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Email);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }
}