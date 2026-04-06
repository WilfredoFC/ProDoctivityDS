using ProDoctivityDS.Application.Dtos.Request;
using ProDoctivityDS.Application.Dtos.Response;

namespace ProDoctivityDS.Application.Interfaces
{
    public interface ISearchService
    {
        /// <summary>
        /// Ejecuta una búsqueda de documentos en Productivity Cloud aplicando los filtros especificados.
        /// </summary>
        /// <param name="request">Filtros de búsqueda (tipo de documento, nombre, paginación).</param>
        /// <param name="cancellationToken">Token de cancelación.</param>
        /// <returns>Respuesta paginada con los documentos encontrados.</returns>
        Task<SearchDocumentsResponseDto> SearchAllAsync(SearchDocumentsRequestDto request, CancellationToken cancellationToken = default);
        /// <summary>
        /// Ejecuta una búsqueda de documentos en Productivity Cloud aplicando los filtros especificados.
        /// </summary>
        /// <param name="request">Filtros de búsqueda (tipo de documento, nombre, paginación).</param>
        /// <param name="cancellationToken">Token de cancelación.</param>
        /// <returns>Respuesta paginada con los documentos encontrados.</returns>
        Task<SearchDocumentsPOSTResponseDto> SearchAsync(SearchDocumentsPOSTRequestDto request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene un documento específico por su ID (incluye metadatos completos).
        /// </summary>
        /// <param name="documentId">ID del documento.</param>
        /// <param name="cancellationToken">Token de cancelación.</param>
        /// <returns>DTO con la información del documento.</returns>
        Task<DocumentDto> GetDocumentAsync(string documentId, CancellationToken cancellationToken = default);

        Task<string?> GetDocumentIdentityNumberAsync(string documentId, CancellationToken cancellationToken = default);
        Task<string?> GetMatriculaNumberAsync(string documentId, CancellationToken cancellationToken = default);
    }
}
