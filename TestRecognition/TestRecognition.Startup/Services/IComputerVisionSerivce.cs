namespace TestRecognition.Core.Services
{
    using System.IO;
    using System.Threading.Tasks;
    using TestRecognition.Dto.TextRecognition;

    public interface IComputerVisionSerivce
    {
        Task<ScannedPageDto> GetBlobTextContent(Stream imageStream);
    }
}