namespace TestRecognition.Services
{
    using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
    using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
    using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Threading.Tasks;
    using TestRecognition.Core.Services;
    using TestRecognition.Dto;
    using TestRecognition.Dto.Enums;
    using TestRecognition.Dto.TextRecognition;

    public class AnswerRecognitionService : BaseService, IAnswerRecognitionService
    {
        private readonly CustomVisionPredictionClient customVisionPredictionClient;
        private readonly IBlobService blobService;
        private const string publishedModelName = "JAEEDemo";
        private Guid projectId = new Guid("8b4cb793-e9e4-4e37-b6d1-30548db1a6ca");

        public AnswerRecognitionService(ILogger logger, CustomVisionPredictionClient customVisionPredictionClient, IBlobService blobService) : base(logger)
        {
            this.customVisionPredictionClient = customVisionPredictionClient;
            this.blobService = blobService;
        }

        public async Task<List<ExtractedQuestion>> GetMatchedQuestions(MappedPageQuestionsDto dto)
        {
            var predictionResults = await this.MakePrediction(dto.BlobName);
            var predictionRectangleResults = this.ConvertPredictionResults(predictionResults, dto.Width, dto.Height);
            var matchedQuestions = MatchPredictionsWithAnswers(dto.ExtractedQuestions, predictionRectangleResults);

            return matchedQuestions;
        }

        public async Task<List<PredictionModel>> MakePrediction(string blobName)
        {
            var imageStream = await blobService.GetBlobStream(blobName);
            var result = await this.customVisionPredictionClient.DetectImageAsync(projectId, publishedModelName, imageStream);

            return result.Predictions
                .Where(p => p.Probability > 0.6)
                .ToList();
        }

        private List<Rectangle> ConvertPredictionResults(List<PredictionModel> predictions, double documentWidth, double documentHeight)
        {
            List<Rectangle> predictedRectanglePositions = new List<Rectangle>();
            foreach (var predictionModel in predictions)
            {

                var area = ConvertToPixelAreas(predictionModel, documentWidth, documentHeight);
                predictedRectanglePositions.Add(area);
            }

            return predictedRectanglePositions;
        }

        private static Rectangle ConvertToPixelAreas(PredictionModel predictionModel, double documentWidth, double documentHeight)
        {
            var box = predictionModel.BoundingBox;

            var left = box.Left * documentWidth;
            var top = box.Top * documentHeight;
            var width = box.Width * documentWidth;
            var heigh = box.Height * documentHeight;

            return new Rectangle((int)left, (int)top, (int)width, (int)heigh);
        }

        private static List<ExtractedQuestion> MatchPredictionsWithAnswers(List<ExtractedQuestion> extractedQuestions, List<Rectangle> predictedRectanglePositions)
        {
            var checkTypeQuestions = extractedQuestions
                .Where(q => q.QuestionAnswerType == QuestionAnswerType.Vertical)
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
    }
}