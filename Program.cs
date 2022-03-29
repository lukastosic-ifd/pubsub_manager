using PubSubManager.Services;

var builder = WebApplication.CreateBuilder(args);

if(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development" )
{
    Environment.SetEnvironmentVariable("PUBSUB_EMULATOR_HOST", "localhost:8085");
    Environment.SetEnvironmentVariable("PUBSUB_PROJECT_ID", "kikker");
}


// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSingleton<ConfigService>();
builder.Services.AddScoped<PubSubService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
