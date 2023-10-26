using Microsoft.EntityFrameworkCore;
using TgCheckerApi.Models;
using Newtonsoft.Json;
using TgCheckerApi;
using TgCheckerApi.Models.BaseModels;
using TgCheckerApi.Websockets;
using TgCheckerApi.MiddleWare;
using AutoMapper;
using TgCheckerApi.MapperProfiles;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

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
builder.Services.AddHostedService<NotificationTask>();
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

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseCors();

app.UseRouting();


//app.UseMiddleware<ApiKeyMiddleware>();

//app.UseMiddleware<ThrottleMiddleware>(2, 60);

app.UseEndpoints(endpoints =>
{
    endpoints.MapHub<AuthHub>("/Authhub");
    endpoints.MapHub<NotificationHub>("/Notificationhub");
});

app.Run();

