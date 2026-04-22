using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using StokSayimApi.Data;
using System.Text;
using StokSayimApi.Services;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("PosApp", policy =>
        {
            policy
                .WithOrigins(
                    "http://localhost:5173",    // Vite dev
                    "http://localhost:4173",    // Vite preview
                    "https://kasa.marifoglu.trade" // prod (domain netleşince güncelle)
                )
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });

    builder.Services.AddControllers().AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // PostgreSQL
    builder.Services.AddDbContext<AppDbContext>(options =>
        options
            .UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

    // JWT
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    var secret = jwtSettings["Secret"]!;

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
            };
        });

    builder.Services.AddAuthorization();
    // Services
    builder.Services.AddScoped<JwtService>();
    builder.Services.AddScoped<AuthService>();
    builder.Services.AddScoped<BranchService>();
    builder.Services.AddScoped<UserService>();
    builder.Services.AddScoped<ProductService>();
    builder.Services.AddScoped<StockService>();
    builder.Services.AddScoped<ReportService>();
    builder.Services.AddScoped<CategoryService>();
    builder.Services.AddScoped<SyncService>();
    builder.Services.AddScoped<SaleService>();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseCors("PosApp");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    // Migration ve seed
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var syncService = scope.ServiceProvider.GetRequiredService<SyncService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        db.Database.Migrate();
        Log.Information("Database migrated");
        await DbSeeder.SeedAsync(db, syncService, logger);
    }

    Log.Information("Application started");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application failed to start");
}
finally
{
    Log.CloseAndFlush();
}