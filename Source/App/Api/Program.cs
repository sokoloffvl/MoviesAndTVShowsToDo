using Marten;
using MoviesAndTVShowsToDo.Api.Data;
using MoviesAndTVShowsToDo.Api.Repositories;
using MoviesAndTVShowsToDo.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is required.");

builder.Services.AddMarten(options =>
{
    options.Connection(connectionString);
    MartenStoreConfig.ConfigureStore(options);
});

builder.Services.AddScoped<IMediaRepository, MartenMediaRepository>();
builder.Services.AddScoped<MediaService>();
builder.Services.AddScoped<IMetadataAggregator, MetadataAggregator>();
builder.Services.AddScoped<IMetadataProvider, TmdbMetadataProvider>();
builder.Services.AddScoped<IRatingEnricher, OmdbRatingEnricher>();
builder.Services.AddScoped<TmdbMetadataProvider>();
builder.Services.AddScoped<OmdbRatingEnricher>();

builder.Services.Configure<TmdbOptions>(builder.Configuration.GetSection(TmdbOptions.SectionName));
builder.Services.Configure<OmdbOptions>(builder.Configuration.GetSection(OmdbOptions.SectionName));
builder.Services.AddHttpClient<TmdbMetadataProvider>();
builder.Services.AddHttpClient<OmdbRatingEnricher>();

var clientOrigin = builder.Configuration["ClientOrigin"] ?? "http://localhost:5173";
builder.Services.AddCors(options =>
{
    options.AddPolicy("SpaDev", policy =>
        policy.WithOrigins(clientOrigin)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseCors("SpaDev");
}
else
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

app.MapControllers();

if (!app.Environment.IsDevelopment())
    app.MapFallbackToFile("index.html");

app.Run();

public partial class Program;
