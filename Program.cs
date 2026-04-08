using Backend.Helper;
using Backend.Presentation;
using Backend.Presentation.Middleware;
using Backend.Presentation.Routes;
using DotNetEnv;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Kestrel: allow more concurrent connections for bridge traffic
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxConcurrentConnections = 500;
    options.Limits.MaxConcurrentUpgradedConnections = 500;
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
});

// Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

Rest.Init(builder);

builder.Services.AddSingleton<WebSocketServer>();
builder.Services.AddSingleton<Backend.Infrastructure.Messaging.ITelegramNotifier, Backend.Infrastructure.Messaging.TelegramNotifier>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin();
    });
});
var app = builder.Build();
app.UseCors("AllowFrontend");

// Enable Swagger only in Development
if (Environment.GetEnvironmentVariable("APP_ENV") == "dev")
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<NotFoundMiddleware>();
var api = app.MapGroup("/api");
api.MapUserRoutes();
api.MapCtraderRoutes();
api.MapTraderRoutes();
api.MapLogRoutes();
api.MapHealthRoutes();
api.MapAiRoutes();

// Socket.Init(app);

app.Run();

