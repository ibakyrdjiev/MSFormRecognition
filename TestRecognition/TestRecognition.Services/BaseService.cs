namespace TestRecognition.Services
{
    using Microsoft.Extensions.Logging;

    public abstract class BaseService
    {
        private readonly ILogger logger;

        public BaseService(ILogger logger)
        {
            this.logger = logger;
        }

        protected ILogger Logger
        {
            get
            {
                return this.logger;
            }
        }
    }
}