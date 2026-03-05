namespace MinGo.CertManager.SDK.Exceptions;

public class SdkException : Exception
{
    public SdkException() { }

    public SdkException(string message) : base(message) { }

    public SdkException(string message, Exception innerException) : base(message, innerException) { }
}