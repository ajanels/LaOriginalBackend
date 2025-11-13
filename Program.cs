using System.Globalization;
using System.Text;
using LaOriginalBackend.Data;
using LaOriginalBackend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ======================= DB =======================
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ======================= Servicios / Controllers =======================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddScoped<IVentasService, VentasService>();
builder.Services.AddScoped<ICajaDomainService, CajaDomainService>();

// Subidas grandes
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = long.MaxValue;
    o.ValueLengthLimit = int.MaxValue;
    o.MultipartHeadersLengthLimit = int.MaxValue;
});

// ======================= JWT =======================
var jwtKey = builder.Configuration["Jwt:Key"] ?? "clave-secreta-super-segura-12345";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "LaOriginal";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "LaOriginalUsuarios";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        o.SaveToken = true;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ======================= CORS =======================
var webUrl = builder.Configuration["App:WebUrl"]?.TrimEnd('/');
var allowedOrigins = new List<string> { "http://localhost:4200" };
if (!string.IsNullOrWhiteSpace(webUrl)) allowedOrigins.Add(webUrl);

builder.Services.AddCors(o =>
{
    o.AddPolicy("AllowAngularClient", p =>
        p.WithOrigins(allowedOrigins.Distinct().ToArray())
         .AllowAnyHeader()
         .AllowAnyMethod()
         .AllowCredentials());
});

// ======================= Swagger =======================
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "LaOriginalBackend", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Bearer {token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var swaggerEnabled = builder.Configuration.GetValue<bool>("Swagger:Enabled");
var swaggerRoutePrefix = builder.Configuration["Swagger:RoutePrefix"] ?? "swagger";

// ======================= Proxy/ALB (cloud/IIS) =======================
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
});

// === SendGrid ===
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddSingleton<EmailService>();

// Cultura (decimal/fecha) — opcional
CultureInfo.DefaultThreadCurrentCulture = new("es-GT");
CultureInfo.DefaultThreadCurrentUICulture = new("es-GT");

var app = builder.Build();

// ========== Forwarded headers ANTES que auth/cors ==========
app.UseForwardedHeaders();

// ======================= Archivos estáticos =======================
// 1) wwwroot
app.UseStaticFiles();

// 2) /uploads físico
var uploadsRoot = builder.Configuration["Uploads:Root"];
if (string.IsNullOrWhiteSpace(uploadsRoot))
{
    uploadsRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "LaOriginal", "uploads"
    );
}
Directory.CreateDirectory(uploadsRoot);

var contentTypeProvider = new FileExtensionContentTypeProvider();
contentTypeProvider.Mappings[".webp"] = "image/webp";
contentTypeProvider.Mappings[".avif"] = "image/avif";

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsRoot),
    RequestPath = "/uploads",
    ContentTypeProvider = contentTypeProvider,
    ServeUnknownFileTypes = false
});

// ======================= Middlewares =======================
app.UseCors("AllowAngularClient");

// Habilitar Swagger en dev o cuando Swagger:Enabled=true en appsettings.Production.json
if (app.Environment.IsDevelopment() || swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "LaOriginalBackend v1");
        c.RoutePrefix = swaggerRoutePrefix; // típico: "swagger"
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// ======================= DB migrate + seed =======================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    DbSeeder.SeedAsync(db).GetAwaiter().GetResult();
}

app.MapControllers();
app.Logger.LogInformation("Uploads sirviéndose desde: {UploadsRoot} => /uploads", uploadsRoot);

app.Run();
