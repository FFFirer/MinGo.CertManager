namespace MinGo.CertManager.SDK.Exceptions;

public class AuthenticationException : SdkException
{
    public AuthenticationException() { }

    public AuthenticationException(string message) : base(message) { }

    public AuthenticationException(string message, Exception innerException) : base(message, innerException) { }
}