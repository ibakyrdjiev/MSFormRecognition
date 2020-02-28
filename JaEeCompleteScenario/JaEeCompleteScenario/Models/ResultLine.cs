using System.Collections.Generic;

namespace ComputerVision.Models
{
    public class ResultLine
    {
        public Position Position { get; set; }

        public string Text { get; set; }

        public List<Word> Words { get; set; }
    }
}
