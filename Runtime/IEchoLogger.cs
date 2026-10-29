namespace Astra.Echo
{
    /// <summary>Sink for engine diagnostics. Null is accepted everywhere.</summary>
    public interface IEchoLogger
    {
        /// <summary>True when <see cref="Info"/> messages are worth building (replaces a level check).</summary>
        bool IsInfoEnabled { get; }

        void Info(string message);
        void Warn(string message);
        void Error(string message);
    }
}
