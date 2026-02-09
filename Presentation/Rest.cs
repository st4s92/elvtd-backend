using Backend.Application.Interfaces;
using Backend.Application.Usecases;
using Backend.Helper;
using Backend.Infrastructure.Messaging;
using Backend.Infrastructure.Repositories;
using Backend.Presentation.Handlers;
using Backend.Presentation.Messaging;
using Backend.Presentation.Middleware;
using Backend.Presentation.Routes;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Backend.Presentation;

public static class Rest
{
    public static void Init(WebApplicationBuilder builder)
    {
        var host = Environment.GetEnvironmentVariable("DB_HOST");
        var port = Environment.GetEnvironmentVariable("DB_PORT");
        var db = Environment.GetEnvironmentVariable("DB_NAME");
        var user = Environment.GetEnvironmentVariable("DB_USER");
        var pass = Environment.GetEnvironmentVariable("DB_PASSWORD");

        var connectionString = $"Host={host};Port={port};Database={db};Username={user};Password={pass}";
        builder.Services.AddDbContext<AppDbContext>(options => options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

        builder.Services.AddHttpClient();
        builder.Services.AddSingleton<RabbitMqConnection>();
        
        builder.Services.AddScoped(typeof(AppLogger<>));
        builder.Services.AddSingleton<IJobPublisher, RabbitMqJobPublisher>();
        builder.Services.AddScoped<IServerAccountRepository, ServerAccountRepository>();
        builder.Services.AddScoped<IServerRepository, ServerRepository>();
        builder.Services.AddScoped<IMasterSlaveRepository, MasterSlaveRepository>();
        builder.Services.AddScoped<IMasterSlaveConfigRepository, MasterSlaveConfigRepository>();
        builder.Services.AddScoped<IMasterSlavePairRepository, MasterSlavePairRepository>();
        builder.Services.AddScoped<IOrderRepository, OrderRepository>();
        builder.Services.AddScoped<IActiveOrderRepository, ActiveOrderRepository>();
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<ITradingRepository, TradingRepository>();
        builder.Services.AddScoped<ICtraderRepository, CtraderRepository>();
        builder.Services.AddScoped<IAccountRepository, AccountRepository>();
        builder.Services.AddScoped<CtraderUsecase>();
        builder.Services.AddScoped<TraderUsecase>();
        builder.Services.AddScoped<UserUsecase>();
        builder.Services.AddScoped<UserHandler>();
        builder.Services.AddScoped<CtraderHandler>();
        builder.Services.AddScoped<TraderHandler>();
        builder.Services.AddScoped<ServerHeartbeatHandler>();
        builder.Services.AddHostedService<ServerHeartbeatConsumer>();
        builder.Services.AddScoped<ServerPlatformCreatedHandler>();
        builder.Services.AddHostedService<ServerPlatformCreatedConsumer>();
    }
}