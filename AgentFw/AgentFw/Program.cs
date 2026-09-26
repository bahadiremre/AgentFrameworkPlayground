using AgentFw.Data;
using AgentFw.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Encrypts stored API keys. A fixed application name keeps the key ring stable
// even if the project folder is moved or renamed.
builder.Services.AddDataProtection().SetApplicationName("AgentFrameworkPlayground");
builder.Services.AddSingleton<ApiKeyProtector>();

// Connection string lives in user secrets (ConnectionStrings:AgentRag), never in appsettings.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("AgentRag"))
           .UseSnakeCaseNamingConvention());

var app = builder.Build();

// Apply pending EF Core migrations on startup.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
