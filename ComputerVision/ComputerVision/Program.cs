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

namespace ComputerVisionQuickstart
{
    class Program
    {
        static string subscriptionKey = "66df573a2f964d3a90f32e038bb0f6de";
        static string endpoint = "https://computervisiontestmm.cognitiveservices.azure.com/";
        private const string EXTRACT_TEXT_LOCAL_IMAGE = @"C:\Users\iliya.bakyrdjiev\Documents\MSFormRecognition\ComputerVision\ComputerVision\surveys\survey34Clean.pdf";
        private static List<Question> questions = new List<Question>();
        private static List<ExtractedQuestion> extractedQuestions = new List<ExtractedQuestion>();
        public static Queue<ResultLine> resultLinesQueue = new Queue<ResultLine>();

        static void Main(string[] args)
        {
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
            Console.WriteLine("----------------------------------------------------------");
            Console.WriteLine("BATCH READ FILE - LOCAL IMAGE");
            Console.WriteLine();

            // Helps calucalte starting index to retrieve operation ID
            const int numberOfCharsInOperationId = 36;

            Console.WriteLine($"Extracting text from local image {Path.GetFileName(localImage)}...");
            Console.WriteLine();
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
                            Position = MapPosition(line.BoundingBox, true),
                            Words = line.Words.Select(w => new ComputerVision.Models.Word()
                            {
                                Position = MapPosition(w.BoundingBox, true),
                                Value = w.Text
                            }).ToList()
                        };

                        resultLinesQueue.Enqueue(current);
                    }
                }

                while (resultLinesQueue.Count != 0)
                {
                    ProccessQueue();
                }

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
            if (questions.Any(q => q.Text == resultLine.Text)) // exact match
            {
                var q = new ExtractedQuestion()
                {
                    ResultLine = resultLine
                };

                extractedQuestions.Add(q);
                var ans = questions.Where(q => q.Text == resultLine.Text).FirstOrDefault().Answers;
                HandleAnswers(q, ans);
            }
            else if (questions.Any(q => q.Text.Contains(resultLine.Text)))
            {
                var nextItem = resultLinesQueue.Dequeue();
                var concat = resultLine.Text + " " + nextItem.Text;

                resultLine.Words.AddRange(nextItem.Words);
                var item4Send = new ResultLine()
                {
                    Text = concat,
                    Position = resultLine.Position,
                    Words = resultLine.Words
                };

                ProccessQueue(item4Send);
            }
        }

        private static void HandleAnswers(ExtractedQuestion question, List<Answer> predefAnswers)
        {
            bool haveToSearch = true;
            while (haveToSearch)
            {
                var current = resultLinesQueue.Peek();

                if (predefAnswers.Any(x => current.Text.Contains(x.Text)))
                {
                    var a = new ExtractedAnswer()
                    {
                        ResultLine = current
                    };

                    question.ExtractedAnswers.Add(a);
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
