namespace ProDoctivityDS.Application.Interfaces
{
    public interface IPdfManipulator
    {
        /// <summary>
        /// Elimina la primera página del PDF. Si solo tiene una página, retorna el original.
        /// </summary>
        Task<byte[]> RemoveFirstPageAsync(byte[] pdfBytes, CancellationToken cancellationToken = default);
    }
}
