using DotNetEnv;
using Open.IO;
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
            var options = await client.OptionsAsync(_serverPath, CancellationToken.None);
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
        public async Task GetItemsTest()
        {
            var stringToUpload = "Hello, World!";
            var client = new WebDavClient(_server, _domain, _user, _password);
            await client.UploadFileAsync(Path.Combine(_rootFolderId, "file.txt"), "text/plain", new MemoryStream(Encoding.UTF8.GetBytes(stringToUpload)), new Progress<StreamProgress>(p => { }), CancellationToken.None);
            var result = await client.PropFindAsync(_rootFolderId);
            var descendants = result.Descendants(WebDavClient.Response);
            Assert.That(descendants, Is.Not.Null);
            Assert.That(descendants.Count(), Is.EqualTo(2));
        }
    }
}
