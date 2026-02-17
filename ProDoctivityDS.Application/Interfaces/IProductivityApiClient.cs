using ProDoctivityDS.Application.Dtos.ProDoctivity;

namespace ProDoctivityDS.Application.Interfaces
{
    public interface IProductivityApiClient
    {
        /// <summary>
        /// Busca documentos aplicando filtros (paginación, tipo, nombre).
        /// </summary>
        Task<(List<ProductivityDocumentDto> Documents, int TotalCount)> GetDocumentsAsync(
            string baseUrl,
            string bearerToken,
            List<string>? documentTypeIds,
            string? name,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene un documento por su ID.
        /// </summary>
        Task<ProductivityDocumentDto> GetDocumentAsync(
            string baseUrl,
            string bearerToken,
            string documentId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene la lista de versiones de un documento.
        /// </summary>
        Task<List<ProductivityVersionDto>> GetDocumentVersionsAsync(
            string baseUrl,
            string bearerToken,
            string documentId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Descarga el PDF de una versión específica.
        /// </summary>
        Task<byte[]> DownloadPdfAsync(
            string baseUrl,
            string bearerToken,
            string versionId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sube un nuevo PDF como una nueva versión del documento.
        /// </summary>
        Task<bool> UploadPdfAsync(
            string baseUrl,
            string bearerToken,
            string fileName,
            byte[] pdfContent,
            string documentTypeId,
            string? parentVersionId = null,
            CancellationToken cancellationToken = default);
    }
}
