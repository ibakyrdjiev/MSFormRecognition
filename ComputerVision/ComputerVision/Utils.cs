using System.Collections.Generic;

namespace ComputerVision
{
    public static class Utils
    {
        public static List<Question> GetFakeQuestions()
        {
            var questions = new List<Question>();

            var first = new Question();
            first.QuestionAnswerType = QuestionAnswerType.UnderCheckBox;
            first.Answers = new List<Answer>();
            first.Text = "Of the following steps, which would be the first step in financial planning?";
            first.Answers.Add(new Answer() { Text = "Get a job so you can start earning money." });
            first.Answers.Add(new Answer() { Text = "Determine your needs and goals for the future." });
            first.Answers.Add(new Answer() { Text = "Starting looking for a home to buy." });
            first.Answers.Add(new Answer() { Text = " Save a portion of your money for the future." });
            questions.Add(first);

            var second = new Question();
            second.QuestionAnswerType = QuestionAnswerType.UnderCheckBox;
            second.Answers = new List<Answer>();
            second.Text = "The best predictor of how much money you will make in the future is the you choose.";
            second.Answers.Add(new Answer() { Text = "skills" });
            second.Answers.Add(new Answer() { Text = "connections" });
            second.Answers.Add(new Answer() { Text = "education" });
            second.Answers.Add(new Answer() { Text = "all of the above" });
            questions.Add(second);

            var third = new Question()
            {
                QuestionAnswerType = QuestionAnswerType.UnderCheckBox,
                Text = "Which of the following should you remember when developing a savings plan?",
                Answers = new List<Answer>()
                {
                    new Answer()
                    {
                        Text = "Wait until you are 40 years old before saving."
                    },
                    new Answer()
                    {
                        Text = "Pay yourself first."
                    },
                    new Answer()
                    {
                        Text = "Pay off your low-interest debt first."
                    },
                    new Answer()
                    {
                        Text = "Have only high-risk investments."
                    }
                }
            };
            questions.Add(third);

            var fourth = new Question()
            {
                QuestionAnswerType = QuestionAnswerType.UnderCheckBox,
                Text = "All of the following are elements of financial planning except one. Which one is NOT?",
                Answers = new List<Answer>()
                {
                    new Answer()
                    {
                        Text = "Earn money"
                    },
                    new Answer()
                    {
                        Text = "Save money"
                    },
                    new Answer()
                    {
                        Text = "Never use credit"
                    },
                    new Answer()
                    {
                        Text = "Spend money wisely"
                    }
                }
            };
            questions.Add(fourth);

            var fith = new Question()
            {
                QuestionAnswerType = QuestionAnswerType.UnderCheckBox,
                Text = "Alan has created a new budget. Which of the following should he NOT do?",
                Answers = new List<Answer>()
                {
                    new Answer()
                    {
                        Text = "Have a spending plan."
                    },
                    new Answer()
                    {
                        Text = "Spend less than he earns."
                    },
                    new Answer()
                    {
                        Text = "Use credit for all items not in his budget."
                    },
                    new Answer()
                    {
                        Text = "Stick to his budget"
                    }
                }
            };
            questions.Add(fith);

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
    }
}
