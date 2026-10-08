namespace GraphPlugin.Nanocad.Runtime;

public sealed class IntegrationTestException
    : System.Exception
{
    public IntegrationTestException(
        string message)
        : base(message)
    {
    }
}