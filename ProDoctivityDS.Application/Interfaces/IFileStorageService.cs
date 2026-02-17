namespace ProDoctivityDS.Application.Interfaces
{
    public interface IFileStorageService
    {
        /// <summary>
        /// Guarda un archivo en la carpeta configurada y retorna la ruta completa.
        /// </summary>
        Task<string> SaveFileAsync(byte[] content, string fileName, string subFolder, CancellationToken cancellationToken = default);

        /// <summary>
        /// Abre la carpeta de salida en el explorador de archivos.
        /// </summary>
        void OpenFolder(string subFolder);

        /// <summary>
        /// Obtiene la ruta base de almacenamiento.
        /// </summary>
        string GetStoragePath(string subFolder = "");
    }
}
