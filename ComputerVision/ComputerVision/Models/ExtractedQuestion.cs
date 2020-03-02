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

        public Question Original { get; set; }
    }

    public class ExtractedAnswer
    {
        public ResultLine ResultLine { get; set; }
    }
}
