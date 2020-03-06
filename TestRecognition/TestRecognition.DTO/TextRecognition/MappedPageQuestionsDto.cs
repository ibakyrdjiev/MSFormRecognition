namespace TestRecognition.Dto.TextRecognition
{
    using System.Collections.Generic;

    public class MappedPageQuestionsDto
    {
        public string BlobName { get; set; }

        public double Width { get; set; }

        public double Height { get; set; }

        public List<ExtractedQuestion> ExtractedQuestions { get; set; }
    }
}