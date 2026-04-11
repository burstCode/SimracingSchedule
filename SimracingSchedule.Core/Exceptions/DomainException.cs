namespace SimRacingSchedule.Core.Exceptions;

/// <summary>
/// Исключение, сгенерированное в доменной службе.
/// </summary>
public class DomainException : Exception
{
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="DomainException"/>.
    /// </summary>
    /// <param name="message">Сообщение об ошибке.</param>
    public DomainException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="DomainException"/>.
    /// </summary>
    /// <param name="message">Сообщение об ошибке.</param>
    /// <param name="innerException">Внутреннее исключение.</param>
    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
