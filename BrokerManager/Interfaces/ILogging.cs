namespace BrokerManager.Interfaces
{
    public interface ILogging
    {
        void LogWarning(string message);

        void LogError(string message);
    }
}
