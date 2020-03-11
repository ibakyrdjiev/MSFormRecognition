namespace TestRecognition.Dto
{
    using System.Collections.Generic;
    using TestRecognition.Dto.Enums;

    public static class Utils
    {
        public static List<Question> GetFakeQuestions()
        {
            var questions = new List<Question>();

            //var first = new Question();
            //first.QuestionAnswerType = QuestionAnswerType.Vertical;
            //first.Answers = new List<Answer>();
            //first.Text = "Of the following steps, which would be the first step in financial planning?";
            //first.Answers.Add(new Answer() { Text = "Get a job so you can start earning money." });
            //first.Answers.Add(new Answer() { Text = "Determine your needs and goals for the future." });
            //first.Answers.Add(new Answer() { Text = "Starting looking for a home to buy." });
            //first.Answers.Add(new Answer() { Text = " Save a portion of your money for the future." });
            //questions.Add(first);

            //var second = new Question();
            //second.QuestionAnswerType = QuestionAnswerType.Vertical;
            //second.Answers = new List<Answer>();
            //second.Text = "The best predictor of how much money you will make in the future is the you choose.";
            //second.Answers.Add(new Answer() { Text = "skills" });
            //second.Answers.Add(new Answer() { Text = "connections" });
            //second.Answers.Add(new Answer() { Text = "education" });
            //second.Answers.Add(new Answer() { Text = "all of the above" });
            //questions.Add(second);

            //var third = new Question()
            //{
            //    QuestionAnswerType = QuestionAnswerType.Vertical,
            //    Text = "Which of the following should you remember when developing a savings plan?",
            //    Answers = new List<Answer>()
            //    {
            //        new Answer()
            //        {
            //            Text = "Wait until you are 40 years old before saving."
            //        },
            //        new Answer()
            //        {
            //            Text = "Pay yourself first."
            //        },
            //        new Answer()
            //        {
            //            Text = "Pay off your low-interest debt first."
            //        },
            //        new Answer()
            //        {
            //            Text = "Have only high-risk investments."
            //        }
            //    }
            //};
            //questions.Add(third);

            //var fourth = new Question()
            //{
            //    QuestionAnswerType = QuestionAnswerType.Vertical,
            //    Text = "All of the following are elements of financial planning except one. Which one is NOT?",
            //    Answers = new List<Answer>()
            //    {
            //        new Answer()
            //        {
            //            Text = "Earn money"
            //        },
            //        new Answer()
            //        {
            //            Text = "Save money"
            //        },
            //        new Answer()
            //        {
            //            Text = "Never use credit"
            //        },
            //        new Answer()
            //        {
            //            Text = "Spend money wisely"
            //        }
            //    }
            //};
            //questions.Add(fourth);

            //var fith = new Question()
            //{
            //    QuestionAnswerType = QuestionAnswerType.Vertical,
            //    Text = "Alan has created a new budget. Which of the following should he NOT do?",
            //    Answers = new List<Answer>()
            //    {
            //        new Answer()
            //        {
            //            Text = "Have a spending plan."
            //        },
            //        new Answer()
            //        {
            //            Text = "Spend less than he earns."
            //        },
            //        new Answer()
            //        {
            //            Text = "Use credit for all items not in his budget."
            //        },
            //        new Answer()
            //        {
            //            Text = "Stick to his budget"
            //        }
            //    }
            //};
            //questions.Add(fith);

            var sixth = new Question()
            {
                Text = "What are the first three letters of your last name?",
                QuestionAnswerType = QuestionAnswerType.FreeText,
                Answers = null
            };

            questions.Add(sixth);

            var seventh = new Question()
            {
                Text = "When were you born?",
                QuestionAnswerType = QuestionAnswerType.FreeText,
                Answers = null,
                MetaData = new List<string>() { "Month", "Day" }
            };

            questions.Add(seventh);

            var question2_09 = new Question()
            {
                Text = "Which of the following is the source of a successful product and service idea?",
                QuestionAnswerType = QuestionAnswerType.Vertical,
                Answers = new List<Answer> 
                { 
                    new Answer{ Text = "An idea that solves the problem"},
                    new Answer{ Text = "An idea that changes or improves a current product or service"},
                    new Answer{ Text = "An idea that develops from the enterpreneur's first hand knoledge of a group"},
                    new Answer{ Text = "All of the above"}
                },
            };

            questions.Add(question2_09);

            question2_09 = new Question()
            {
                Text = "What grade are you in?",
                QuestionAnswerType = QuestionAnswerType.Inline,
                Answers = new List<Answer>
                {
                    new Answer{ Text = "9th"},
                    new Answer{ Text = "10th"},
                    new Answer{ Text = "11th"},
                    new Answer{ Text = "12th"},
                },
            };

            questions.Add(question2_09);

            question2_09 = new Question()
            {
                Text = "Which of the following is the best definiyion of demographic?",
                QuestionAnswerType = QuestionAnswerType.Inline,
                Answers = new List<Answer>
                {
                    new Answer{ Text = "The means by which the product or service is made known and sold to the customers"},
                    new Answer{ Text = "A group that shares characteristics that is used to identify consumer markets"},
                    new Answer{ Text = "The expectations and behaviours of a business that set it apart from its competitors through improvements in quality, value, or delivery"},
                    new Answer{ Text = "None of the above"},
                },
            };

            questions.Add(question2_09);

            question2_09 = new Question()
            {
                Text = "Gloria has designed a new line of jewelry and wants to begin marketing her jewelry in her town. Which choice best describes what Gloria will be doing?",
                QuestionAnswerType = QuestionAnswerType.Inline,
                Answers = new List<Answer>
                {
                    new Answer{ Text = "Making her product known and selling to the customers"},
                    new Answer{ Text = "Creating a new product for a group sharing characteristics that is used to identify consumer markets"},
                    new Answer{ Text = "Creating expectations and behaviors for her business that set it apart from its competitors through improvements in quality, value, or delivery"},
                    new Answer{ Text = "Developing a new product or service, or improving on an existing product or service"},
                },
            };

            questions.Add(question2_09);

            return questions;
        }

        public static List<SkipData> GetSkippedData()
        {
            var result = new List<SkipData>();
            result.Add(new SkipData("Pre-Program Survey"));
            result.Add(new SkipData("(Kit)"));
            result.Add(new SkipData("Tell Us about You"));

            result.Add(new SkipData("JA Personal Finance"));
            result.Add(new SkipData("Questions about the Program Content"));
            result.Add(new SkipData("Before participating in this program, please try to answer these questions."));
            result.Add(new SkipData("Circle the letter of the response that you think best answers the question."));
            return result;
        }

        public static List<Answer> GetAnswers(List<Question> questions)
        {
            List<Answer> result = new List<Answer>();

            foreach (var q in questions)
            {
                if (q.Answers != null)
                {
                    result.AddRange(q.Answers);
                }
            }

            return result;
        }
    }
}