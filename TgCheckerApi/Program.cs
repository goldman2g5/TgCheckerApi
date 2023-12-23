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
using Quartz;
using Quartz.Impl;
using Quartz.Spi;


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
builder.Services.AddSingleton<IJobFactory, QuartzJobFactory>();
builder.Services.AddSingleton<ISchedulerFactory, StdSchedulerFactory>();
builder.Services.AddSingleton<RatingResetJob>();
builder.Services.AddSingleton(new JobSchedule(
    jobType: typeof(RatingResetJob),
    cronExpression: "0 27 0 * * ?")); // Runs at 23:48 every day
builder.Services.AddHostedService<QuartzHostedService>(); // Ensure this is present and correctly implemented
builder.Services.AddHttpClient("MyClient", client =>
{
    client.BaseAddress = new Uri("https://localhost:7256"); // Use your application's address
});

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();



var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

//app.UseHttpsRedirection();

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

