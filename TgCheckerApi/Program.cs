using Microsoft.EntityFrameworkCore;
using TgCheckerApi.Models;
using Newtonsoft.Json;
using TgCheckerApi;
using TgCheckerApi.Models.BaseModels;
using TgCheckerApi.Websockets;
using TgCheckerApi.MiddleWare;
using AutoMapper;
using Microsoft.IdentityModel.Tokens;
using TgCheckerApi.MapperProfiles;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using TgCheckerApi.Services;
using TgCheckerApi.Job;
using TgCheckerApi.Quartz;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using TgCheckerApi.Controllers;
using Serilog;
using TgCheckerApi.Interfaces;using WTelegram;
using Nest;
using TgCheckerApi.Events;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("bebrahsdfihjopuskdfghoujsdfghjkjskudfghdfgjskhdfgkhjdfghjkgdfhjk")),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "My API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "Bearer",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});
builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddDbContext<TgDbContext>(o => o.UseLazyLoadingProxies().UseNpgsql(builder.Configuration.GetConnectionString("MainConnectionString")));
//builder.Services.AddHostedService<NotificationTask>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders("*");
    });
});
builder.Services.AddSignalR();
builder.Services.AddAutoMapper(typeof(MappingProfile));
builder.Services.AddSingleton<TaskManager>();
builder.Services.AddSingleton<WebSocketService>();
builder.Services.AddScoped<YooKassaService>();
builder.Services.AddScoped<BotControllerService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddSingleton<IJobFactory, QuartzJobFactory>();
builder.Services.AddSingleton<ISchedulerFactory, StdSchedulerFactory>();
builder.Services.AddTransient<RecalculateTopPosJob>();
builder.Services.AddTransient<LoadDelayedNotificationsJob>();
builder.Services.AddScoped<NotificationJob>();
builder.Services.AddSingleton<RatingResetJob>();
builder.Services.AddSingleton<UpdateSubscribersJob>();


builder.Services.AddSingleton(new JobSchedule(
    jobType: typeof(RatingResetJob),
    cronExpression: "0 0 3 1,15 * ?"));
builder.Services.AddSingleton(new JobSchedule(
    jobType: typeof(UpdateSubscribersJob),
    cronExpression: "0 0 4 * * ?"));
builder.Services.AddSingleton(new JobSchedule(
    jobType: typeof(RecalculateTopPosJob),
    cronExpression: "0 16 20 * * ?"));



//builder.Services.AddSingleton(new JobSchedule(
//    jobType: typeof(RatingResetJob),
//    cronExpression: "0 27 0 * * ?")); // Runs at 23:48 every day

builder.Services.AddHostedService<QuartzHostedService>();

builder.Services.AddSingleton<IScheduler>(provider =>
{
    var schedulerFactory = provider.GetRequiredService<ISchedulerFactory>();
    var scheduler = schedulerFactory.GetScheduler().Result; // Get the scheduler instance
    scheduler.JobFactory = provider.GetRequiredService<IJobFactory>(); // Set the custom job factory
    scheduler.Start().Wait(); // Start the scheduler
    return scheduler;
});

builder.Services.AddSingleton<IDbContextFactory<TgDbContext>>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("MainConnectionString");

    var optionsBuilder = new DbContextOptionsBuilder<TgDbContext>();
    optionsBuilder.UseNpgsql(connectionString).UseLazyLoadingProxies();

    return new MyDbContextFactory(optionsBuilder.Options, serviceProvider.GetRequiredService<IServiceScopeFactory>());
});
builder.Services.AddSingleton<TgClientFactory>();
builder.Services.AddSingleton<TelegramClientService>();
builder.Services.AddHostedService<TelegramClientInitializer>();
builder.Services.AddScoped<IElasticsearchIndexingService, ElasticsearchIndexingService>();
builder.Services.AddHostedService<ElasticsearchIndexInitializer>();
builder.Services.AddSingleton<ChannelUpdateBackgroundService>();



var settings = new ConnectionSettings(new Uri("http://localhost:9200"))
        .DefaultIndex("channels")
        .DefaultMappingFor<Channel>(m => m
        .Ignore(p => p.ChannelAccesses)
        .Ignore(p => p.ChannelHasSubscriptions)
        .Ignore(p => p.ChannelHasTags)
        .Ignore(p => p.Comments)
        .Ignore(p => p.Messages)
        .Ignore(p => p.NotificationDelayedTasks)
        .Ignore(p => p.NotificationsNavigation)
        .Ignore(p => p.Payments)
        .Ignore(p => p.Reports)
        .Ignore(p => p.StatisticsSheets)
        .Ignore(p => p.TelegramPayments)
        .Ignore(p => p.Tgclient)
        .Ignore(p => p.UserNavigation)
    );
; // Set the default index here
var client = new ElasticClient(settings);

builder.Services.AddSingleton<IElasticClient>(client);



builder.Services.AddHttpClient("MyClient", client =>
{
    client.BaseAddress = new Uri("https://localhost:7256");
});

builder.Host.UseSerilog((_, conf) =>
{
    conf
        .WriteTo.Console()
        .WriteTo.File("log-.txt",
 rollingInterval: RollingInterval.Day)
        .MinimumLevel.Override("Quartz", Serilog.Events.LogEventLevel.Information);
    ;
});
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();



var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseCors();

app.MapControllers();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();


//app.UseMiddleware<ApiKeyMiddleware>();

//app.UseMiddleware<ThrottleMiddleware>(30, 60);

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapHub<AuthHub>("/Authhub");
    endpoints.MapHub<NotificationHub>("/NotificationHub");
    endpoints.MapHub<BotHub>("/BotHub");
});

app.Run();

