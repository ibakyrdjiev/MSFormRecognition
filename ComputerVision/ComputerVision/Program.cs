using System;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using ComputerVision;
using System.Linq;
using ComputerVision.Models;
using System.Text.RegularExpressions;
using DuoVia.FuzzyStrings;

namespace ComputerVisionQuickstart
{
    class Program
    {
        static string subscriptionKey = "66df573a2f964d3a90f32e038bb0f6de";
        static string endpoint = "https://computervisiontestmm.cognitiveservices.azure.com/";
        //private const string EXTRACT_TEXT_LOCAL_IMAGE = @"C:\Users\iliya.bakyrdjiev\Documents\MSFormRecognition\ComputerVision\ComputerVision\surveys\survey34.pdf";
        //private const string EXTRACT_TEXT_LOCAL_IMAGE = @"C:\Users\iliya.bakyrdjiev\Documents\MSFormRecognition\ComputerVision\ComputerVision\surveys\survey34-1.jpg";
        private const string EXTRACT_TEXT_LOCAL_IMAGE = @"C:\Users\iliya.bakyrdjiev\Documents\MSFormRecognition\ComputerVision\ComputerVision\surveys\survey34.pdf";
        private static List<Question> questions = new List<Question>();
        private static List<ExtractedQuestion> extractedQuestions = new List<ExtractedQuestion>();
        public static Queue<ResultLine> resultLinesQueue = new Queue<ResultLine>();
        public static List<string> textLines = new List<string>();
        public static List<SkipData> skipData = new List<SkipData>();
        public static List<ComputerVision.Answer> answers = new List<ComputerVision.Answer>();

        static void Main(string[] args)
        {
            questions = Utils.GetFakeQuestions();
            skipData = Utils.GetSkippedData();
            answers = GetAnswers(questions);
            ComputerVisionClient client = Authenticate(endpoint, subscriptionKey);
            BatchReadFileLocal(client, EXTRACT_TEXT_LOCAL_IMAGE).Wait();
        }

        private static List<Answer> GetAnswers(List<Question> questions)
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

        public static ComputerVisionClient Authenticate(string endpoint, string key)
        {
            ComputerVisionClient client =
              new ComputerVisionClient(new ApiKeyServiceClientCredentials(key))
              { Endpoint = endpoint };
            return client;
        }

        private static async Task BatchReadFileLocal(ComputerVisionClient client, string localImage)
        {
            // Helps calucalte starting index to retrieve operation ID
            const int numberOfCharsInOperationId = 36;
            using (Stream imageStream = File.OpenRead(localImage))
            {
                BatchReadFileInStreamHeaders localFileTextHeaders = await client.BatchReadFileInStreamAsync(imageStream);
                string operationLocation = localFileTextHeaders.OperationLocation;
                string operationId = operationLocation.Substring(operationLocation.Length - numberOfCharsInOperationId);
                int i = 0;
                int maxRetries = 10;
                ReadOperationResult results;
                do
                {
                    results = await client.GetReadOperationResultAsync(operationId);
                    Console.WriteLine("Server status: {0}, waiting {1} seconds...", results.Status, i);
                    await Task.Delay(1000);
                    if (i == 9)
                    {
                        Console.WriteLine("Server timed out.");
                    }
                }
                while ((results.Status == TextOperationStatusCodes.Running ||
                    results.Status == TextOperationStatusCodes.NotStarted) && i++ < maxRetries);

                Console.WriteLine();
                List<string> lines = new List<string>();
                var textRecognitionLocalFileResults = results.RecognitionResults;

                foreach (TextRecognitionResult recResult in textRecognitionLocalFileResults)
                {
                    foreach (Line line in recResult.Lines)
                    {
                        var current = new ResultLine()
                        {
                            Text = line.Text,
                            Position = MapPosition(line.BoundingBox, false),
                            Words = line.Words.Select(w => new ComputerVision.Models.Word()
                            {
                                Position = MapPosition(w.BoundingBox, false),
                                Value = w.Text
                            }).ToList()
                        };

                        Console.WriteLine(line.Text);

                        if (!skipData.Any(s => s.Data.ToLower().FuzzyEquals(line.Text.ToLower())))
                        {
                            resultLinesQueue.Enqueue(current);
                        }
                    }
                }

                ProccessQueue();

                System.IO.File.WriteAllLines(@"C:\Users\iliya.bakyrdjiev\Desktop\trash\test1.txt", textLines);
                Console.WriteLine();
            }
        }

        private static Position MapPosition(IList<double> boundingBox, bool isPDF)
        {
            var result = new Position()
            {
                TopLeft = CreatePoint(boundingBox[0], boundingBox[1], isPDF),
                TopRight = CreatePoint(boundingBox[2], boundingBox[3], isPDF),
                BottomRight = CreatePoint(boundingBox[4], boundingBox[5], isPDF),
                BottomLeft = CreatePoint(boundingBox[6], boundingBox[7], isPDF),
            };

            return result;
        }

        private static Point CreatePoint(double x, double y, bool isPDF = false)
        {
            if (isPDF)
            {
                x = x * 96;
                y = y * 96;
            }

            return new Point(x, y);
        }

        private static void ProccessQueue(ExtractedQuestion prev = null)
        {
            var currentItem = resultLinesQueue.Dequeue();
            var question = HandleMatch(currentItem, prev);
            if (resultLinesQueue.Any())
            {
                ProccessQueue(question);
            }
        }

        private static ExtractedQuestion HandleMatch(ResultLine resultLine, ExtractedQuestion extractedQuestion)
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

                extractedQuestions.Add(extractedQuestion);
            }

            if (extractedQuestion != null)
            {
                //handle invalin junk content
                //when something is found and not related to any question 
                //can be added as junk 
                //this is specific for each question Type
                //verify is not question or answer and add it junk

                HandleQuestionAnswers(extractedQuestion);
            }

            if (extractedQuestion == null && IsLineQuestion(resultLine.Text.ToLower()))
            {
                throw new ApplicationException("Answer without question!");
            }

            return extractedQuestion;
        }

        private static void HandleQuestionAnswers(ExtractedQuestion question)
        {
            switch (question.QuestionAnswerType)
            {
                case QuestionAnswerType.FreeText:
                    HandleFreeTextQuestion(question);
                    break;

                case QuestionAnswerType.UnderCheckBox:
                    HandleAnswers(question.Original.Answers, question);
                    break;

                default:
                    break;
            }
        }

        private static bool IsLineQuestion(string text)
        {
            //TODO strip answer if nessery 
            bool result = answers.Any(q => q.Text.FuzzyEquals(text));
            return result;
        }

        private static void HandleFreeTextQuestion(ExtractedQuestion extractedQuestion)
        {
            var answer = resultLinesQueue.Peek();
            extractedQuestion.ExtractedAnswers.Add(new ExtractedAnswer()
            {
                ResultLine = answer
            });
        }

        private static void HandleAnswers(List<Answer> predefAnswers, ExtractedQuestion extractedQuestion)
        {
            bool haveToSearch = true;
            while (haveToSearch)
            {
                if (!resultLinesQueue.Any())
                {
                    return;
                }
                var current = resultLinesQueue.Peek();
                var userInput = current.Text.ToLower();

                if (extractedQuestion.Original.Text.EndsWith(current.Text)) //Todo make this with fuzzy match
                {
                    resultLinesQueue.Dequeue();
                    current = resultLinesQueue.Peek();
                    HandleAnswers(predefAnswers, extractedQuestion);
                }

                var match = Regex.Match(current.Text, @"^[a-z] ?\.? ?\)?$"); //there is a problem only the a) a ) a.) a. ) a) is captured

                if (match.Success)
                {
                    resultLinesQueue.Dequeue();
                    HandleAnswers(predefAnswers, extractedQuestion);
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
                    resultLinesQueue.Dequeue();
                }

                else
                {
                    haveToSearch = false;
                }
            }
        }
    }
}
