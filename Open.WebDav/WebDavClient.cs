using Open.IO;
using Open.Net.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Open.WebDav
{
    /// <summary>
    /// WebDav client helps connecting with servers supporting this protocol.
    /// </summary>
    public class WebDavClient
    {
        #region ** fields

        private string _server, _user, _password, _domain;
        private bool _ignoreCertErrors = false;
        private IHttpMessageHandlerFactory _messageHandlerFactory = null;

        #endregion

        #region ** tags

        /// <summary>
        /// The dav namespace.
        /// </summary>
        public static XNamespace DavNS = XNamespace.Get("DAV:");
        /// <summary>
        /// The foo namespace
        /// </summary>
        public static XNamespace FooNS = XNamespace.Get("http://example.com/foo");
        /// <summary>
        /// The link to a file or folder tag.
        /// </summary>
        public static XName HRef = DavNS + "href";
        /// <summary>
        /// The response tag.
        /// </summary>
        public static XName Response = DavNS + "response";
        /// <summary>
        /// The display name of the file or folder tag.
        /// </summary>
        public static XName DisplayName = DavNS + "displayname";
        /// <summary>
        /// The resource type tag.
        /// </summary>
        public static XName ResourceType = DavNS + "resourcetype";
        /// <summary>
        /// The collection tag.
        /// </summary>
        public static XName Collection = DavNS + "collection";
        /// <summary>
        /// The get content type tag.
        /// </summary>
        public static XName GetContentType = DavNS + "getcontenttype";
        /// <summary>
        /// The get content length tag.
        /// </summary>
        public static XName GetContentLength = DavNS + "getcontentlength";
        /// <summary>
        /// The creation date tag.
        /// </summary>
        public static XName CreationDate = DavNS + "creationdate";
        /// <summary>
        /// The get last modified tag.
        /// </summary>
        public static XName GetLastModified = DavNS + "getlastmodified";
        /// <summary>
        /// The get content language tag.
        /// </summary>
        public static XName GetContentLanguage = DavNS + "getcontentlanguage";
        /// <summary>
        /// The is collection tag.
        /// </summary>
        public static XName IsCollection = DavNS + "iscollection";
        /// <summary>
        /// The is hidden tag.
        /// </summary>
        public static XName IsHidden = DavNS + "ishidden";
        /// <summary>
        /// The status tag.
        /// </summary>
        public static XName Status = DavNS + "status";
        /// <summary>
        /// The mk col tag.
        /// </summary>
        public static XName MKCol = DavNS + "mkcol";
        /// <summary>
        /// The property find tag.
        /// </summary>
        public static XName PropFind = DavNS + "propfind";
        /// <summary>
        /// The supported query grammar set tag.
        /// </summary>
        public static XName SupportedQueryGrammarSet = DavNS + "supported-query-grammar-set";
        /// <summary>
        /// The supported method set tag.
        /// </summary>
        public static XName SupportedMethodSet = DavNS + "supported-method-set";
        /// <summary>
        /// The prop tag.
        /// </summary>
        public static XName Prop = DavNS + "prop";
        /// <summary>
        /// The property name tag.
        /// </summary>
        public static XName PropName = DavNS + "propname";
        /// <summary>
        /// The all property tag.
        /// </summary>
        public static XName AllProp = DavNS + "allprop";
        /// <summary>
        /// The include tag.
        /// </summary>
        public static XName Include = DavNS + "include";
        /// <summary>
        /// The supported live property set tag.
        /// </summary>
        public static XName SupportedLivePropertySet = DavNS + "supported-live-property-set";
        /// <summary>
        /// The supported report set tag.
        /// </summary>
        public static XName SupportedReportSet = DavNS + "supported-report-set";
        /// <summary>
        /// The property update tag.
        /// </summary>
        public static XName PropertyUpdate = DavNS + "propertyupdate";
        /// <summary>
        /// The set tag
        /// </summary>
        public static XName Set = DavNS + "set";
        /// <summary>
        /// The search request tag.
        /// </summary>
        public static XName SearchRequest = DavNS + "searchrequest";
        /// <summary>
        /// The natural language query tag
        /// </summary>
        public static XName NaturalLanguageQuery = FooNS + "natural-language-query";

        #endregion

        #region ** initialization

        /// <summary>
        /// Initializes a new instance of the <see cref="WebDavClient"/> class.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="domain">The domain.</param>
        /// <param name="user">The user.</param>
        /// <param name="password">The password.</param>
        /// <param name="ignoreCertErrors">if set to <c>true</c> ignores certificate errors.</param>
        /// <param name="messageHandlerFactory"><see cref="HttpMessageHandler"/> factory.</param>
        public WebDavClient(string server, string domain, string user, string password, bool ignoreCertErrors = false, IHttpMessageHandlerFactory messageHandlerFactory = null)
        {
            _server = server;
            _user = user;
            _password = password;
            _domain = domain;
            _ignoreCertErrors = ignoreCertErrors;
            _messageHandlerFactory = messageHandlerFactory ?? HttpMessageHandlerFactory.Default;
        }

        #endregion

        #region ** object model

        /// <summary>
        /// Gets the content of the directory specified by <paramref name="path"/>.
        /// </summary>
        /// <param name="path">The directory path to query.</param>
        /// <param name="depth">The depth of the query.</param>
        /// <param name="requestBody">The request body.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<XDocument> PropFindAsync(string path, WebDavDepth depth = WebDavDepth.One, XDocument requestBody = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var uri = BuildApiUri(path);
            var client = CreateClient();
            client.DefaultRequestHeaders.Add("Depth", depth == WebDavDepth.One ? "1" : depth == WebDavDepth.Zero ? "0" : "infinity");
            var request = new HttpRequestMessage(new HttpMethod("PROPFIND"), uri);
            if (requestBody != null)
            {
                var requestStream = new MemoryStream();
                requestBody.Save(requestStream);
                requestStream.Seek(0, SeekOrigin.Begin);
                request.Content = new StreamContent(requestStream);
            }
            else
            {
                request.SetEmptyContent();
            }
            var response = await client.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return XDocument.Load(await response.Content.ReadAsStreamAsync());
            }
            else
            {
                throw await ProcessException(response);
            }
        }

        /// <summary>
        /// Updates the information of a file or folder.
        /// </summary>
        /// <param name="path">The file or folder path.</param>
        /// <param name="requestBody">The request body.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<XDocument> PropPatchAsync(string path, XDocument requestBody, CancellationToken cancellationToken)
        {
            var uri = BuildApiUri(path);
            var client = CreateClient();
            var request = new HttpRequestMessage(new HttpMethod("PROPPATCH"), uri);
            var requestStream = new MemoryStream();
            requestBody.Save(requestStream);
            requestStream.Seek(0, SeekOrigin.Begin);
            request.Content = new StreamContent(requestStream);
            var response = await client.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return XDocument.Load(await response.Content.ReadAsStreamAsync());
            }
            else
            {
                throw await ProcessException(response);
            }
        }

        /// <summary>
        /// Creates a new folder.
        /// </summary>
        /// <param name="path">The path of the new folder.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task MkColAsync(string path, CancellationToken cancellationToken)
        {
            var uri = BuildApiUri(path);
            var client = CreateClient();
            var request = new HttpRequestMessage(new HttpMethod("MKCOL"), uri);
            request.SetEmptyContent();
            var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw await ProcessException(response);
            }
        }

        /// <summary>
        /// Creates a new folder.
        /// </summary>
        /// <param name="path">The path of the new folder.</param>
        /// <param name="requestBody">The request body.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task MkColExtendedAsync(string path, XDocument requestBody, CancellationToken cancellationToken)
        {
            var uri = BuildApiUri(path);
            var client = CreateClient();
            var request = new HttpRequestMessage(new HttpMethod("MKCOL"), uri);
            var requestStream = new MemoryStream();
            requestBody.Save(requestStream);
            requestStream.Seek(0, SeekOrigin.Begin);
            var content = new StreamContent(requestStream);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/xml");
            request.Content = content;
            var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw await ProcessException(response);
            }
        }

        /// <summary>
        /// Gets the content of a file.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<Stream> DownloadFileAsync(string filePath, CancellationToken cancellationToken)
        {
            var uri = BuildApiUri(filePath);
            var client = CreateClient();
            var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return new StreamWithLength(await response.Content.ReadAsStreamAsync(), response.Content.Headers.ContentLength);
            }
            else
            {
                throw await ProcessException(response);
            }
        }

        /// <summary>
        /// Uploads a new file to the specified <paramref name="filePath"/>.
        /// </summary>
        /// <param name="filePath">The file identifier.</param>
        /// <param name="contentType">MimeType of the file. This parameter can be null.</param>
        /// <param name="fileStream">The file stream.</param>
        /// <param name="progress">The progress of the file upload will be notified to this <see cref="IProgress{T}"/>.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task UploadFileAsync(string filePath, string contentType, Stream fileStream, IProgress<StreamProgress> progress, CancellationToken cancellationToken)
        {
            var uri = BuildApiUri(filePath);
            var client = CreateClient();
            var content = new StreamedContent(fileStream, progress, cancellationToken);
            if (!string.IsNullOrWhiteSpace(contentType))
                content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            var response = await client.PutAsync(uri, content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw await ProcessException(response);
            }
        }

        /// <summary>
        /// Deletes the file or folder at the specified <paramref name="path"/>.
        /// </summary>
        /// <param name="path">The path of the resource to be deleted.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task DeleteResourceAsync(string path, CancellationToken cancellationToken)
        {
            var uri = BuildApiUri(path);
            var client = CreateClient();
            var response = await client.DeleteAsync(uri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw await ProcessException(response);
            }
        }

        /// <summary>
        /// Creates a copy of the file at the specified <paramref name="sourceResPath"/> to the <paramref name="targetResPath"/>.
        /// </summary>
        /// <param name="sourceResPath">The source resource path.</param>
        /// <param name="targetResPath">The target resource path.</param>
        /// <param name="overwrite">if set to <c>true</c> it overwrites any existing file at the <paramref name="targetResPath"/>.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task CopyResourceAsync(string sourceResPath, string targetResPath, bool overwrite = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            var uri = BuildApiUri(sourceResPath);
            var client = CreateClient();
            var request = new HttpRequestMessage(new HttpMethod("COPY"), uri);
            request.Headers.Add("Destination", BuildApiUri(targetResPath).AbsoluteUri);
            request.Headers.Add("Overwrite", overwrite ? "T" : "F");
            request.SetEmptyContent();
            var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw await ProcessException(response);
            }
        }

        /// <summary>
        /// Moves a file or folder from <paramref name="sourceResPath"/> to <paramref name="targetResPath"/>.
        /// </summary>
        /// <param name="sourceResPath">The source resource path.</param>
        /// <param name="targetResPath">The target resource path.</param>
        /// <param name="overwrite">if set to <c>true</c> it overwrites any file at <paramref name="targetResPath"/>.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task MoveResourceAsync(string sourceResPath, string targetResPath, bool overwrite = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            var uri = BuildApiUri(sourceResPath);
            var client = CreateClient();
            var request = new HttpRequestMessage(new HttpMethod("MOVE"), uri);
            request.Headers.Add("Destination", BuildApiUri(targetResPath).AbsoluteUri);
            request.Headers.Add("Overwrite", overwrite ? "T" : "F");
            request.SetEmptyContent();
            var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw await ProcessException(response);
            }
        }

        /// <summary>
        /// Gets information about the operations that can be executed at the specified <paramref name="path"/>.
        /// </summary>
        /// <param name="path">The folder path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<WebDavOptions> OptionsAsync(string path, CancellationToken cancellationToken)
        {
            var uri = BuildApiUri(path);
            var client = CreateClient();
            var request = new HttpRequestMessage(new HttpMethod("OPTIONS"), uri);
            request.SetEmptyContent();
            var response = await client.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                IEnumerable<string> dav = new string[0];
                var davHeader = response.Headers.TryGetValues("DAV", out dav);
                var sDav = dav.FirstOrDefault() ?? "";
                var options = new WebDavOptions { Allow = response.Content.Headers.Allow.ToArray(), Dav = sDav.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray() };
                return options;
            }
            else
            {
                throw await ProcessException(response);
            }
        }

        /// <summary>
        /// Performs a search according to <paramref name="queryDoc"/>.
        /// </summary>
        /// <param name="path">The folder path.</param>
        /// <param name="queryDoc">The query document.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<XDocument> SearchAsync(string path, XDocument queryDoc, CancellationToken cancellationToken)
        {
            var uri = BuildApiUri("/");
            var client = CreateClient();
            var request = new HttpRequestMessage(new HttpMethod("SEARCH"), uri);
            var requestStream = new MemoryStream();
            queryDoc.Save(requestStream);
            var content = new StreamContent(requestStream);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/xml");
            request.Content = content;
            var response = await client.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return XDocument.Load(await response.Content.ReadAsStreamAsync());
            }
            else
            {
                throw await ProcessException(response);
            }
        }

        #endregion

        #region ** private stuff

        private async Task<Exception> ProcessException(HttpResponseMessage response)
        {
            return new WebDavException(response.ReasonPhrase, (int)response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        private Uri BuildApiUri(string path)
        {
            path = string.Join("/", path.Split('\\', '/').Select(str => EscapeUriString(str)).ToArray());
            return new Uri(_server + path);
        }
        private string EscapeUriString(string text)
        {
            var builder = new StringBuilder(Uri.EscapeDataString(text));
            builder.Replace("!", "%21");
            builder.Replace("'", "%27");
            builder.Replace("(", "%28");
            builder.Replace(")", "%29");
            return builder.ToString();
        }

        private HttpClient CreateClient()
        {
            var handler = _messageHandlerFactory.GetHttpMessageHandler(new NetworkCredential(_user, _password, _domain), ignoreCertErrors: _ignoreCertErrors);
            var client = new HttpClient(new DigestAuthenticationMessageHandler(handler));
            client.Timeout = Timeout.InfiniteTimeSpan;
            return client;
        }

        #endregion
    }
}
