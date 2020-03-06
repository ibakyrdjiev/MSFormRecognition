namespace TestRecognition.Core.Services
{
    using System.IO;
    using System.Threading.Tasks;

    public interface IBlobService
    {
        Task<Stream> GetBlobStream(string blobName);
    }
}