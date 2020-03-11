namespace TestRecognition.Dto
{
    using System.Collections.Generic;
    using TestRecognition.Dto.Enums;

    public class Question
    {
        public int Id { get; set; }

        public string Text { get; set; }

        public List<Answer> Answers { get; set; }

        public QuestionAnswerType QuestionAnswerType { get; set; }

        public ICollection<Question> ChildQuestions { get; set; }

        public List<string> MetaData { get; set; }
    }
}