using System;
using System.Collections.Generic;

namespace TestRecognition.Dto
{
    public class Answer
    {
        public int Id { get; set; }

        public string Text { get; set; }

        public bool IsCorrect { get; set; }

        public virtual ICollection<Question> Questions { get; set; }
    }

    public class Template
    {
        public Guid Id { get; set; }

        public virtual ICollection<Question> Questions { get; set; }

        //TODO ask if question answers can be less than related answers in db.

        public virtual ICollection<SkipData> SkipData { get; set; }
    }

    public class QuestionAnswers
    {
        public int Id { get; set; }

        public Question Question { get; set; }

        public Answer Answer { get; set; }

        public bool IsValid { get; set; }

        public int MyProperty { get; set; }

        public Action Action { get; set; } // todo ask for specific actions related with user input (skip, handrwite)
    }

    public class Action
    {
    }
}