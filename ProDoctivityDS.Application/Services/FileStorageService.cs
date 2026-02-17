using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProDoctivityDS.Application.Dtos.ValueObjects;
using ProDoctivityDS.Application.Interfaces;

namespace ProDoctivityDS.Application.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly ILogger<FileStorageService> _logger;
        private readonly string _basePath;

        public FileStorageService(ILogger<FileStorageService> logger, IOptions<FileStorageSettingsDto> settings)
        {
            _logger = logger;
            _basePath = Path.GetFullPath(settings.Value.BasePath);
            EnsureDirectoryExists(_basePath);
        }

        /// <inheritdoc />
        public Task<string> SaveFileAsync(byte[] content, string fileName, string subFolder, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                try
                {
                    // Sanitizar nombre de archivo
                    var safeFileName = SanitizeFileName(fileName);
                    var uniqueFileName = MakeUniqueFileName(safeFileName);

                    var folderPath = Path.Combine(_basePath, subFolder);
                    EnsureDirectoryExists(folderPath);

                    var filePath = Path.Combine(folderPath, uniqueFileName);
                    File.WriteAllBytes(filePath, content);

                    _logger.LogInformation("Archivo guardado: {FilePath}", filePath);
                    return filePath;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al guardar archivo {FileName} en {SubFolder}", fileName, subFolder);
                    throw;
                }
            }, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<string> SaveOriginalFileAsync(byte[] content, string documentId, string fileName, CancellationToken cancellationToken = default)
        {
            // Formato: {documentId}_original_{timestamp}_{fileName}
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var safeName = $"{documentId}_original_{timestamp}_{SanitizeFileName(fileName)}";
            return await SaveFileAsync(content, safeName, "Originals", cancellationToken);
        }

        /// <inheritdoc />
        public async Task<string> SaveProcessedFileAsync(byte[] content, string documentId, string fileName, CancellationToken cancellationToken = default)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var safeName = $"{documentId}_processed_{timestamp}_{SanitizeFileName(fileName)}";
            return await SaveFileAsync(content, safeName, "Processed", cancellationToken);
        }

        /// <inheritdoc />
        public string GetStoragePath(string subFolder = "")
        {
            if (string.IsNullOrEmpty(subFolder))
                return _basePath;

            var path = Path.Combine(_basePath, subFolder);
            EnsureDirectoryExists(path);
            return path;
        }

        /// <inheritdoc />
        public void OpenFolder(string subFolder = "")
        {
            try
            {
                var path = GetStoragePath(subFolder);
                if (Directory.Exists(path))
                {
                    // Solo Windows
                    System.Diagnostics.Process.Start("explorer.exe", path);
                    _logger.LogInformation("Carpeta abierta: {Path}", path);
                }
                else
                {
                    _logger.LogWarning("No se puede abrir la carpeta porque no existe: {Path}", path);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al abrir la carpeta {SubFolder}", subFolder);
            }
        }

        // ---------- Métodos auxiliares privados ----------

        private void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "unnamed.pdf";

            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));

            // Si después de sanitizar queda vacío, usar un nombre por defecto
            if (string.IsNullOrWhiteSpace(sanitized))
                sanitized = "document.pdf";

            return sanitized;
        }

        private string MakeUniqueFileName(string fileName)
        {
            var directory = Path.GetDirectoryName(fileName) ?? "";
            var name = Path.GetFileNameWithoutExtension(fileName);
            var ext = Path.GetExtension(fileName);

            if (string.IsNullOrEmpty(directory))
                directory = _basePath;

            var fullPath = Path.Combine(directory, fileName);
            var counter = 1;

            while (File.Exists(fullPath))
            {
                var newName = $"{name}_{counter}{ext}";
                fullPath = Path.Combine(directory, newName);
                counter++;
            }

            return Path.GetFileName(fullPath);
        }
    }
}