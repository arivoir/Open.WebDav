namespace Open.WebDav;

/// <summary>
/// Provides information about exceptions thrown.
/// </summary>
public class WebDavException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WebDavException"/> class.
    /// </summary>
    /// <param name="reasonPhrase">The reason phrase.</param>
    /// <param name="statusCode">The status code.</param>
    /// <param name="message">The message.</param>
    public WebDavException(string reasonPhrase, int statusCode, string message)
        : base(message)
    {
        ReasonPhrase = reasonPhrase;
        StatusCode = statusCode;
    }

    /// <summary>
    /// Gets the reason why the exception happened.
    /// </summary>
    public string ReasonPhrase { get; private set; }

    /// <summary>
    /// Gets the status code of the network request.
    /// </summary>
    public int StatusCode { get; private set; }
}
