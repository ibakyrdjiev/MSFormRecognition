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
    using TestRecognition.Extensions;

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
            var originalQueue = scanneedPage.ScannedLinesQueue.ToList();

            //ProccessQueue(result, queue);
            var results = ProccessOriginalQueue(originalQueue);
            return results;
        }

        private List<ExtractedQuestion> ProccessOriginalQueue(List<ScannedLine> originalQueue)
        {
            var results = new List<ExtractedQuestion>();
            ExtractedQuestion extractedQuestion;
            var currentLineNumber = 0;

            foreach (var originalLine in originalQueue)
            {
                var relatedQuestions = questions
                        .Where(q => q.Text.FuzzyEquals(originalLine.Text.ToLower(), 0.9))
                        .ToList();

                //Try to match the line with the predefined question
                if (relatedQuestions.Count > 0) // "exact" fuzzy match
                {
                    var relatedQuestion = relatedQuestions
                        .Select(q => new QuestionMatch
                        {
                            Question = q,
                            Match = q.Text.FuzzyMatch(originalLine.Text.ToLower())
                        }).OrderByDescending(x => x.Match)
                         .First()
                         .Question;

                    //Call appropriate method according to question type and add result to results collection
                    switch (relatedQuestion.QuestionAnswerType)
                    {
                        case QuestionAnswerType.Vertical:
                        case QuestionAnswerType.Inline:
                            extractedQuestion = ExtractQuestion(relatedQuestion, originalLine, originalQueue, currentLineNumber);
                            break;

                        case QuestionAnswerType.Table:
                            extractedQuestion = ExtractTableQuestion(relatedQuestion, originalLine, originalQueue);
                            break;
                        case QuestionAnswerType.Nested:
                            extractedQuestion = ExtractNestedQuestion(relatedQuestion, originalLine, originalQueue);
                            break;
                        case QuestionAnswerType.NameIdentity:
                            extractedQuestion = ExtractNameIdentityQuestion(relatedQuestion, originalLine, originalQueue);
                            break;
                        case QuestionAnswerType.BirthIdentity:
                            extractedQuestion = ExtractBirthIdentityQuestion(relatedQuestion, originalLine, originalQueue);
                            break;
                        case QuestionAnswerType.FreeText:
                            extractedQuestion = ExtractFreeTextQuestion(relatedQuestion, originalLine, originalQueue);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException("Question type not defined");
                    }

                    //Mark line as "Useful" - this will allow us to collect all lines that are not "Useful" into trash later
                    originalLine.IsUseful = true;

                    if (extractedQuestion != null)
                    {
                        results.Add(extractedQuestion);
                    }
                }

                currentLineNumber++;
            }

            //Collect all original lines that are not "Useful" in trash
            //TODO utilize trash somehow
            var trash = originalQueue
                .Where(q => q.IsUseful == false)
                .ToList();

            return results;
        }

        private ExtractedQuestion ExtractQuestion(Question relatedQuestion, ScannedLine originalLine, List<ScannedLine> originalQueue, int currentLineNumber)
        {
            var extractedQuestion = new ExtractedQuestion()
            {
                ResultLine = originalLine,
                QuestionAnswerType = relatedQuestion.QuestionAnswerType,
                Original = relatedQuestion
            };

            //Get answers that belong to the question
            var answers = relatedQuestion.Answers;

            //Collect all the answers and get coordinates
            //Search forward
            foreach (var predefinedAnswer in answers)
            {
                var answerIsNotFound = true;
                var currentAnswerNumber = currentLineNumber + 1;

                while (answerIsNotFound)
                {
                    var potencialAnswer = originalQueue[currentAnswerNumber].Text.ToLower().FuzzyEquals(predefinedAnswer.Text.ToLower())
                        ? originalQueue[currentAnswerNumber]
                        : null;

                    if (potencialAnswer != null)
                    {
                        answerIsNotFound = false;
                        extractedQuestion.ExtractedAnswers.Add(new ExtractedAnswer
                        {
                            ResultLine = new ScannedLine
                            {
                                Text = potencialAnswer.Text,
                                Position = new Position
                                {
                                    TopLeft = potencialAnswer.Position.TopLeft,
                                    TopRight = potencialAnswer.Position.TopRight,
                                    BottomLeft = potencialAnswer.Position.BottomLeft,
                                    BottomRight = potencialAnswer.Position.BottomRight,
                                }
                            }
                        });
                    }

                    if (questions.Any(q => q.Text.FuzzyEquals(originalQueue[currentAnswerNumber].Text.ToLower()))) // if we've already reached the answer before current
                    {
                        answerIsNotFound = false;
                    }

                    currentAnswerNumber++;
                }
            }

            if (extractedQuestion.ExtractedAnswers.Count == 0)
            {
                //Search in words
                foreach (var predefinedAnswer in answers)
                {
                    var answerIsNotFound = true;
                    var currentAnswerNumber = currentLineNumber + 1;

                    while (answerIsNotFound)
                    {
                        var answer = originalQueue[currentAnswerNumber];

                        if (questions.Any(q => q.Text.FuzzyEquals(answer.Text.ToLower()))) // if we've already reached the answer after current
                        {
                            answerIsNotFound = false;
                            continue;
                        }

                        for (int i = 0; i < answer.Words.Count; i++)
                        {
                            //First check in single predefined answer
                            var potencialAnswerInWord = answer.Words[i].Value.ToLower().FuzzyEquals(predefinedAnswer.Text.ToLower())
                            ? answer.Words[i]
                            : null;

                            //If not matched, check in the collection of predefined possible answers
                            if (potencialAnswerInWord == null)
                            {
                                potencialAnswerInWord = answer.Words[i].Value.FuzzyEqualsCollection(predefinedAnswer.TextVariations)
                                ? answer.Words[i]
                                : null;
                            }

                            if (potencialAnswerInWord != null)
                            {
                                answerIsNotFound = false;
                                extractedQuestion.ExtractedAnswers.Add(new ExtractedAnswer
                                {
                                    ResultLine = new ScannedLine
                                    {
                                        Text = potencialAnswerInWord.Value,
                                        Position = new Position
                                        {
                                            TopLeft = potencialAnswerInWord.Position.TopLeft,
                                            TopRight = potencialAnswerInWord.Position.TopRight,
                                            BottomLeft = potencialAnswerInWord.Position.BottomLeft,
                                            BottomRight = potencialAnswerInWord.Position.BottomRight,
                                        }
                                    }
                                });

                                break;
                            }
                        }


                        currentAnswerNumber++;
                    }
                }
            }

            if (extractedQuestion.ExtractedAnswers.Count == 0)
            {
                //Search backward
                foreach (var predefinedAnswer in answers)
                {
                    var answerIsNotFound = true;
                    var currentAnswerNumber = currentLineNumber - 1;

                    while (answerIsNotFound)
                    {
                        var potencialAnswer = originalQueue[currentAnswerNumber].Text.ToLower().FuzzyEquals(predefinedAnswer.Text.ToLower())
                            ? originalQueue[currentAnswerNumber]
                            : null;

                        if (potencialAnswer != null)
                        {
                            answerIsNotFound = false;
                            extractedQuestion.ExtractedAnswers.Add(new ExtractedAnswer
                            {
                                ResultLine = new ScannedLine
                                {
                                    Text = potencialAnswer.Text,
                                    Position = new Position
                                    {
                                        TopLeft = potencialAnswer.Position.TopLeft,
                                        TopRight = potencialAnswer.Position.TopRight,
                                        BottomLeft = potencialAnswer.Position.BottomLeft,
                                        BottomRight = potencialAnswer.Position.BottomRight,
                                    }
                                }
                            });
                        }

                        if (questions.Any(q => q.Text.FuzzyEquals(originalQueue[currentAnswerNumber].Text.ToLower()))) // if we've already reached the answer before current
                        {
                            answerIsNotFound = false;
                        }

                        currentAnswerNumber--;
                    }
                }
            }

            //Find answers
            //Fill in extrtacted question and answers


            return extractedQuestion;
        }

        private ExtractedQuestion ExtractFreeTextQuestion(Question relatedQuestion, ScannedLine originalLine, List<ScannedLine> originalQueue)
        {
            return null;
        }

        private ExtractedQuestion ExtractBirthIdentityQuestion(Question relatedQuestion, ScannedLine originalLine, List<ScannedLine> originalQueue)
        {
            return null;
        }

        private ExtractedQuestion ExtractNameIdentityQuestion(Question relatedQuestion, ScannedLine originalLine, List<ScannedLine> originalQueue)
        {
            return null;
        }

        private ExtractedQuestion ExtractNestedQuestion(Question relatedQuestion, ScannedLine originalLine, List<ScannedLine> originalQueue)
        {
            return null;
        }

        private ExtractedQuestion ExtractTableQuestion(Question relatedQuestion, ScannedLine originalLine, List<ScannedLine> originalQueue)
        {
            return null;
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