using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Ticket.Application.Interfaces;
using Ticket.Infrastructure.ExternalServices;
using Ticket.Infrastructure.Persistence;
using Ticket.Infrastructure.Repositories;
using Ticket.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

var connStr = builder.Configuration.GetConnectionString("Default")!;
builder.Services.AddDbContext<TicketDbContext>(opt => opt.UseNpgsql(connStr));
builder.Services.AddHttpContextAccessor();

// ── SlotService HTTP client ───────────────────────────────────────────────────
builder.Services.AddHttpClient("SlotService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["SlotService:BaseUrl"]!);
    client.Timeout     = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// ── PaymentService HTTP client ────────────────────────────────────────────────
builder.Services.AddHttpClient("PaymentService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["PaymentService:BaseUrl"]!);
    client.Timeout     = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// ── Repositories & Services ───────────────────────────────────────────────────
builder.Services.AddScoped<ITicketRepository,     TicketRepository>();
builder.Services.AddScoped<ISlotServiceClient,    SlotServiceClient>();
builder.Services.AddScoped<IPaymentServiceClient, PaymentServiceClient>(); // NEW
builder.Services.AddScoped<ITicketService,        TicketService>();

// ── JWT ───────────────────────────────────────────────────────────────────────
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ParkEase Ticket API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Enter your JWT token here"
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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
    db.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ParkEase Ticket API v1"));

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();