namespace TestRecognition.Services
{
    using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
    using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
    using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using TestRecognition.Core.Services;

    public class AnswerRecognitionService : BaseService, IAnswerRecognitionService
    {
        private readonly CustomVisionPredictionClient customVisionPredictionClient;

        public AnswerRecognitionService(ILogger logger, CustomVisionPredictionClient customVisionPredictionClient) : base(logger)
        {
            this.customVisionPredictionClient = customVisionPredictionClient;
        }

        public async Task<List<PredictionModel>> MakePrediction(Stream imageStream)
        {
            var publishedModelName = "JAEEDemo";
            var projectId = new Guid("8b4cb793-e9e4-4e37-b6d1-30548db1a6ca");

            //URL ?
            var result = await this.customVisionPredictionClient.DetectImageAsync(projectId, publishedModelName, imageStream);

            //ImageUrl imageUrl = new ImageUrl("the path to the image");


            return result.Predictions
                .Where(p => p.Probability > 0.6)
                .ToList();
        }

        private static void ConvertPredictionResults(List<PredictionModel> predictions)
        {
            List<System.Drawing.Rectangle> predictedRectanglePositions = new List<System.Drawing.Rectangle>();
            foreach (var predictionModel in predictions)
            {
                //todo
                //var area = ConvertToPixelAreas(predictionModel);
                //predictedRectanglePositions.Add(area);
            }
        }

        private static System.Drawing.Rectangle ConvertToPixelAreas(PredictionModel predictionModel, TextRecognitionResult documentResult)
        {
            var box = predictionModel.BoundingBox;

            var left = box.Left * documentResult.Width.Value;
            var top = box.Top * documentResult.Height.Value;
            var width = box.Width * documentResult.Width.Value;
            var heigh = box.Height * documentResult.Height.Value;

            return new System.Drawing.Rectangle((int)left, (int)top, (int)width, (int)heigh);
        }

        //private static List<ExtractedQuestion> MatchPredictionsWithAnswers(List<ExtractedQuestion> extractedQuestions)
        //{
        //    var checkTypeQuestions = extractedQuestions
        //        .Where(q => q.QuestionAnswerType == QuestionAnswerType.UnderCheckBox)
        //        .ToList();

        //    foreach (var question in checkTypeQuestions)
        //    {
        //        foreach (var answer in question.ExtractedAnswers)
        //        {
        //            var answerPosition = answer.ResultLine.Position.ReturnAsRectangle();

        //            foreach (var predictedPosition in predictedRectanglePositions)
        //            {
        //                var haveIntersection = predictedPosition.IntersectsWith(answerPosition);

        //                if (haveIntersection)
        //                {
        //                    var predictionArea = predictedPosition.Width * predictedPosition.Height;

        //                    predictedPosition.Intersect(answerPosition);

        //                    question.IsAnswered = true;
        //                    answer.IsSelected = true;
        //                    answer.Coverage = predictedPosition.Width * predictedPosition.Height * 100.0 / predictionArea;
        //                }
        //            }
        //        }

        //        if (question.IsAnswered)
        //        {
        //            var potentialAnswers = question.ExtractedAnswers.Where(a => a.IsSelected);

        //            if (potentialAnswers.Count() > 1)
        //            {
        //                potentialAnswers.OrderByDescending(a => a.Coverage).Skip(1).Select(a => { a.IsSelected = false; return a; }).ToList();
        //            }
        //        }
        //    }

        //    return checkTypeQuestions;
        //}
    }
}