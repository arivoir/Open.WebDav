using DotNetEnv;
using Open.IO;
using Sprache;
using System.Text;
using System.Xml.Linq;

namespace Open.WebDav.Test
{
    public class Tests
    {
        private string _server;
        private string _serverPath;
        private string _domain;
        private string _user;
        private string _password;
        private string _rootFolderId;

        [SetUp]
        public async Task Setup()
        {
            Env.Load();
            _server = Environment.GetEnvironmentVariable("SERVER")!;
            _serverPath = Environment.GetEnvironmentVariable("SERVER_PATH")!;
            _domain = Environment.GetEnvironmentVariable("DOMAIN")!;
            _user = Environment.GetEnvironmentVariable("USER")!;
            _password = Environment.GetEnvironmentVariable("PASSWORD")!;

            var client = new WebDavClient(_server, _domain, _user, _password);
            var rootFolderName = Guid.NewGuid().ToString();
            _rootFolderId = Path.Combine(_serverPath, rootFolderName);
            await client.MkColAsync(_rootFolderId, CancellationToken.None);
        }

        [TearDown]
        public async Task TearDown()
        {
            var client = new WebDavClient(_server, _domain, _user, _password);
            await client.DeleteResourceAsync(_rootFolderId, CancellationToken.None);
        }

        [Test]
        public async Task PropFindTest()
        {
            var stringToUpload = "Hello, World!";
            var client = new WebDavClient(_server, _domain, _user, _password);
            await client.UploadFileAsync(Path.Combine(_rootFolderId, "file.txt"), "text/plain", new MemoryStream(Encoding.UTF8.GetBytes(stringToUpload)), null, CancellationToken.None);
            var result = await client.PropFindAsync(_rootFolderId);

            var descendants = result.Descendants(WebDavClient.Response)
                .Where(d => d.Element(WebDavClient.HRef)!.Value.TrimEnd('/') != _rootFolderId)
                .Select(d => d.Descendants(WebDavClient.DisplayName).First().Value).ToArray();
            Assert.That(descendants, Is.Not.Null);
            Assert.That(descendants.Length, Is.EqualTo(1));
            Assert.That(descendants[0], Is.EqualTo("file.txt"));
        }

        [Test]
        public async Task ProprPatchTest()
        {
            var stringToUpload = "Hello, World!";
            var client = new WebDavClient(_server, _domain, _user, _password);
            var filePath = Path.Combine(_rootFolderId, "file.txt");
            await client.UploadFileAsync(filePath, "text/plain", new MemoryStream(Encoding.UTF8.GetBytes(stringToUpload)), null, CancellationToken.None);
            var fileProperties = new XDocument();
            var propUpdate = new XElement(WebDavClient.PropertyUpdate);
            propUpdate.Add(new XAttribute(XNamespace.Xmlns + "dav", WebDavClient.DavNS));
            var setProp = new XElement(WebDavClient.Set);
            var prop = new XElement(WebDavClient.Prop);
            var propValue = new XElement(WebDavClient.FooNS + "MyProperty");
            propValue.Value = "Custom property value";
            prop.Add(propValue);
            setProp.Add(prop);
            propUpdate.Add(setProp);
            fileProperties.Add(propUpdate);
            var result = await client.PropPatchAsync(filePath, fileProperties, CancellationToken.None);
            var status = result.Descendants(WebDavClient.Status).First();
            Assert.That(status, Is.Not.Null);
            Assert.That(status.Value, Is.EqualTo("HTTP/1.1 200 OK"));
        }

        [Test]
        public async Task DownloadTest()
        {
            var stringToUpload = "Hello, World!";
            var client = new WebDavClient(_server, _domain, _user, _password);
            var filePath = Path.Combine(_rootFolderId, "file.txt");
            await client.UploadFileAsync(filePath, "text/plain", new MemoryStream(Encoding.UTF8.GetBytes(stringToUpload)), null, CancellationToken.None);
            var fileStream = await client.DownloadFileAsync(filePath, CancellationToken.None);
            var fileContent = await new StreamReader(fileStream).ReadToEndAsync();
            Assert.That(fileContent, Is.EqualTo(stringToUpload));
        }

        [Test]
        public async Task CopyTest()
        {
            var stringToUpload = "Hello, World!";
            var client = new WebDavClient(_server, _domain, _user, _password);
            var filePath = Path.Combine(_rootFolderId, "file.txt");
            await client.UploadFileAsync(filePath, "text/plain", new MemoryStream(Encoding.UTF8.GetBytes(stringToUpload)), null, CancellationToken.None);
            var file2Path = Path.Combine(_rootFolderId, "file2.txt");
            await client.CopyResourceAsync(filePath, file2Path);
            var result = await client.PropFindAsync(_rootFolderId);

            var descendants = result.Descendants(WebDavClient.Response)
                .Where(d => d.Element(WebDavClient.HRef)!.Value.TrimEnd('/') != _rootFolderId)
                .Select(d => d.Descendants(WebDavClient.DisplayName).First().Value).ToArray();
            Assert.That(descendants, Is.Not.Null);
            Assert.That(descendants.Length, Is.EqualTo(2));
            Assert.That(descendants[0], Is.EqualTo("file.txt"));
            Assert.That(descendants[1], Is.EqualTo("file2.txt"));
        }
    }
}
