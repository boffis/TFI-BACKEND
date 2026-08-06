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

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

//builder.Services.AddSwaggerGen(options =>
//{
//options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
//{
//    Name = "Authorization",
//    Type = SecuritySchemeType.Http,
//    Scheme = "bearer",
//    BearerFormat = "JWT",
//    In = ParameterLocation.Header,
//    Description = "Ingresá el token JWT. No hace falta escribir 'Bearer', Swagger lo agrega solo."
//});

//    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
//    {
//        { new OpenApiSecuritySchemeReference("Bearer", document), [] }
//    });
//});

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
builder.Services.AddScoped<IGymClassService, GymClassService>();

builder.Services.AddScoped<IGymClassScheduleRepository, GymClassScheduleRepository>();
builder.Services.AddScoped<GymClassScheduleService>();

builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<PaymentService>();

builder.Services.AddHttpClient();
builder.Services.AddScoped<MercadoPagoService>();

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
    context.Database.Migrate();
    
    // Seed Admin
    if (!context.Admins.Any(a => a.Email == "highlevelperformancegym@gmail.com"))
    {
        var admin = new GymManagement.Domain.Entities.Admin
        {
            UserId = Guid.NewGuid(),
            Name = "GymAdmin",
            Email = "highlevelperformancegym@gmail.com",
            Password = BCrypt.Net.BCrypt.HashPassword("Admin1234"),
            DateOfBirth = new DateOnly(2002, 6, 21),
            DNI = "12345678",
            Gender = "Other",
            PhoneNumber = "3416150161",
            IsUserDeleted = false,
            IsEmailConfirmed = true
        };
        context.Admins.Add(admin);
        context.SaveChanges();
    }
}

app.Run();
