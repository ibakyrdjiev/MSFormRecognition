namespace TestRecognition.Services
{
    using DuoVia.FuzzyStrings;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using TestRecognition.Core.Services;
    using TestRecognition.Dto;
    using TestRecognition.Dto.Enums;
    using TestRecognition.Dto.TextRecognition;

    public class QuestionMapperService : BaseService, IQuestionMapperService
    {
        //TODO REMOVE THIS AFTER DB CONNECTION - this is only for testing
        private List<Question> questions = new List<Question>();
        public List<string> textLines = new List<string>();
        public List<SkipData> skipData = new List<SkipData>();
        public List<Answer> answers = new List<Answer>();

        public QuestionMapperService(ILogger logger) : base(logger)
        {
            //todo add repo here
            this.questions = Utils.GetFakeQuestions(); 
            this.skipData = Utils.GetSkippedData();
            this.answers = Utils.GetAnswers(questions);
        }

        public List<ExtractedQuestion> MapQuestionsAndAnswers(ScannedPageDto scanneedPage)
        {
            var result = new List<ExtractedQuestion>();
            var queue = new Queue<ScannedLine>(scanneedPage.ScannedLinesQueue);
            ProccessQueue(result, queue);
            return result;
        }

        private void ProccessQueue(List<ExtractedQuestion> result, Queue<ScannedLine> textLineQueue, ExtractedQuestion prev = null)
        {
            var currentItem = textLineQueue.Dequeue();
            var question = HandleMatch(currentItem, prev, result, textLineQueue);
            if (textLineQueue.Any())
            {
                ProccessQueue(result, textLineQueue, question);
            }
        }

        private ExtractedQuestion HandleMatch(ScannedLine resultLine, ExtractedQuestion extractedQuestion, List<ExtractedQuestion> result, Queue<ScannedLine> textLineQueue)
        {
            if (questions.Any(q => q.Text.FuzzyEquals(resultLine.Text))) // "exact" fuzzy match
            {
                var relatedQuestionMatch = questions.Where(q => q.Text.FuzzyEquals(resultLine.Text.ToLower()))
                     .Select(q => new QuestionMatch
                     {
                         Question = q,
                         Match = q.Text.FuzzyMatch(resultLine.Text.ToLower())
                     }).OrderByDescending(x => x.Match);

                var question = relatedQuestionMatch.First().Question;

                extractedQuestion = new ExtractedQuestion()
                {
                    ResultLine = resultLine,
                    QuestionAnswerType = question.QuestionAnswerType,
                    Original = question
                };

                result.Add(extractedQuestion);
            }

            if (extractedQuestion != null)
            {
                //handle invalid junk content
                //when something is found and not related to any question
                //can be added as junk
                //this is specific for each question Type
                //verify is not question or answer and add it junk

                HandleQuestionAnswers(extractedQuestion, textLineQueue);
            }

            if (extractedQuestion == null && IsLineQuestion(resultLine.Text.ToLower()))
            {
                throw new ApplicationException("Answer without question!");
            }

            return extractedQuestion;
        }

        private void HandleQuestionAnswers(ExtractedQuestion question, Queue<ScannedLine> textLineQueue)
        {
            switch (question.QuestionAnswerType)
            {
                case QuestionAnswerType.FreeText:
                    HandleFreeTextQuestion(question, textLineQueue);
                    break;

                case QuestionAnswerType.Vertical:
                    HandleAnswers(question.Original.Answers, question, textLineQueue);
                    break;

                default:
                    break;
            }
        }

        private bool IsLineQuestion(string text)
        {
            //TODO strip answer if nessery
            bool result = answers.Any(q => q.Text.FuzzyEquals(text));
            return result;
        }

        private void HandleFreeTextQuestion(ExtractedQuestion extractedQuestion, Queue<ScannedLine> textLineQueue)
        {
            var answer = textLineQueue.Peek();
            extractedQuestion.ExtractedAnswers.Add(new ExtractedAnswer()
            {
                ResultLine = answer
            });
        }

        private void HandleAnswers(List<Answer> predefAnswers, ExtractedQuestion extractedQuestion, Queue<ScannedLine> textLineQueue)
        {
            bool haveToSearch = true;
            while (haveToSearch)
            {
                if (!textLineQueue.Any())
                {
                    return;
                }
                var current = textLineQueue.Peek();
                var userInput = current.Text.ToLower();

                if (extractedQuestion.Original.Text.EndsWith(current.Text)) //Todo make this with fuzzy match
                {
                    textLineQueue.Dequeue();
                    current = textLineQueue.Peek();
                    HandleAnswers(predefAnswers, extractedQuestion, textLineQueue);
                }

                var match = Regex.Match(current.Text, @"^[a-z] ?\.? ?\)?$"); //there is a problem only the a) a ) a.) a. ) a) is captured

                if (match.Success)
                {
                    textLineQueue.Dequeue();
                    HandleAnswers(predefAnswers, extractedQuestion, textLineQueue);
                }

                bool haveToStripAnswer = Regex.Match(userInput, @"^[a-z] ??\. ?s?\)?.*").Success;
                if (haveToStripAnswer)
                {
                    userInput = Regex.Replace(current.Text, @"^[a-z] ?\.? ?\)?", string.Empty);
                }

                //it will be good to strip this a.) / a.
                //TODO test with 0.75 Default
                if (predefAnswers.Any(x => userInput.FuzzyEquals(x.Text.ToLower(), 0.60)))
                {
                    var answer = new ExtractedAnswer()
                    {
                        ResultLine = current
                    };

                    extractedQuestion.ExtractedAnswers.Add(answer);
                    textLineQueue.Dequeue();
                }
                else
                {
                    haveToSearch = false;
                }
            }
        }
    }
}