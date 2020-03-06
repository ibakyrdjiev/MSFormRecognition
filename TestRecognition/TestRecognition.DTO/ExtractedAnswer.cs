namespace TestRecognition.Dto
{
    public class ExtractedAnswer
    {
        public ScannedLine ResultLine { get; set; }

        public bool IsSelected { get; set; }

        public double Coverage { get; set; }
    }
}