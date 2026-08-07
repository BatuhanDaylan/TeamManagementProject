using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TeamManagement.Application.Interfaces;
using TeamManagement.Infrastructure.Persistence;
using TeamManagement.Infrastructure.Services;
using TeamManagement.Infrastructure.BackgroundServices;
using TeamManagement.Api.Hubs;

var builder = WebApplication.CreateBuilder(args);

//DbContext Kayıtları
builder.Services.AddDbContext<TeamManagementIdentityDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("IdentityConnection")));

builder.Services.AddDbContext<TeamManagementAppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("AppConnection")));

//Servis Kayıtları
builder.Services.AddHttpClient<IAiEmailParserService, AiEmailParserService>();
builder.Services.AddHttpClient<IAiMetinUretimService, AiMetinUretimService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEkipService, EkipService>();
builder.Services.AddScoped<IProjeService, ProjeService>();
builder.Services.AddScoped<IGorevService, GorevService>();
builder.Services.AddScoped<IYorumService, YorumService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ITimelineService, TimelineService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IDavetService, DavetService>();
builder.Services.AddScoped<IBildirimService, BildirimService>();
builder.Services.AddScoped<IHataService, HataService>();
builder.Services.AddScoped<IGorevIlerlemeService, GorevIlerlemeService>();
builder.Services.AddScoped<INotificationService, SignalRNotificationService>();
builder.Services.AddScoped<IGizliVeriKorumaService, GizliVeriKorumaService>();
builder.Services.AddScoped<IMailAyarlariService, MailAyarlariService>();
builder.Services.AddScoped<IEkipOzetiService, EkipOzetiService>();
builder.Services.AddDataProtection();
builder.Services.AddHostedService<HaftalikPlanMailListenerBackgroundService>();
builder.Services.AddHostedService<HaftalikOzetBackgroundService>();

//SignalR gerçek zamanlı iletişim servisi
builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // SignalR için ileride gerekli
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

//JWT Authentication Ayarları
var jwtSecret = builder.Configuration["Jwt:Secret"]!;
var key = Encoding.ASCII.GetBytes(jwtSecret);

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("EkipLideriPolicisi", policy =>
        policy.RequireRole("Lider"));
});

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<TeamManagement.Api.Middlewares.ExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseCors("FrontendPolicy");

// Kimlik doğrulama sistemlerini aktifleştir
app.UseAuthentication();
app.UseAuthorization();

//Controllarlari haritalandir
app.MapControllers();

//SignalR Hub endpoint'i istemciler bu adrese bağlanır
app.MapHub<GorevHub>("/hubs/gorev");

app.Run();
