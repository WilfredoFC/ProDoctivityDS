namespace ProDoctivityDS.Domain.Entities.ValueObjects
{

    public class CriterionDto
    {
        public string Text { get; set; } = string.Empty;
        public bool IsRegex { get; set; } = false;
    }

}
