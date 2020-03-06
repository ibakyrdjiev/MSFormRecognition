namespace TestRecognition
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using TestRecognition.Dto.TextRecognition;
    using TestRecognition.Core.Services;

    public class QuestionAnswerMapper
    {
        private readonly IQuestionMapperService questionMapperService;

        public QuestionAnswerMapper(IQuestionMapperService questionMapperService)
        {
            this.questionMapperService = questionMapperService;
        }

        [FunctionName("MapQuestionsAndAnswers")]
        public IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]ScannedPageDto scannedPage, HttpRequest req,
            ILogger log)
        {
            var extractedQuestions = this.questionMapperService.MapQuestionsAndAnswers(scannedPage);
            return new OkObjectResult(extractedQuestions);
        }
    }
}
