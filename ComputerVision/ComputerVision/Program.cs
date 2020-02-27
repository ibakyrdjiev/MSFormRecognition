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
        public static Queue<string> textQueue = new Queue<string>();
        public static List<string> questionsTest = new List<string>();
        public static Queue<ResultLine> resultLinesQueue = new Queue<ResultLine>();

        static void Main(string[] args)
        {
            PopulateFakeData();
            Console.WriteLine("Azure Cognitive Services Computer Vision - .NET quickstart example");
            Console.WriteLine();
            ComputerVisionClient client = Authenticate(endpoint, subscriptionKey);

            BatchReadFileLocal(client, EXTRACT_TEXT_LOCAL_IMAGE).Wait();
            Console.WriteLine("----------------------------------------------------------");
            Console.WriteLine();
            Console.WriteLine("Computer Vision quickstart is complete.");
            Console.WriteLine();
            Console.WriteLine("Press enter to exit...");
            Console.WriteLine();
        }

        private static void PopulateFakeData()
        {
            var first = new Question();
            first.Answers = new List<Answer>();
            first.Text = "Of the following steps, which would be the first step in financial planning?";
            first.Answers.Add(new Answer() { Text = "Get a job so you can start earning money." });
            first.Answers.Add(new Answer() { Text = "Determine your needs and goals for the future." });
            first.Answers.Add(new Answer() { Text = "Starting looking for a home to buy." });
            first.Answers.Add(new Answer() { Text = " Save a portion of your money for the future." });
            questions.Add(first);

            var second = new Question();
            second.Answers = new List<Answer>();
            second.Text = "The best predictor of how much money you will make in the future is the you choose.";
            second.Answers.Add(new Answer() { Text = "skills" });
            second.Answers.Add(new Answer() { Text = "connections" });
            second.Answers.Add(new Answer() { Text = "education" });
            second.Answers.Add(new Answer() { Text = "all of the above" });
            questions.Add(second);

            var third = new Question()
            {
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
        }

        public static ComputerVisionClient Authenticate(string endpoint, string key)
        {
            ComputerVisionClient client =
              new ComputerVisionClient(new ApiKeyServiceClientCredentials(key))
              { Endpoint = endpoint };
            return client;
        }

        public static async Task BatchReadFileLocal(ComputerVisionClient client, string localImage)
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
                // Read the text from the local image
                BatchReadFileInStreamHeaders localFileTextHeaders = await client.BatchReadFileInStreamAsync(imageStream);
                // Get the operation location (operation ID)
                string operationLocation = localFileTextHeaders.OperationLocation;

                // Retrieve the URI where the recognized text will be stored from the Operation-Location header.
                string operationId = operationLocation.Substring(operationLocation.Length - numberOfCharsInOperationId);

                // Extract text, wait for it to complete.
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

                // Display the found text.
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
                        textQueue.Enqueue(line.Text);
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
                questionsTest.Add(resultLine.Text);
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
