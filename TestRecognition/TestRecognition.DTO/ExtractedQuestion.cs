namespace TestRecognition.Dto
{
    using System.Collections.Generic;
    using TestRecognition.Dto.Enums;

    public class ExtractedQuestion
    {
        public ExtractedQuestion()
        {
            this.ExtractedAnswers = new List<ExtractedAnswer>();
        }

        public ScannedLine ResultLine { get; set; }

        public List<ExtractedAnswer> ExtractedAnswers { get; set; }

        public QuestionAnswerType QuestionAnswerType { get; set; }

        public Question Original { get; set; }

        public bool IsAnswered { get; set; }

        public bool? IsAnsweredCorectly { get; set; }

        public int OrderNumber { get; set; }
    }
}