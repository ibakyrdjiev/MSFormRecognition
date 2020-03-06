namespace TestRecognition.Services
{
    using Microsoft.Extensions.Logging;
    using Microsoft.WindowsAzure.Storage.Blob;
    using System.IO;
    using System.Threading.Tasks;
    using TestRecognition.Core.Services;

    public class BlobService : BaseService, IBlobService
    {
        private readonly CloudBlobClient cloudBlobClient;
        private const string containerName = "inputblob";

        public BlobService(ILogger logger, CloudBlobClient cloudBlobClient) : base(logger)
        {
            this.cloudBlobClient = cloudBlobClient;
        }

        public Task<Stream> GetBlobStream(string blobName)
        {
            return this.RetrieveBlobByName(blobName).OpenReadAsync();
        }

        private CloudBlockBlob RetrieveBlobByName(string blobName)
        {
            var container = this.cloudBlobClient.GetContainerReference(containerName);
            return container.GetBlockBlobReference(blobName);
        }
    }
}