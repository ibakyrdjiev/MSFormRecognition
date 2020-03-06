namespace TestRecognition.Core.Services
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using TestRecognition.Dto;
    using TestRecognition.Dto.TextRecognition;

    public interface IAnswerRecognitionService
    {
        Task<List<ExtractedQuestion>> GetMatchedQuestions(MappedPageQuestionsDto dto);
    }
}