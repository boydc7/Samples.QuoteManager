namespace Samples.QuoteManager.Recommended
{
    public interface ILockManager
    {
        void EnterRead(string key);
        void ExitRead(string key);
        void EnterIntentWrite(string key);
        void ExitIntentWrite(string key);
        void EnterWrite(string key);
        void ExitWrite(string key);
    }
}
