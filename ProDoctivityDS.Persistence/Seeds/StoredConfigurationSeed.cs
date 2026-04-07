using Microsoft.Extensions.DependencyInjection;
using ProDoctivityDS.Domain.Entities;
using ProDoctivityDS.Domain.Entities.ValueObjects;
using ProDoctivityDS.Domain.Interfaces;

namespace ProDoctivityDS.Persistence.Seeds
{
    public static class StoredConfigurationSeeder
    {
        public static async Task SeedDefaultConfigurationAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IStoredConfigurationRepository>();

            // Verificar si ya existe una configuración
            var existing = await repository.GetActiveConfigurationAsync();

            // Consideramos que no hay configuración si el Id es 0 y todos los campos relevantes están vacíos
            if (existing == null || string.IsNullOrEmpty(existing.ApiBaseUrl))
            {
                var defaultConfig = new StoredConfiguration
                {
                    ApiBaseUrl = "https://cloud.prodoctivity.com/api/",
                    ApiKey = "pdoca42e8b242be79048104aba65b2e09ecc",
                    ApiSecret = "7f92aa9722fbfa7ce7f5c7aa5113ec871947a9c6198a8682e64077bd6baebe77",
                    BearerToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpYXQiOjE3NzU1MDM2NzEuNzA0LCJwZXJtaXNzaW9ucyI6WyJkb2N1bWVudC1jb2xsZWN0aW9uLXRhc2siLCJnZW5lcmF0aW9uLW1vbml0b3IiLCJwdWJsaXNoLXRlbXBsYXRlIiwic2hhcmUtZG9jdW1lbnQtbW9uaXRvciIsImNhbi1pbXBlcnNvbmF0ZSIsImRlbGV0ZS1kb2N1bWVudC1jb2xsZWN0aW9uIiwiZGVsZXRlLWRvY3VtZW50IiwidGFza3MtbWFuYWdlciIsImRlbGV0ZS10ZW1wbGF0ZSIsIm9yZ2FuaXphdGlvbi1hZG1pbiJdLCJvcmdhbml6YXRpb25JZCI6ImJhcm5hZG8iLCJtZmFBdXRoZW50aWNhdGVkIjpmYWxzZSwidXNlcm5hbWUiOiJXZmVsaXpAbm92b3NpdC5jb20iLCJqdGkiOiJkYjc3NGQ1NC0zYmY5LTQ1NjEtOGYxZS0xZjMzYTI2ODM4MWYiLCJuYmYiOjE3NzU1MDM2NzEsImV4cCI6MTc3NTUwNTQ3MSwiYXVkIjoiYXBwLXVzZXIiLCJpc3MiOiJQcm9Eb2N0aXZpdHkiLCJzdWIiOiJXZmVsaXpAbm92b3NpdC5jb20ifQ.FXqcKsfAQ3iIUpLfGMp9ZFy6yUuRIz_RYaH8hZMBft8",
                    CookieSessionId = "PRODOC-SESSIONID=cf7f4b726ee5c61361a12a8e81e55bba|28133ace3d4d3e232a9164fcaf7f411e",
                    Username = "Wfeliz@novosit.com",
                    Password = "Dharafeliz1327!",
                    ProcessingOptions = new ProcessingOptions() {
                    RemoveFirstPage = true,
                    OnlyIfCriteriaMet = true,
                    UpdateApi = true,
                    SaveOriginalFiles = true,
                    AutoRemoveAllSeparators = false,
                    CreateBackup = true,
                    RemovePagesEnabled = true,
                    PagesToRemove = "1",
                    RemoveMode = "specific",
                    PageRangeStart = 0,
                    PageRangeEnd = 1,
                    AnalyzeAllPages = false,
                    ShowExtractedText = true
                    }, 
                    AnalysisRules = new AnalysisRuleSet()
                    {
                        KeywordSeparador = "SEPARADOR DE DOCUMENTOS",
                        KeywordCodigo = "DOC-001",
                        Normalization = new NormalizationOptions()
                        {
                            IsEnabled = true,
                            ToUpperCase = true,
                            RemoveAccents = true,
                            RemovePunctuation = true,
                            IgnoreLineBreaks = true,
                            TrimExtraSpaces = true,
                        },
                        SearchCharacterLimit = 25

                    }        
                };

                await repository.UpdateConfigurationAsync(defaultConfig);
            }
        }
    }
}