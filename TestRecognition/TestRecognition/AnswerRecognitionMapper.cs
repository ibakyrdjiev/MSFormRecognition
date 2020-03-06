namespace TestRecognition
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Extensions.Logging;
    using System.Threading.Tasks;
    using TestRecognition.Core.Services;
    using TestRecognition.Dto.TextRecognition;

    public class AnswerRecognitionMapper
    {
        private readonly IAnswerRecognitionService answerRecognitionService;

        public AnswerRecognitionMapper(IAnswerRecognitionService answerRecognitionService)
        {
            this.answerRecognitionService = answerRecognitionService;
        }

        [FunctionName("MapAnsweredQuestions")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]MappedPageQuestionsDto dto, HttpRequest req,
            ILogger log)
        {
            var result = await this.answerRecognitionService.GetMatchedQuestions(dto);
            return new OkObjectResult(result);
        }
    }
}