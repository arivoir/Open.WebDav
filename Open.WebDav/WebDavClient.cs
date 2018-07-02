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
    public class WebDavClient
    {
        #region ** fields

        private string _server, _user, _password, _domain;
        private bool _ignoreCertErrors = false;
        public static XNamespace DavNS = XNamespace.Get("DAV:");
        public static XNamespace FooNS = XNamespace.Get("http://example.com/foo");
        public static XName HRef = DavNS + "href";
        public static XName Response = DavNS + "response";
        public static XName DisplayName = DavNS + "displayname";
        public static XName ResourceType = DavNS + "resourcetype";
        public static XName Collection = DavNS + "collection";
        public static XName GetContentType = DavNS + "getcontenttype";
        public static XName GetContentLength = DavNS + "getcontentlength";
        public static XName CreationDate = DavNS + "creationdate";
        public static XName GetLastModified = DavNS + "getlastmodified";
        public static XName GetContentLanguage = DavNS + "getcontentlanguage";
        public static XName IsCollection = DavNS + "iscollection";
        public static XName IsHidden = DavNS + "ishidden";
        public static XName Status = DavNS + "status";
        public static XName MKCol = DavNS + "mkcol";
        public static XName PropFind = DavNS + "propfind";
        public static XName SupportedQueryGrammarSet = DavNS + "supported-query-grammar-set";
        public static XName SupportedMethodSet = DavNS + "supported-method-set";
        public static XName Prop = DavNS + "prop";
        public static XName PropName = DavNS + "propname";
        public static XName AllProp = DavNS + "allprop";
        public static XName Include = DavNS + "include";
        public static XName SupportedLivePropertySet = DavNS + "supported-live-property-set";
        public static XName SupportedReportSet = DavNS + "supported-report-set";
        public static XName PropertyUpdate = DavNS + "propertyupdate";
        public static XName Set = DavNS + "set";
        public static XName SearchRequest = DavNS + "searchrequest";
        public static XName NaturalLanguageQuery = FooNS + "natural-language-query";

        #endregion

        #region ** initialization

        public WebDavClient(string server, string domain, string user, string password, bool ignoreCertErrors = false)
        {
            _server = server;
            _user = user;
            _password = password;
            _domain = domain;
            _ignoreCertErrors = ignoreCertErrors;
        }

        #endregion

        #region ** object model

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

        public async Task<Stream> DownloadFileAsync(string fileId, CancellationToken cancellationToken)
        {
            var uri = BuildApiUri(fileId);
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

        public async Task UploadFileAsync(string fileId, string contentType, Stream fileStream, IProgress<StreamProgress> progress, System.Threading.CancellationToken cancellationToken)
        {
            var uri = BuildApiUri(fileId);
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


        public async Task<Options> OptionsAsync(string path, CancellationToken cancellationToken)
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
                var options = new Options { Allow = response.Content.Headers.Allow.ToArray(), Dav = sDav.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray() };
                return options;
            }
            else
            {
                throw await ProcessException(response);
            }
        }

        public async Task<XDocument> SearchAsync(string dirId, XDocument queryDoc, CancellationToken cancellationToken)
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
            var handler = new HttpClientHandler();
            //handler.ServerCertificateCustomValidationCallback = (requestMessage, X509Certificate2, X509Chain, SslPolicyErrors) =>
            //{
            //    return _ignoreCertErrors;
            //};

            //var handler = new WinRTHttpClientHandler();
            //if (_ignoreCertErrors)
            //{
            //    handler.IgnorableServerCertificateErrors.Add(ChainValidationResult.Untrusted);
            //    handler.IgnorableServerCertificateErrors.Add(ChainValidationResult.Expired);
            //}
            //handler.PreAuthenticate = true; //This is necessary for Android because authentication negotiation is not performed when sending a PUT request.
            handler.Credentials = new NetworkCredential(_user, _password, _domain);
            //var client = new HttpClient(new AndroidWorkaroundAuthenticationFilter(handler));

            var client = new HttpClient(handler/*HttpClientEx.GetHttpMessageHandler(new NetworkCredential(_user, _password, _domain), ignoreCertErrors:_ignoreCertErrors)*/);
            client.Timeout = Timeout.InfiniteTimeSpan;
            return client;
        }

        #endregion
    }

    public enum WebDavDepth
    {
        Zero,
        One,
        Infinity,
    }

    public class WebDavException : Exception
    {
        public WebDavException(string reasonPhrase, int statusCode, string message)
            : base(message)
        {
            ReasonPhrase = reasonPhrase;
            StatusCode = statusCode;
        }

        public string ReasonPhrase { get; private set; }
        public int StatusCode { get; private set; }
    }

    public class Options
    {
        public string[] Allow { get; set; }
        public string[] Dav { get; set; }
    }
}
