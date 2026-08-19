using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;
using System.Text;
using GymManagement.Application.Interfaces;
using GymManagement.Application.Services;
using GymManagement.Infrastructure.Persistence;
using GymManagement.Infrastructure.Repositories;
using GymManagement.Infrastructure.Services;
using GymManagement.Infrastructure.Settings;
using GymManagement.Infrastructure.Payments;
using GymManagement.Presentation.Authorization;
using GymManagement.Presentation.Middlewares;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("GymManagementConnectionString"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()));

builder.Services.Configure<MercadoPagoSettings>(
    builder.Configuration.GetSection("MercadoPago"));

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<ITrainerRepository, TrainerRepository>();
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddScoped<IGymClassRepository, GymClassRepository>();
builder.Services.AddScoped<IInscriptionRepository, InscriptionRepository>();
builder.Services.AddScoped<IClassNotificationService, ClassNotificationService>();
builder.Services.AddScoped<IGymClassService, GymClassService>();

builder.Services.AddScoped<IGymClassScheduleRepository, GymClassScheduleRepository>();
builder.Services.AddScoped<GymClassScheduleService>();

builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<PaymentService>();

builder.Services.AddHttpClient();

// Mercado Pago cancellations: up to 10 Polly retries with exponential backoff.
builder.Services.AddHttpClient("MercadoPago")
    .AddPolicyHandler(HttpPolicyExtensions
        .HandleTransientHttpError()   // 5xx + 408
        .WaitAndRetryAsync(
            retryCount: 5,
            sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
            onRetry: (outcome, timespan, attempt, _) =>
            {
                Console.WriteLine($"[MercadoPago] Retry {attempt} after {timespan.TotalSeconds:F1}s — {outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()}");
            }));

builder.Services.AddScoped<MercadoPagoService>();
builder.Services.AddScoped<IMembershipBillingService>(sp => sp.GetRequiredService<MercadoPagoService>());

builder.Services.AddScoped<IMetricsRepository, MetricsRepository>();
builder.Services.AddScoped<MetricsService>();

builder.Services.AddScoped<IMembershipRepository, MembershipRepository>();
builder.Services.AddScoped<IMembershipPlanRepository, MembershipPlanRepository>();
builder.Services.AddScoped<MembershipService>();
builder.Services.AddScoped<IMembershipPlanService, MembershipPlanService>();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Policies.OnlyAdmin, policy => policy.RequireClaim(ClaimTypes.Role, "Admin"))
    .AddPolicy(Policies.OnlyTrainer, policy => policy.RequireClaim(ClaimTypes.Role, "Trainer"))
    .AddPolicy(Policies.OnlyClient, policy => policy.RequireClaim(ClaimTypes.Role, "Client"))
    .AddPolicy(Policies.AdminOrTrainer, policy => policy.RequireAssertion(ctx =>
        ctx.User.HasClaim(ClaimTypes.Role, "Admin") ||
        ctx.User.HasClaim(ClaimTypes.Role, "Trainer")));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{

}

app.UseHttpsRedirection();

app.UseCors(x=>x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()); 

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" })).AllowAnonymous();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // The deploy pipeline migrates once per release; doing it here too would let several App
    // Service instances migrate concurrently. Development has no pipeline, so it self-migrates.
    if (app.Environment.IsDevelopment())
    {
        context.Database.Migrate();
    }

    
}

app.Run();
