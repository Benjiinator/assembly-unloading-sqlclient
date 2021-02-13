namespace Contract
{
    public interface IModule
    {
        string Name { get; }

        void Initialize();
        void Execute();
        void Close();
    }
}
