using System;
using System.Collections.Generic;
using System.Text;

namespace ComputerVision.Models
{
    public class ExtractedQuestion
    {
        public ExtractedQuestion()
        {
            this.ExtractedAnswers = new List<ExtractedAnswer>();
        }

        public ResultLine ResultLine { get; set; }

        public List<ExtractedAnswer> ExtractedAnswers { get; set; }

        public QuestionAnswerType QuestionAnswerType { get; set; }

        public bool IsAnswered { get; set; }

        public bool? IsAnsweredCorectly { get; set; }
    }

    public class ExtractedAnswer
    {
        public ResultLine ResultLine { get; set; }

        public bool IsSelected { get; set; }

        public double Coverage { get; set; }
    }
}
