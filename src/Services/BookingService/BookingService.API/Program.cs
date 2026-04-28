using System.Text;
using BookingService.Application.Interfaces;
using BookingService.Infrastructure.Messaging;
using BookingService.Infrastructure.Persistence;
using BookingService.Infrastructure.Repositories;
using BookingService.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
var connStr = builder.Configuration.GetConnectionString("Default")!;
builder.Services.AddDbContext<BookingDbContext>(opt => opt.UseNpgsql(connStr));

// ── Repositories & Services ───────────────────────────────────────────────────
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IBookingService,    BookingService.Infrastructure.Services.BookingService>();
builder.Services.AddSingleton<IBookingEventPublisher, RabbitMqBookingEventPublisher>();

// ── HTTP Clients (inter-service) ──────────────────────────────────────────────
builder.Services.AddHttpClient<ISlotServiceClient, SlotServiceClient>(c =>
{
    c.BaseAddress = new Uri(builder.Configuration["Services:SlotService"]
                            ?? "http://slot-service:80");
    c.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddHttpClient<IPaymentServiceClient, PaymentServiceClient>(c =>
{
    c.BaseAddress = new Uri(builder.Configuration["Services:PaymentService"]
                            ?? "http://payment-service:80");
    c.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddHttpClient<IAuthServiceClient, AuthServiceClient>(c =>
{
    c.BaseAddress = new Uri(builder.Configuration["Services:AuthService"]
                            ?? "http://auth-service:80");
    c.Timeout = TimeSpan.FromSeconds(10);
});

// ── RabbitMQ Consumer (background service) ────────────────────────────────────
if (builder.Configuration.GetValue("RabbitMQ:Enabled", true))
{
    builder.Services.AddHostedService<PaymentEventConsumer>();
}

// ── JWT ───────────────────────────────────────────────────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ParkEase Booking API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization", Type = SecuritySchemeType.Http,
        Scheme = "bearer", BearerFormat = "JWT", In = ParameterLocation.Header
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {{
        new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        },
        Array.Empty<string>()
    }});
});

var app = builder.Build();

// ── Migrations ────────────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
    db.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ParkEase Booking API v1"));

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
