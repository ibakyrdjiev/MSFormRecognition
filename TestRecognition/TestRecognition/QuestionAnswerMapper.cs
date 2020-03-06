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
    using TestRecognition.Models;

    public class QuestionAnswerMapper
    {
        private readonly IQuestionMapperService questionMapperService;
            
        public QuestionAnswerMapper(IQuestionMapperService questionMapperService)
        {
            this.questionMapperService = questionMapperService;
        }

        [FunctionName("MapQuestionsAndAnswers")]
        public  async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]MappedQuestionsModel mappedQuestionsModel, HttpRequest req,
            ILogger log)
        {
            //var extractedQuestions = this.questionMapperService.MapQuestionsAndAnswers(scannedPageDto);
            //var result = new MappedPageQuestionsDto()
            //{
            //    ExtractedQuestions = extractedQuestions,
            //    Height = scannedPageDto.Height,
            //    Width = scannedPageDto.Height
            //};

            return new OkObjectResult(mappedQuestionsModel);
        }
    }
}
