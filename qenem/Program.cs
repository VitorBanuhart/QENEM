using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using qenem;
using qenem.Data;
using qenem.Interfaces;
using qenem.Models;
using qenem.Services;
using System;

var builder = WebApplication.CreateBuilder(args);

var jsonBasePath = Path.Combine(builder.Environment.ContentRootPath, "Data", "Questions");

builder.Services.AddSingleton<JsonDataService>(sp =>
{
    var dataPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Questions");
    return new JsonDataService(dataPath);
});
builder.Services.AddSingleton<EnemRepository>();
builder.Services.AddSingleton<QuestionService>(sp =>
{
    var repo = sp.GetRequiredService<EnemRepository>();
    var dataPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Questions");
    return new QuestionService(repo, dataPath);
});
builder.Services.AddScoped<IEmailSender, MailGunEmailService>();
builder.Services.AddScoped<IEmailService, MailGunEmailService>();
builder.Services.AddScoped<SimuladoService>();
builder.Services.AddScoped<AvaliacaoService>();
builder.Services.AddScoped<AnotacoesService>();
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddSingleton<IEmailSender, MailGunEmailService>();
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();
builder.Services.AddScoped<PontosService>();
builder.Services.Configure<MailGunSetting>(builder.Configuration.GetSection("MailGun"));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = services.GetRequiredService<qenem.Data.ApplicationDbContext>();

        // 1. Aplicar Migrations
        logger.LogInformation("Aplicando migrations...");
        context.Database.Migrate();
        logger.LogInformation("Migrations aplicadas com sucesso.");

        // 2. Aplicar "Seed" de AreasInteresse
        if (!context.AreasInteresse.Any())
        {
            logger.LogInformation("Adicionando Areas de Interesse (seed)...");
            context.AreasInteresse.AddRange(
                new AreaInteresse { NomeAreaInteresse = "ciencias-humanas" },
                new AreaInteresse { NomeAreaInteresse = "ciencias-natureza" },
                new AreaInteresse { NomeAreaInteresse = "linguagens" },
                new AreaInteresse { NomeAreaInteresse = "matematica" },
                new AreaInteresse { NomeAreaInteresse = "espanhol" },
                new AreaInteresse { NomeAreaInteresse = "ingles" }
            );
            context.SaveChanges();
            logger.LogInformation("Seed de Areas de Interesse concluído.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Um erro ocorreu ao inicializar o banco de dados (Migration ou Seed).");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); 
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();
app.Run();