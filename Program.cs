using Backend.Helper;
using Backend.Presentation;
using Backend.Presentation.Middleware;
using Backend.Presentation.Routes;
using DotNetEnv;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

Rest.Init(builder);

builder.Services.AddSingleton<WebSocketServer>();

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

// Socket.Init(app);

app.Run();

