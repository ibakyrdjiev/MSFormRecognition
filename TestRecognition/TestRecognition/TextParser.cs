namespace TestRecognition
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Extensions.Logging;
    using System.Threading.Tasks;
    using TestRecognition.Core.Services;
    using TestRecognition.Models;

    public class TextParser
    {
        private readonly IComputerVisionSerivce computerVisionSerivce;
        private readonly IBlobService blobService;

        public TextParser(IComputerVisionSerivce computerVisionSerivce, IBlobService blobService)
        {
            this.computerVisionSerivce = computerVisionSerivce;
            this.blobService = blobService;
        }

        [FunctionName("ParseTextFromImageBlob")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]UploadedBlobModel blobModel,
            ILogger log)
        {
            var stream = await this.blobService.GetBlobStream(blobModel.BlobName);
            var result = await this.computerVisionSerivce.GetBlobTextContent(stream);
            result.BlobName = blobModel.BlobName;
            return new OkObjectResult(result);
        }
    }
}