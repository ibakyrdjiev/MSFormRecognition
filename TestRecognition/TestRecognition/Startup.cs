[assembly: Microsoft.Azure.Functions.Extensions.DependencyInjection.FunctionsStartup(typeof(TestRecognition.Startup))]

namespace TestRecognition
{
    using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
    using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
    using Microsoft.Azure.Functions.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using TestRecognition.Core.Services;
    using TestRecognition.Services;

    //TODO ADD magic strings for auth to the json config file
    //TODO integrate with Vault
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddTransient<IComputerVisionSerivce, ComputerVisionSerivce>();
            builder.Services.AddTransient<IAnswerRecognitionService, AnswerRecognitionService>();
            builder.Services.AddTransient<IBlobService, BlobService>();
            builder.Services.AddTransient<IQuestionMapperService, QuestionMapperService>();

            builder.Services.AddSingleton(this.GetCloudBlobClient());
            builder.Services.AddSingleton(this.GetComputerVisionClient());
            builder.Services.AddSingleton(this.CetCustomVisionClient());
        }

        private CloudBlobClient GetCloudBlobClient()
        {
            string strorageconn = "DefaultEndpointsProtocol=https;AccountName=demostorageiliya;AccountKey=ui6WqQv+oj+CKZ0rfjfk97T0YgZIArJaJG4RI6UmewHMcN1oZJOVYUsvrele4edDyVipfwAnS9acry0oC5PdfA==;EndpointSuffix=core.windows.net";
            CloudStorageAccount storageacc = CloudStorageAccount.Parse(strorageconn);
            CloudBlobClient blobClient = storageacc.CreateCloudBlobClient();
            return blobClient;
        }

        private ComputerVisionClient GetComputerVisionClient()
        {
            string endpoint = "https://computervisiontestmm.cognitiveservices.azure.com/";
            string subscriptionKey = "66df573a2f964d3a90f32e038bb0f6de";
            ComputerVisionClient client =
                   new ComputerVisionClient(new ApiKeyServiceClientCredentials(subscriptionKey))
                   { Endpoint = endpoint };

            return client;
        }

        private CustomVisionPredictionClient CetCustomVisionClient()
        {
            var predictionKey = "1bce06510cae4a33a3c50ce1d11d5e68";
            var endpointUrl = "https://southcentralus.api.cognitive.microsoft.com/";

            return new CustomVisionPredictionClient()
            {
                ApiKey = predictionKey,
                Endpoint = endpointUrl
            };
        }
    }
}