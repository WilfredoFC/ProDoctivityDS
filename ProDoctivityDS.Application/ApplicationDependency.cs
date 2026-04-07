using Microsoft.Extensions.DependencyInjection;
using ProDoctivityDS.Application.Interfaces;
using ProDoctivityDS.Application.Mappings;
using ProDoctivityDS.Application.Services;

namespace ProDoctivityDS.Application
{
    public static class ApplicationDependency
    {
        public static void AddApplicationServices(this IServiceCollection services)
        {

            services.AddMemoryCache();
            services.AddHttpClient();

            // Servicios de aplicación
            services.AddScoped<IConfigurationService, ConfigurationService>();
            services.AddScoped<ISearchService, SearchService>();
            services.AddScoped<ISelectionService, SelectionService>();
            services.AddScoped<IProcessingService, ProcessingService>();
            services.AddScoped<IAnalysisService, AnalysisService>();
            services.AddScoped<IDocumentTypeService, DocumentTypeService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IDuplicateDetectionService, DuplicateDetectionService>();
            services.AddScoped<IDocumentDeletionService, DocumentDeletionService>();


            services.AddScoped<IProductivityApiClient, ProductivityApiClient>();
            services.AddScoped<IFileStorageService, FileStorageService>();

            services.AddSingleton<IProcessingProgressStore, ProcessingProgressStore>();
            // AutoMapper
            services.AddAutoMapper(config => { }, typeof(MappingProfile));
        }
    }
}
