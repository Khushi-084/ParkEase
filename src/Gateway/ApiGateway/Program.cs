using ApiGateway.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration));

// Add CORS
builder.Services.AddCors(options =>
{
     options.AddPolicy("GatewayCorsPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "https://parkease-frontend-epu1.onrender.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });

});

// Add Reverse Proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure Swagger UI - only in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Middleware pipeline
app.UseSerilogRequestLogging();
app.UseCors("GatewayCorsPolicy");
app.UseMiddleware<ExceptionMiddleware>();
app.MapReverseProxy();

app.Run();