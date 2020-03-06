namespace TestRecognition.Services
{
    using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
    using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
    using Microsoft.Extensions.Logging;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using TestRecognition.Core.Services;
    using TestRecognition.Dto;
    using TestRecognition.Dto.TextRecognition;

    public class ComputerVisionSerivce : BaseService, IComputerVisionSerivce
    {
        private ComputerVisionClient client;

        public ComputerVisionSerivce(ILogger logger, ComputerVisionClient client) : base(logger)
        {
            this.client = client;
        }

        public async Task<ScannedPageDto> GetBlobTextContent(Stream imageStream)
        {
            const int numberOfCharsInOperationId = 36;
            BatchReadFileInStreamHeaders localFileTextHeaders = await client.BatchReadFileInStreamAsync(imageStream);
            string operationLocation = localFileTextHeaders.OperationLocation;
            string operationId = operationLocation.Substring(operationLocation.Length - numberOfCharsInOperationId);
            int i = 0;
            int maxRetries = 10;
            ReadOperationResult results;
            do
            {
                results = await client.GetReadOperationResultAsync(operationId);
                await Task.Delay(1000);
                if (i == 9)
                {
                    this.Logger.LogError("Server timed out.");
                }
            }
            while ((results.Status == TextOperationStatusCodes.Running ||
                results.Status == TextOperationStatusCodes.NotStarted) && i++ < maxRetries);

            var textRecognitionResult = results.RecognitionResults.First();
            //We are processing only one page this maybe is unessary
            //var result = this.MapResultLines(textRecognitionLocalFileResults);

            var queue = this.CreateScanLineQueue(textRecognitionResult);

            var result = new ScannedPageDto()
            {
                Width = textRecognitionResult.Width.Value,
                Height = textRecognitionResult.Height.Value,
                ScannedLinesQueue = queue
            };

            return result;
        }

        private Queue<ScannedLine> CreateScanLineQueue(TextRecognitionResult textRecognitionResult)
        {
            Queue<ScannedLine> resultQueue = new Queue<ScannedLine>();

            foreach (Line line in textRecognitionResult.Lines)
            {
                var current = new ScannedLine()
                {
                    Text = line.Text,
                    Position = MapPosition(line.BoundingBox, false),
                    Words = line.Words.Select(w => new Dto.Word()
                    {
                        Position = MapPosition(w.BoundingBox, false),
                        Value = w.Text
                    }).ToList()
                };

                resultQueue.Enqueue(current);
            }

            return resultQueue;
        }

        //private Queue<ScannedLine> MapResultLines(IList<TextRecognitionResult> textRecognitionLocalFileResults)
        //{
        //    Queue<ScannedLine> resultQueue = new Queue<ScannedLine>();

        //    foreach (TextRecognitionResult recResult in textRecognitionLocalFileResults)
        //    {
        //        foreach (Line line in recResult.Lines)
        //        {
        //            var current = new ScannedLine()
        //            {
        //                Text = line.Text,
        //                Position = MapPosition(line.BoundingBox, false),
        //                Words = line.Words.Select(w => new Dto.Word()
        //                {
        //                    Position = MapPosition(w.BoundingBox, false),
        //                    Value = w.Text
        //                }).ToList()
        //            };

        //            resultQueue.Enqueue(current);
        //        }
        //    }

        //    return resultQueue;
        //}

        private Position MapPosition(IList<double> boundingBox, bool isPDF)
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

        private Point CreatePoint(double x, double y, bool isPDF = false)
        {
            if (isPDF)
            {
                x = x * 96;
                y = y * 96;
            }

            return new Point(x, y);
        }
    }
}