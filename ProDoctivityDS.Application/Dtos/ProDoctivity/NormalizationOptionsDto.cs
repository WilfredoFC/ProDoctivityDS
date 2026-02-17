namespace ProDoctivityDS.Application.Dtos.ProDoctivity
{
    public class NormalizationOptionsDto
    {
        public bool IsEnabled { get; set; } = false;
        public bool ToUpperCase { get; set; } = false;
        public bool RemoveAccents { get; set; } = false;
        public bool RemovePunctuation { get; set; } = false;
        public bool IgnoreLineBreaks { get; set; } = false;
        public bool TrimExtraSpaces { get; set; } = false;
    }
}
