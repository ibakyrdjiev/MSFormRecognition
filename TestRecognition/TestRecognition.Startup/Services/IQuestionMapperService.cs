namespace TestRecognition.Core.Services
{
    using System.Collections.Generic;
    using TestRecognition.Dto;
    using TestRecognition.Dto.TextRecognition;

    public interface IQuestionMapperService
    {
        List<ExtractedQuestion> MapQuestionsAndAnswers(ScannedPageDto scanneedPage);
    }
}