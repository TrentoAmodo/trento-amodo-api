using TrentoAmodo.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Gitignored, developer-local override (connection strings, etc.) — takes precedence over appsettings.*.json.
builder.Configuration.AddJsonFile("appsettings.Development.Local.json", optional: true, reloadOnChange: true);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' not configured.");
builder.Services.AddInfrastructure(connectionString);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
