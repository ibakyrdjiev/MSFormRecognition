using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using System;
using System.IO;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using ComputerVision;
using System.Linq;
using ComputerVision.Models;
using System.Text.RegularExpressions;
using DuoVia.FuzzyStrings;

namespace JaEeCompleteScenario
{
    class Program
    {
        private static List<Question> questions = new List<Question>();
        private static List<ExtractedQuestion> extractedQuestions = new List<ExtractedQuestion>();
        private static TextRecognitionResult documentResult;
        private static List<System.Drawing.Rectangle> predictedRectanglePositions = new List<System.Drawing.Rectangle>();
        public static Queue<ResultLine> resultLinesQueue = new Queue<ResultLine>();
        public static List<string> textLines = new List<string>();

        static void Main(string[] args)
        {
            var predictionResult = MakePrediction();
            ParseDocument();
            ConvertPredictionResults(predictionResult);
            var matchedQuestions = MatchPredictionsWithAnswers();
        }

        private static List<PredictionModel> MakePrediction()
        {
            var predictionKey = "1bce06510cae4a33a3c50ce1d11d5e68";
            var endpointUrl = "https://southcentralus.api.cognitive.microsoft.com/";
            var publishedModelName = "JAEEDemo";
            var projectId = new Guid("8b4cb793-e9e4-4e37-b6d1-30548db1a6ca");

            // <snippet_prediction_endpoint>
            // Create a prediction endpoint, passing in the obtained prediction key
            CustomVisionPredictionClient endpoint = new CustomVisionPredictionClient()
            {
                ApiKey = predictionKey,
                Endpoint = endpointUrl
            };
            // </snippet_prediction_endpoint>

            // <snippet_prediction>
            // Make a prediction against the new project
            Console.WriteLine("Making a prediction:");
            var imageFile = @"../../../Resources/Docs/image.png";//@"C:\Users\Mitko\Documents\MSFormRecognition\ComputerVision\ComputerVision\surveys\survey34-1.jpg";
            using (var stream = File.OpenRead(imageFile))
            {
                var result = endpoint.DetectImage(projectId, publishedModelName, File.OpenRead(imageFile));

                //// Loop over each prediction and write out the results
                //foreach (var c in result.Predictions)
                //{
                //    if (c.Probability > 0.6)
                //    {
                //        Console.WriteLine($"\t{c.TagName}: {c.Probability:P1} [ {c.BoundingBox.Left}, {c.BoundingBox.Top}, {c.BoundingBox.Width}, {c.BoundingBox.Height} ]");
                //    }
                //}

                //Console.ReadKey();
                return result.Predictions
                    .Where(p => p.Probability > 0.6)
                    .ToList();
            }
            //Console.ReadKey();
            // </snippet_prediction>
        }

        private static void ParseDocument()
        {
            var subscriptionKey = "66df573a2f964d3a90f32e038bb0f6de";
            var endpoint = "https://computervisiontestmm.cognitiveservices.azure.com/";
            //var EXTRACT_TEXT_LOCAL_IMAGE = @"C:\Users\iliya.bakyrdjiev\Documents\MSFormRecognition\ComputerVision\ComputerVision\surveys\survey34.pdf";
            var EXTRACT_TEXT_LOCAL_IMAGE = @"../../../Resources/Docs/image.png";//@"C:\Users\Mitko\Documents\MSFormRecognition\ComputerVision\ComputerVision\surveys\survey34-1.jpg";

            questions = Utils.GetFakeQuestions();
            ComputerVisionClient client = Authenticate(endpoint, subscriptionKey);
            BatchReadFileLocal(client, EXTRACT_TEXT_LOCAL_IMAGE).Wait();
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
                documentResult = textRecognitionLocalFileResults.FirstOrDefault();

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
                        textLines.Add(line.Text);
                        resultLinesQueue.Enqueue(current);
                    }
                }

                while (resultLinesQueue.Count != 0)
                {
                    ProccessQueue();
                }

                //System.IO.File.WriteAllLines(@"C:\Users\Mitko\Desktop\trash\test1.txt", textLines);
                //Console.WriteLine();

                //foreach (var question in extractedQuestions)
                //{
                //    Console.WriteLine($"Question: {question.ResultLine.Text}");

                //    foreach (var answer in question.ExtractedAnswers)
                //    {
                //        var resultLine = answer.ResultLine;
                //        var lineTopLeft = $"{{{resultLine.Position.TopLeft.X}, {resultLine.Position.TopLeft.Y}}}";
                //        var lineTopRight = $"{{{resultLine.Position.TopRight.X}, {resultLine.Position.TopRight.Y}}}";
                //        var lineBottomLeft = $"{{{resultLine.Position.BottomLeft.X}, {resultLine.Position.BottomLeft.Y}}}";
                //        var lineBottomRight = $"{{{resultLine.Position.BottomRight.X}, {resultLine.Position.BottomRight.Y}}}";

                //        Console.WriteLine($"Line text: {resultLine.Text}, Line pos: {lineTopLeft}, {lineTopRight}, {lineBottomLeft}, {lineBottomRight}");
                //    }
                //}
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

        private static void ProccessQueue(ResultLine prev = null)
        {
            ResultLine currentItem = null;
            if (prev != null)
            {
                HandleMatch(prev);
            }
            else
            {
                currentItem = resultLinesQueue.Dequeue();
                var match = Regex.Match(currentItem.Text, @"[1-9]+\.\s+.*");
                if (match.Success)
                {
                    string strippedLine = Regex.Replace(currentItem.Text, @"^[0-9]+\. ", string.Empty);
                    currentItem.Text = strippedLine;
                    HandleMatch(currentItem);
                }
            }
        }

        private static void HandleMatch(ResultLine resultLine)
        {
            if (questions.Any(q => q.Text.FuzzyEquals(resultLine.Text))) // "exact" fuzzy match
            {
                var relatedQuestionMatch = questions.Where(q => q.Text.FuzzyEquals(resultLine.Text))
                    .Select(q => new QuestionMatch
                    {
                        Question = q,
                        Match = q.Text.FuzzyMatch(resultLine.Text)
                    }).OrderByDescending(x => x.Match);


                var question = relatedQuestionMatch.First().Question;

                var relatedAnswers = question.Answers;

                var extractedQuestion = new ExtractedQuestion()
                {
                    ResultLine = resultLine,
                    QuestionAnswerType = question.QuestionAnswerType
                };

                extractedQuestions.Add(extractedQuestion);

                switch (question.QuestionAnswerType)
                {
                    case QuestionAnswerType.FreeText:
                        HandleFreeTextQuestion(extractedQuestion);
                        break;

                    case QuestionAnswerType.UnderCheckBox:
                        HandleAnswers(question, relatedAnswers, extractedQuestion);
                        break;

                    default:
                        break;
                }
            }
        }

        private static void HandleFreeTextQuestion(ExtractedQuestion extractedQuestion)
        {
            var answer = resultLinesQueue.Peek();
            extractedQuestion.ExtractedAnswers.Add(new ExtractedAnswer()
            {
                ResultLine = answer
            });
        }

        private static void HandleAnswers(Question question, List<Answer> predefAnswers, ExtractedQuestion extractedQuestion)
        {
            bool haveToSearch = true;
            while (haveToSearch)
            {
                var current = resultLinesQueue.Peek();
                var userInput = current.Text.ToLower();

                if (question.Text.EndsWith(current.Text)) //Todo make this with fuzzy mathc
                {
                    resultLinesQueue.Dequeue();
                    current = resultLinesQueue.Peek();
                    HandleAnswers(question, predefAnswers, extractedQuestion);
                }

                var match = Regex.Match(current.Text, @"^[a-z] ?\.? ?\)?$"); //there is a problem only the a) is captured

                if (match.Success)
                {
                    resultLinesQueue.Dequeue();
                    HandleAnswers(question, predefAnswers, extractedQuestion);
                }

                bool haveToStripAnswer = Regex.Match(userInput, @"^[a-z] ??\. ?s?\)?.*").Success;
                if (haveToStripAnswer)
                {
                    var test = Regex.Replace(current.Text, @"^[a-z] ?\.? ?\)?", string.Empty);
                    userInput = test;
                }

                //todo strip 

                //it will be good to strip this a.) / a. 
                if (predefAnswers.Any(x => userInput.FuzzyEquals(x.Text.ToLower(), 0.60)))
                {
                    var a = new ExtractedAnswer()
                    {
                        ResultLine = current
                    };

                    extractedQuestion.ExtractedAnswers.Add(a);
                    resultLinesQueue.Dequeue();
                }

                else
                {
                    haveToSearch = false;
                }
            }
        }

        private static void ConvertPredictionResults(List<PredictionModel> predictions)
        {
            foreach (var predictionModel in predictions)
            {
                var area = ConvertToPixelAreas(predictionModel);
                predictedRectanglePositions.Add(area);
            }
        }

        private static List<ExtractedQuestion> MatchPredictionsWithAnswers()
        {
            var checkTypeQuestions = extractedQuestions
                .Where(q => q.QuestionAnswerType == QuestionAnswerType.UnderCheckBox)
                .ToList();

            foreach (var question in checkTypeQuestions)
            {
                foreach (var answer in question.ExtractedAnswers)
                {
                    var answerPosition = answer.ResultLine.Position.ReturnAsRectangle();

                    foreach (var predictedPosition in predictedRectanglePositions)
                    {
                        var haveIntersection = predictedPosition.IntersectsWith(answerPosition);

                        if (haveIntersection)
                        {
                            var predictionArea = predictedPosition.Width * predictedPosition.Height;

                            predictedPosition.Intersect(answerPosition);

                            question.IsAnswered = true;
                            answer.IsSelected = true;
                            answer.Coverage = predictedPosition.Width * predictedPosition.Height * 100.0 / predictionArea;
                        }
                    }
                }

                if (question.IsAnswered)
                {
                    var potentialAnswers = question.ExtractedAnswers.Where(a => a.IsSelected);

                    if (potentialAnswers.Count() > 1)
                    {
                        potentialAnswers.OrderByDescending(a => a.Coverage).Skip(1).Select(a => { a.IsSelected = false; return a; }).ToList();
                    }
                }
            }

            return checkTypeQuestions;
        }

        private static System.Drawing.Rectangle ConvertToPixelAreas(PredictionModel predictionModel)
        {
            var box = predictionModel.BoundingBox;

            var left = box.Left * documentResult.Width.Value;
            var top = box.Top * documentResult.Height.Value;
            var width = box.Width * documentResult.Width.Value;
            var heigh = box.Height * documentResult.Height.Value;

            return new System.Drawing.Rectangle((int)left, (int)top, (int)width, (int)heigh);
        }
    }

    public class QuestionMatch
    {
        public Question Question { get; set; }

        public double Match { get; set; }
    }
}
