using GymManagement.Application.Interfaces;
using GymManagement.Application.Services;
using GymManagement.Infrastructure.Persistence;
using GymManagement.Infrastructure.Repositories;
using GymManagement.Infrastructure.Services;
using GymManagement.Presentation.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddSwaggerGen(options =>
{
options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
{
    Name = "Authorization",
    Type = SecuritySchemeType.Http,
    Scheme = "bearer",
    BearerFormat = "JWT",
    In = ParameterLocation.Header,
    Description = "Ingresá el token JWT. No hace falta escribir 'Bearer', Swagger lo agrega solo."
});

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("Bearer", document), [] }
    });
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("GymManagementConnectionString")));

builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<AdminService>();

builder.Services.AddScoped<ITrainerRepository, TrainerRepository>();
builder.Services.AddScoped<TrainerService>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.OnlyAdmin, policy => policy.RequireClaim("role", "Admin"));
    options.AddPolicy(Policies.OnlyTrainer, policy => policy.RequireClaim("role", "Trainer"));
    options.AddPolicy(Policies.OnlyClient, policy => policy.RequireClaim("role", "Client"));
});

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


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();