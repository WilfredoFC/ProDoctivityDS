namespace ProDoctivityDS.Domain.Entities.ValueObjects
{
    public class ProcessingOptionsDto
    {
        public bool RemoveFirstPage { get; set; } = true;
        public bool OnlyIfCriteriaMet { get; set; } = true;
        public bool UpdateApi { get; set; } = false;
        public bool SaveOriginalFiles { get; set; } = false;
        public bool CreateBackup { get; set; } = false; // reservado
    }
}
