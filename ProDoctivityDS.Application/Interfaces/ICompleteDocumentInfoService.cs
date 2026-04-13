using ProDoctivityDS.Application.Dtos.Response;

namespace ProDoctivityDS.Application.Interfaces
{
    public interface ICompleteDocumentInfoService
    {
        Task<DocumentsByCedulaResponseDto> GetDocumentsByCedulaAsync(string cedula, CancellationToken cancellationToken);
        Task<CompleteDocumentInfoResponseDto> CompleteMissingDocumentsAsync(string cedula, CancellationToken cancellationToken);
    }
}
