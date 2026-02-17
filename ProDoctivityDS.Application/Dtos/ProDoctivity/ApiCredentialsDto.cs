namespace ProDoctivityDS.Application.Dtos.ProDoctivity
{
    public class ApiCredentialsDto
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string ApiSecret { get; set; } = string.Empty;
        public string BearerToken { get; set; } = string.Empty;
        public string CookieSessionId { get; set; } = string.Empty;
    }

}
