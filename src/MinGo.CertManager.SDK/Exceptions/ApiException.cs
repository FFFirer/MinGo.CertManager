namespace MinGo.CertManager.SDK.Exceptions;

public class ApiException : SdkException
{
    public int StatusCode { get; set; }

    public ApiException() { }

    public ApiException(string message) : base(message) { }

    public ApiException(string message, int statusCode) : base(message)
    {
        StatusCode = statusCode;
    }

    public ApiException(string message, Exception innerException) : base(message, innerException) { }

    public ApiException(string message, int statusCode, Exception innerException) : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}