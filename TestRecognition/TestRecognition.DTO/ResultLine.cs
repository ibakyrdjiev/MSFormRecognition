using System.Collections.Generic;

namespace TestRecognition.Dto
{
    public class ScannedLine
    {
        public Position Position { get; set; }

        public string Text { get; set; }

        public List<Word> Words { get; set; }

        public bool IsUseful { get; set; }
    }
}