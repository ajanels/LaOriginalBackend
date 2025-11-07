using LaOriginalBackend.Data;
using LaOriginalBackend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// =======================
// DB
// =======================
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// =======================
// Servicios / Controllers
// =======================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddScoped<IVentasService, VentasService>();
builder.Services.AddScoped<ICajaDomainService, CajaDomainService>();

// Aumentar límites de multipart (por si suben fotos grandes)
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = long.MaxValue;
    o.ValueLengthLimit = int.MaxValue;
    o.MultipartHeadersLengthLimit = int.MaxValue;
});

// =======================
// JWT
// =======================
var jwtKey = builder.Configuration["Jwt:Key"] ?? "clave-secreta-super-segura-12345";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "LaOriginal";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "LaOriginalUsuarios";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.RequireHttpsMetadata = false; // dev
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

// =======================
// CORS
// =======================
builder.Services.AddCors(o =>
{
    o.AddPolicy("AllowAngularClient", p =>
        p.WithOrigins("http://localhost:4200") // agrega tu dominio productivo aquí
         .AllowAnyHeader()
         .AllowAnyMethod()
         .AllowCredentials());
});

// =======================
// Swagger
// =======================
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

// =======================
// Proxy/ALB (cloud)
// =======================
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
});

// === SendGrid ===
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddSingleton<EmailService>(); // lee SendGrid:* desde user-secrets / variables de entorno

var app = builder.Build();

// =======================
// Rutas/archivos estáticos
// =======================

// 1) wwwroot normal
app.UseStaticFiles();

// 2) Carpeta externa para /uploads (evita reinicios por file-watcher)
var uploadsRoot = builder.Configuration["Uploads:Root"];

// Fallback SIEMPRE EXTERNO (nunca wwwroot/uploads)
if (string.IsNullOrWhiteSpace(uploadsRoot))
{
    uploadsRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "LaOriginal", "uploads"
    );
}
Directory.CreateDirectory(uploadsRoot);

// Content types extra (por si tu framework no mapea alguno)
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

// =======================
// Middlewares
// =======================
app.UseCors("AllowAngularClient");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Mantén estos puertos fijos si así lo usas en dev
    app.Urls.Clear();
    app.Urls.Add("http://localhost:5078");
    app.Urls.Add("https://localhost:7140");
}
else
{
    // En AWS respeta X-Forwarded-* del balanceador
    app.UseForwardedHeaders();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Mensaje útil al iniciar
app.Logger.LogInformation("Uploads sirviéndose desde: {UploadsRoot} => /uploads", uploadsRoot);

app.Run();
