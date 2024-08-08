using ChatApp.Data;
using ChatApp.HubConfig;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    var serverVersion = ServerVersion.AutoDetect(connectionString);
    options.UseMySql(connectionString, serverVersion);

    if (builder.Environment.IsDevelopment())
    {
        options.LogTo(Console.WriteLine, LogLevel.Information)
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors();
    }
});

builder.Services.AddAuthentication(options => {
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateAudience = false,
        ValidateIssuer = false,
        ValidateLifetime = false,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudiences = builder.Configuration["Jwt:Audience"]?.Split(';'),
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"].ToString()))
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];

            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) &&
                (path.StartsWithSegments("/chathub")))
            {
                context.Token = $"{accessToken}";
                context.Request.Headers.Add("Authorization", context.Token);
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowAllHeaders", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});

builder.Services.AddSignalR(opts =>
{
    opts.EnableDetailedErrors = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseCors("AllowAllHeaders");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHub<CommonHub>("/chathub");

app.Run();
