using TestRecognition.Dto.TextRecognition;

namespace TestRecognition.Models
{
    public class MappedQuestionsModel
    {
        public string BlobName { get; set; }

        public ScannedPageDto ScannedPage { get; set; }
    }
}
