using System;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Silapa.Controllers;
using Silapa.Models;
using iText.Layout;
using Syncfusion.Licensing;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http.Features;
if (File.Exists(Directory.GetCurrentDirectory() + "/wwwroot/SyncfusionLicense.txt"))
{
    string licenseKey = File.ReadAllText(Directory.GetCurrentDirectory() + "/wwwroot/SyncfusionLicense.txt").Trim();
    SyncfusionLicenseProvider.RegisterLicense(licenseKey);
    if (File.Exists(Directory.GetCurrentDirectory() + "/wwwroot/scripts/index.js"))
    {
        string regexPattern = "ej.base.registerLicense(.*);";
        string jsContent = File.ReadAllText(Directory.GetCurrentDirectory() + "/wwwroot/scripts/index.js");
        MatchCollection matchCases = Regex.Matches(jsContent, regexPattern);
        foreach (Match matchCase in matchCases)
        {
            var replaceableString = matchCase.ToString();
            jsContent = jsContent.Replace(replaceableString, "ej.base.registerLicense('" + licenseKey + "');");
        }
        File.WriteAllText(Directory.GetCurrentDirectory() + "/wwwroot/scripts/index.js", jsContent);
    }
}
var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure Entity Framework to use MySQL
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(10, 11, 10)) // Specify the MySQL version here
    ));
// If using Identity, configure Identity as well
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    options.SignIn.RequireConfirmedAccount = false)
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();
// Configure the application cookie settings
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";   // Redirect path for unauthenticated users
    options.LogoutPath = "/Account/Logout";
});

builder.Services.AddDbContextFactory<ApplicationDbContext>();


builder.Services.AddRazorPages();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
});
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 100 * 1024 * 1024; // 10 MB
});
// เพิ่มบริการ SignalR
builder.Services.AddSignalR();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    // ตั้งเวลาหมดอายุของ Session (เช่น 20 นาทีถ้าไม่ใช้งาน)
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error"); // Redirect to error page for production
    app.UseHsts();
}

app.UseHttpsRedirection();
//app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication(); // Ensure this is before UseAuthorization
app.UseAuthorization();

app.UseCors();

app.UseSession();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var roles = new[] { "Admin", "Manager", "Member" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }
}
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    string email = "tunyapisit.ons@jv.ac.th";
    string password = "Tun@123";
    if (await userManager.FindByEmailAsync(email) == null)
    {
        var user = new ApplicationUser();
        user.UserName = email;
        user.Email = email;
        // user.EmailConfirmed= false;
        user.titlename = "นาย";
        user.FirstName = "ธัญพิสิษฐ์";
        user.LastName = "อ่อนศรี";

        await userManager.CreateAsync(user, password);
        await userManager.AddToRoleAsync(user, "Admin");


    }
}
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // ดึง path ของไฟล์ที่กำลังถูกเรียก
        var path = ctx.Context.Request.Path.Value?.ToLower();

        // 1. ถ้าเป็นไฟล์รูปภาพที่อัปโหลด (เช่น อยู่ในโฟลเดอร์ /images/)
        // ให้ "ห้ามจำ Cache" (เพื่อให้เห็นรูปใหม่เสมอ)
        if (path.StartsWith("/images"))
        {
            ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
            ctx.Context.Response.Headers.Append("Pragma", "no-cache");
            ctx.Context.Response.Headers.Append("Expires", "0");
        }
        // 2. ถ้าเป็นไฟล์ระบบอื่นๆ (CSS, JS, Libs) 
        // ให้ "จำ Cache นานๆ" (เพื่อความเร็ว)
        else
        {
            ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=31536000");
        }
    }
});

// กำหนดเส้นทางสำหรับ SignalR
app.MapHub<ResultsHub>("/resultsHub");

// เพิ่มโค้ดส่วนนี้เข้าไป
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    // ใช้ MigrateAsync() สำหรับเวอร์ชัน async
    await dbContext.Database.MigrateAsync();
}
app.Run();
