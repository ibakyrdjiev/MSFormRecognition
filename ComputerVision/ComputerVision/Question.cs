using System.Collections.Generic;

namespace ComputerVision
{
    public class Question
    {
        public Question()
        {
            this.Answers = new List<Answer>();
        }

        public string Text { get; set; }

        public List<Answer> Answers { get; set; }

        public QuestionAnswerType QuestionAnswerType { get; set; }

        public List<string> MetaData { get; set; }
    }

    public enum QuestionAnswerType
    {
        UnderCheckBox,
        Table,
        FreeText
    }
}
