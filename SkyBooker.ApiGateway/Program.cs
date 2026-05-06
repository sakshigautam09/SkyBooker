using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpClient();

// ─── JWT Authentication ───────────────────────────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer           = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidateAudience         = true,
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            ValidateLifetime         = true,
            ClockSkew                = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
// ─────────────────────────────────────────────────────────────────────────────

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors("AllowFrontend");

// ─── Auth middleware MUST come before MapReverseProxy ─────────────────────────
app.UseAuthentication();
app.UseAuthorization();
// ─────────────────────────────────────────────────────────────────────────────

var swaggerDocs = new Dictionary<string, string>
{
    { "auth",         "http://localhost:5000/swagger/v1/swagger.json" },
    { "flight",       "http://localhost:5002/swagger/v1/swagger.json" },
    { "booking",      "http://localhost:5004/swagger/v1/swagger.json" },
    { "seat",         "http://localhost:5003/swagger/v1/swagger.json" },
    { "passenger",    "http://localhost:5005/swagger/v1/swagger.json" },
    { "payment",      "http://localhost:5006/swagger/v1/swagger.json" },
    { "notification", "http://localhost:5007/swagger/v1/swagger.json" },
    { "airline",      "http://localhost:5008/swagger/v1/swagger.json" },
};

foreach (var (name, url) in swaggerDocs)
{
    app.MapGet($"/swagger/docs/{name}", async (IHttpClientFactory factory) =>
    {
        var client = factory.CreateClient();
        try
        {
            var json = await client.GetStringAsync(url);
            return Results.Content(json, "application/json");
        }
        catch
        {
            return Results.Problem($"{name} service is not running.");
        }
    });
}

app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/docs/auth",         "Auth Service");
    c.SwaggerEndpoint("/swagger/docs/flight",       "Flight Service");
    c.SwaggerEndpoint("/swagger/docs/booking",      "Booking Service");
    c.SwaggerEndpoint("/swagger/docs/seat",         "Seat Service");
    c.SwaggerEndpoint("/swagger/docs/passenger",    "Passenger Service");
    c.SwaggerEndpoint("/swagger/docs/payment",      "Payment Service");
    c.SwaggerEndpoint("/swagger/docs/notification", "Notification Service");
    c.SwaggerEndpoint("/swagger/docs/airline",      "Airline Service");
    c.RoutePrefix   = "swagger";
    c.DocumentTitle = "SkyBooker — API Gateway";
    c.DisplayRequestDuration();
});

app.MapReverseProxy();

app.Run();