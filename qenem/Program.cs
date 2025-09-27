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
builder.Services.AddScoped<SimuladoService>();
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddSingleton<IEmailSender, MailerSendEmailService>();
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.Configure<MailerSendSetting>(builder.Configuration.GetSection("MailerSend"));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (!context.AreasInteresse.Any())
    {
        context.AreasInteresse.AddRange(
            new AreaInteresse { NomeAreaInteresse = "ciencias-humanas" },
            new AreaInteresse { NomeAreaInteresse = "ciencias-natureza" },
            new AreaInteresse { NomeAreaInteresse = "linguagens" },
            new AreaInteresse { NomeAreaInteresse = "matematica" },
            new AreaInteresse { NomeAreaInteresse = "espanhol" },
            new AreaInteresse { NomeAreaInteresse = "ingles" }
        );

        context.SaveChanges();
    }
}

app.Run();