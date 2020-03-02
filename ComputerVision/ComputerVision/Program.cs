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
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;

namespace ComputerVisionQuickstart
{
    class Program
    {
        static string subscriptionKey = "66df573a2f964d3a90f32e038bb0f6de";
        static string endpoint = "https://computervisiontestmm.cognitiveservices.azure.com/";
        //private const string EXTRACT_TEXT_LOCAL_IMAGE = @"C:\Users\iliya.bakyrdjiev\Documents\MSFormRecognition\ComputerVision\ComputerVision\surveys\survey34.pdf";
        //private const string EXTRACT_TEXT_LOCAL_IMAGE = @"C:\Users\iliya.bakyrdjiev\Documents\MSFormRecognition\ComputerVision\ComputerVision\surveys\survey34-1.jpg";
       // private const string EXTRACT_TEXT_LOCAL_IMAGE = @"C:\Users\iliya.bakyrdjiev\Documents\MSFormRecognition\ComputerVision\ComputerVision\surveys\survey34.pdf";
        private static List<Question> questions = new List<Question>();
        private static List<ExtractedQuestion> extractedQuestions = new List<ExtractedQuestion>();
        public static Queue<ResultLine> resultLinesQueue = new Queue<ResultLine>();
        public static List<string> textLines = new List<string>();
        public static List<SkipData> skipData = new List<SkipData>();
        private static TextRecognitionResult documentResult;
        private static List<Position> predictedPositions = new List<Position>();
        public static List<ComputerVision.Answer> answers = new List<ComputerVision.Answer>();

        static void Main(string[] args)
        {
            var image = @"C:\Users\iliya.bakyrdjiev\Documents\MSFormRecognition\ComputerVision\ComputerVision\surveys\survey34-1.jpg";
            questions = Utils.GetFakeQuestions();
            skipData = Utils.GetSkippedData();
            answers = GetAnswers(questions);
            ComputerVisionClient client = Authenticate(endpoint, subscriptionKey);
            BatchReadFileLocal(client, image).Wait();
            var predictionResult = MakePrediction(image);
            ConvertPredictionResults(predictionResult);
            var matchedQuestions = MatchPredictionsWithAnswers();
            var freeTextQuestions = extractedQuestions.Where(q => q.QuestionAnswerType == QuestionAnswerType.FreeText).ToList();
            DisplayResults(matchedQuestions, freeTextQuestions);
            Console.WriteLine("End");
            Console.ReadLine();
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
                    var answerPosition = answer.ResultLine.Position;

                    foreach (var predictedPosition in predictedPositions)
                    {
                        var selectArea = (predictedPosition.TopRight.X - predictedPosition.TopLeft.X) * (predictedPosition.BottomLeft.Y - predictedPosition.TopLeft.Y);

                        //I. A is entirely in B
                        if (predictedPosition.TopLeft.X >= answerPosition.TopLeft.X && predictedPosition.TopLeft.Y >= answerPosition.TopLeft.Y
                            &&
                            predictedPosition.TopRight.X <= answerPosition.TopRight.X && predictedPosition.TopRight.Y >= answerPosition.TopRight.Y
                            &&
                            predictedPosition.BottomLeft.X >= answerPosition.BottomLeft.X && predictedPosition.BottomLeft.Y <= answerPosition.BottomLeft.Y
                            &&
                            predictedPosition.BottomRight.X <= answerPosition.BottomRight.X && predictedPosition.BottomRight.Y <= answerPosition.BottomRight.Y
                            )
                        {
                            question.IsAnswered = true;
                            answer.IsSelected = true;
                            answer.Coverage = 1;
                        }

                        //II. A is parthly in B
                        //II.a One poin of A is inside B

                        //If only top left is inside
                        else if (predictedPosition.TopLeft.X >= answerPosition.TopLeft.X && predictedPosition.TopLeft.Y >= answerPosition.TopLeft.Y
                            &&
                            predictedPosition.TopLeft.X < answerPosition.TopRight.X && predictedPosition.TopLeft.Y < answerPosition.BottomRight.Y
                            &&
                            predictedPosition.TopRight.X >= answerPosition.TopRight.X
                            &&
                            predictedPosition.BottomLeft.Y >= answerPosition.BottomLeft.Y
                            )
                        {
                            question.IsAnswered = true;
                            answer.IsSelected = true;
                            answer.Coverage = (answerPosition.TopRight.X - predictedPosition.TopLeft.X) * (answerPosition.BottomLeft.Y - predictedPosition.TopLeft.Y) / selectArea; ;
                        }

                        //If only top right is inside
                        else if (predictedPosition.TopRight.X > answerPosition.TopLeft.X && predictedPosition.TopRight.Y >= answerPosition.TopLeft.Y
                            &&
                            predictedPosition.TopRight.X <= answerPosition.TopRight.X && predictedPosition.TopRight.Y < answerPosition.BottomLeft.Y
                            &&
                            predictedPosition.TopLeft.X <= answerPosition.TopLeft.X
                            &&
                            predictedPosition.BottomRight.Y >= answerPosition.BottomLeft.Y
                            )
                        {
                            question.IsAnswered = true;
                            answer.IsSelected = true;
                            answer.Coverage = (predictedPosition.TopRight.X - answerPosition.TopLeft.X) * (answerPosition.BottomLeft.Y - predictedPosition.TopRight.Y) / selectArea; ;
                        }

                        //If only bottom right is inside
                        else if (predictedPosition.BottomRight.X > answerPosition.TopLeft.X && predictedPosition.BottomRight.Y > answerPosition.TopLeft.Y
                            &&
                            predictedPosition.BottomRight.X <= answerPosition.TopRight.X && predictedPosition.BottomRight.Y <= answerPosition.BottomLeft.Y
                            &&
                            predictedPosition.BottomLeft.X <= answerPosition.TopLeft.X
                            &&
                            predictedPosition.TopRight.Y <= answerPosition.TopLeft.Y
                            )
                        {
                            question.IsAnswered = true;
                            answer.IsSelected = true;
                            answer.Coverage = (predictedPosition.BottomRight.X - answerPosition.TopLeft.X) * (predictedPosition.BottomRight.Y - answerPosition.TopLeft.Y) / selectArea; ;
                        }

                        //If only bottom right is inside
                        else if (predictedPosition.BottomRight.X > answerPosition.TopLeft.X && predictedPosition.BottomRight.Y > answerPosition.TopLeft.Y
                            &&
                            predictedPosition.BottomRight.X <= answerPosition.TopRight.X && predictedPosition.BottomRight.Y <= answerPosition.BottomLeft.Y
                            &&
                            predictedPosition.BottomLeft.X <= answerPosition.TopLeft.X
                            &&
                            predictedPosition.TopRight.Y <= answerPosition.TopLeft.Y
                            )
                        {
                            question.IsAnswered = true;
                            answer.IsSelected = true;
                            answer.Coverage = (predictedPosition.BottomRight.X - answerPosition.TopLeft.X) * (predictedPosition.BottomRight.Y - answerPosition.TopLeft.Y) / selectArea; ;
                        }

                        //If only bottom left is inside
                        else if (predictedPosition.BottomLeft.X >= answerPosition.TopLeft.X && predictedPosition.BottomLeft.Y > answerPosition.TopLeft.Y
                            &&
                            predictedPosition.BottomLeft.X < answerPosition.TopRight.X && predictedPosition.BottomLeft.Y <= answerPosition.BottomLeft.Y
                            &&
                            predictedPosition.BottomRight.X >= answerPosition.TopRight.X
                            &&
                            predictedPosition.TopLeft.Y <= answerPosition.TopLeft.Y
                            )
                        {
                            question.IsAnswered = true;
                            answer.IsSelected = true;
                            answer.Coverage = (answerPosition.TopRight.X - predictedPosition.BottomLeft.X) * (predictedPosition.BottomLeft.Y - answerPosition.TopRight.Y) / selectArea; ;
                        }

                        //II.b Two points of A are inside B
                        //If top right and bottom right are inside
                        else if (predictedPosition.TopRight.X > answerPosition.TopLeft.X && predictedPosition.TopRight.Y >= answerPosition.TopLeft.Y
                            &&
                            predictedPosition.TopRight.X <= answerPosition.TopRight.X
                            &&
                            predictedPosition.BottomRight.Y <= answerPosition.BottomRight.Y
                            &&
                            predictedPosition.TopLeft.X <= answerPosition.TopLeft.X
                            )
                        {
                            question.IsAnswered = true;
                            answer.IsSelected = true;
                            answer.Coverage = (predictedPosition.TopRight.X - answerPosition.TopLeft.X) * (predictedPosition.BottomRight.Y - predictedPosition.TopRight.Y) / selectArea; ;
                        }

                        //If bottom left and bottom right are inside
                        else if (predictedPosition.BottomLeft.X > answerPosition.TopLeft.X && predictedPosition.BottomLeft.Y > answerPosition.TopLeft.Y
                            &&
                            predictedPosition.BottomRight.X <= answerPosition.TopRight.X
                            &&
                            predictedPosition.BottomRight.Y > answerPosition.TopLeft.Y && predictedPosition.BottomRight.Y <= answerPosition.BottomRight.Y
                            &&
                            predictedPosition.TopLeft.Y <= answerPosition.TopLeft.Y
                            )
                        {
                            question.IsAnswered = true;
                            answer.IsSelected = true;
                            answer.Coverage = (predictedPosition.BottomLeft.Y - answerPosition.TopLeft.Y) * (predictedPosition.BottomRight.X - predictedPosition.BottomLeft.X) / selectArea; ;
                        }

                        //If top left and bottom left are inside
                        else if (predictedPosition.TopLeft.X < answerPosition.TopRight.X && predictedPosition.TopLeft.Y >= answerPosition.TopLeft.Y
                            &&
                            predictedPosition.BottomLeft.Y <= answerPosition.TopRight.Y
                            &&
                            predictedPosition.TopRight.X >= answerPosition.TopRight.X
                            )
                        {
                            question.IsAnswered = true;
                            answer.IsSelected = true;
                            answer.Coverage = (answerPosition.TopRight.X - predictedPosition.TopLeft.X) * (predictedPosition.BottomLeft.Y - predictedPosition.TopLeft.Y) / selectArea; ;
                        }

                        //If top left and top right are inside
                        else if (predictedPosition.TopLeft.Y >= answerPosition.TopRight.Y && predictedPosition.TopLeft.Y < answerPosition.BottomLeft.Y
                            &&
                            predictedPosition.TopRight.X <= answerPosition.TopRight.X
                            &&
                            predictedPosition.BottomLeft.Y >= answerPosition.BottomLeft.Y
                            )
                        {
                            question.IsAnswered = true;
                            answer.IsSelected = true;
                            answer.Coverage = (answerPosition.BottomLeft.Y - predictedPosition.TopLeft.Y) * (predictedPosition.TopRight.X - predictedPosition.TopLeft.X) / selectArea; ;
                        }
                        else
                        {
                            ///III. A is outside B
                        }


                    }
                }
            }

            return checkTypeQuestions;

        }

        private static void ConvertPredictionResults(List<PredictionModel> predictions)
        {
            foreach (var predictionModel in predictions)
            {
                var position = ConvertToPixelDimensions(predictionModel);
                predictedPositions.Add(position);
            }
        }

        private static Position ConvertToPixelDimensions(PredictionModel predictionModel)
        {
            var box = predictionModel.BoundingBox;
            var verticalCorrection = 10;

            var position = new Position();
            position.TopLeft = new Point(box.Left * documentResult.Width.Value, box.Top * documentResult.Height.Value + verticalCorrection);
            position.TopRight = new Point(position.TopLeft.X + box.Width * documentResult.Width.Value, position.TopLeft.Y + verticalCorrection);
            position.BottomLeft = new Point(position.TopLeft.X, position.TopLeft.Y + box.Height * documentResult.Height.Value + verticalCorrection);
            position.BottomRight = new Point(position.TopRight.X, position.BottomLeft.Y + verticalCorrection);

            return position;
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

        private static void DisplayResults(List<ExtractedQuestion> matchedQuestions, List<ExtractedQuestion> freeTextQuestions)
        {

            Console.WriteLine("Free text");
            Console.WriteLine("--------------------");
            foreach (var q in freeTextQuestions)
            {
                Console.WriteLine($"Question: {q.ResultLine.Text}");
                Console.WriteLine($"    {q.ExtractedAnswers.FirstOrDefault().ResultLine.Text}");
            }
            Console.WriteLine();

            Console.WriteLine("--------------------");
            Console.WriteLine("--------------------");
            foreach (var q in matchedQuestions)
            {
                Console.WriteLine($"Question: {q.ResultLine.Text}");

                foreach (var a in q.ExtractedAnswers)
                {
                    Console.WriteLine($"    {a.ResultLine.Text} ");
                }

                var selected = q.ExtractedAnswers.OrderByDescending(x => x.Coverage).FirstOrDefault();

                Console.WriteLine($"Selected: {selected.ResultLine.Text}");
            }
            Console.WriteLine("--------------------");
        }

        private static List<PredictionModel> MakePrediction(string imageFile)
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
    }
}
