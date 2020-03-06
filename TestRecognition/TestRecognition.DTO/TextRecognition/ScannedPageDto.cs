namespace TestRecognition.Dto.TextRecognition
{
    using System.Collections.Generic;

    public class ScannedPageDto
    {
        public string BlobName { get; set; }

        public double Width { get; set; }

        public double Height { get; set; }

        public Queue<ScannedLine> ScannedLinesQueue { get; set; }
    }
}
