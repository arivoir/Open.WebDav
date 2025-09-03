namespace Open.WebDav;

/// <summary>
/// Specifies the depth of the PropFind query.
/// </summary>
public enum WebDavDepth
{
    /// <summary>
    /// Only the specified folder information is returned.
    /// </summary>
    Zero,
    /// <summary>
    /// All the direct files and folders in the specified folder are returned.
    /// </summary>
    One,
    /// <summary>
    /// All the sub folders and files are returned.
    /// </summary>
    Infinity,
}
