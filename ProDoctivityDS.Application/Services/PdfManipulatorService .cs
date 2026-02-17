using Microsoft.Extensions.Logging;
using PdfSharp.Pdf.IO;
using ProDoctivityDS.Application.Interfaces;

namespace ProDoctivityDS.Application.Services
{
    public class PdfManipulatorService : IPdfManipulator
    {
        private readonly ILogger<PdfManipulatorService> _logger;

        public PdfManipulatorService(ILogger<PdfManipulatorService> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<byte[]> RemoveFirstPageAsync(byte[] pdfBytes, CancellationToken cancellationToken = default)
        {
            // PDFsharp no es asíncrono; lo ejecutamos en un hilo de pool para no bloquear
            return await Task.Run(() =>
            {
                if (pdfBytes == null || pdfBytes.Length == 0)
                {
                    _logger.LogWarning("El PDF está vacío o es nulo");
                    return pdfBytes ?? Array.Empty<byte>();
                }

                try
                {
                    using var inputStream = new MemoryStream(pdfBytes);
                    using var document = PdfReader.Open(inputStream, PdfDocumentOpenMode.Modify);

                    // Si el documento no tiene páginas o solo una, no hacemos nada
                    if (document.Pages.Count <= 1)
                    {
                        _logger.LogDebug("El PDF tiene {PageCount} páginas, no se elimina ninguna", document.Pages.Count);
                        return pdfBytes;
                    }

                    // Eliminar la primera página (índice 0)
                    document.Pages.RemoveAt(0);
                    _logger.LogDebug("Primera página eliminada. Páginas restantes: {PageCount}", document.Pages.Count);

                    using var outputStream = new MemoryStream();
                    document.Save(outputStream, false);
                    return outputStream.ToArray();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al eliminar la primera página del PDF");
                    // En caso de error, retornamos el PDF original (comportamiento del WinForms original)
                    return pdfBytes;
                }
            }, cancellationToken);
        }
    }
}

