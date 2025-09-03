namespace Open.WebDav;

/// <summary>
/// Contains information about what operations are allowed.
/// </summary>
public class WebDavOptions
{
    /// <summary>
    /// Gets the allowed operations.
    /// </summary>
    public string[] Allow { get; set; }
    /// <summary>
    /// Gets extended operations.
    /// </summary>
    public string[] Dav { get; set; }
}
