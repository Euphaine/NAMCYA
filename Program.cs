using Microsoft.EntityFrameworkCore;
using EventScoringSystem.Data;
using EventScoringSystem.Components;
using EventScoringSystem.Services;

var builder = WebApplication.CreateBuilder(args);
// ... the rest of your code

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
    builder.Services.AddScoped<TabulationService>();
builder.Services.AddScoped<EventPdfReportService>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<EventPdfReportService>();

// Configure SQLite in Local Application Data
var folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NAMCYA");
Directory.CreateDirectory(folderPath);
var dbPath = Path.Combine(folderPath, "namcya_scoring.db");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

var app = builder.Build();

app.Urls.Add("http://0.0.0.0:5000");  

// Ensure database and tables are automatically created on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();