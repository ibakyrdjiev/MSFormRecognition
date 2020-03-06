namespace TestRecognition.Dto.TextRecognition
{
    using System.Collections.Generic;

    public class ScannedPageDto
    {
        public double Width { get; set; }

        public double Height { get; set; }

        public Queue<ScannedLine> ScannedLinesQueue { get; set; }
    }
}