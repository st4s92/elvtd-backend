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

var app = builder.Build();

// Enable Swagger only in Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<NotFoundMiddleware>();
app.MapUserRoutes();
app.MapCtraderRoutes();
app.MapTraderRoutes();

Socket.Init(app);

app.Run();

