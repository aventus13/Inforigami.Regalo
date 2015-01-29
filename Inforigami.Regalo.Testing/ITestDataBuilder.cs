namespace Inforigami.Regalo.Testing
{
    public interface ITestDataBuilder<T>
    {
        string CurrentDescription { get; }
        T Build();
    }
}
