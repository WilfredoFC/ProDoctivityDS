using Microsoft.AspNetCore.DataProtection;
using Microsoft.OpenApi;
using ProDoctivityDS.Application;
using ProDoctivityDS.Persistence;
using ProDoctivityDS.Shared;
using ProDoctivityDS.Persistence.Seeds;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddPersistenceDependencies(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddSharedServices();

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// Configurar Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Prodoctivity Processor API",
        Version = "v1",
        Description = "API para procesamiento inteligente de documentos PDF en Productivity Cloud"
    });

    // Definir el header X-Session-Id
    c.AddSecurityDefinition("SessionId", new OpenApiSecurityScheme
    {
        Name = "X-Session-Id",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "Identificador de sesión. Si no se envía, el backend generará uno nuevo."
    });

    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        // Aquí usamos OpenApiSecuritySchemeReference en lugar de Reference
        [new OpenApiSecuritySchemeReference("SessionId", document)] = new List<string>()
    });
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None;
});

var keyRingPath = Path.Combine(builder.Environment.ContentRootPath, "DataProtectionKeys");
Directory.CreateDirectory(keyRingPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "DataProtectionKeys")))
    .SetApplicationName("ProDoctivityDS");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await StoredConfigurationSeeder.SeedDefaultConfigurationAsync(scope.ServiceProvider);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Prodoctivity Processor API v1");
        c.RoutePrefix = string.Empty; // Para que Swagger UI sea la página principal (opcional)
    });
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseSession();

app.UseAuthorization();
app.MapControllers();

app.Run();
