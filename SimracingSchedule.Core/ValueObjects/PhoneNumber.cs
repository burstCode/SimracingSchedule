using System.Text.RegularExpressions;

namespace SimRacingSchedule.Core.ValueObjects;

public class PhoneNumber : IEquatable<PhoneNumber>
{
    public string Value { get; }

    public PhoneNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Требуется указать номер телефона", nameof(value));

        string cleaned = CleanPhoneNumber(value);

        if (!IsValidPhoneNumber(cleaned))
            throw new ArgumentException("Неверный формат номера телефона", nameof(value));

        Value = cleaned;
    }

    private string CleanPhoneNumber(string phone)
    {
        // Удаление не цифровых символов.
        return Regex.Replace(phone, @"[^\d+]", "");
    }

    private bool IsValidPhoneNumber(string phone)
    {
        // Проверка российского номера.
        string pattern = @"^(\+7|8)\d{10}$";
        return Regex.IsMatch(phone, pattern);
    }

    public bool Equals(PhoneNumber? other)
    {
        if (other is null)
            return false;

        return Value == other.Value;
    }

    public override string ToString()
    {
        return Value;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as PhoneNumber);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }
}
