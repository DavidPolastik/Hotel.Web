using Hotel.Infrastructure.Data; 
using Hotel.Application.Abstraction;
using Hotel.Application.Services; 
using Hotel.Domain.Entities.Interfaces; 
using Hotel.Infrastructure.Repositories; 
using Hotel.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity; 
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

var cultureInfo = new CultureInfo("cs-CZ");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;


// Connection String pro MySQL
string connectionString = builder.Configuration.GetConnectionString("MySQL");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 21))));

// Registrace repozitářů
builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<ICartItemRepository, CartItemRepository>();

// Registrace služeb
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<ICartService, CartService>();



builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);




// Registrace PasswordHasher
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();




// Přidání autentizace
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; // Stránka pro přihlášení
        options.AccessDeniedPath = "/Account/AccessDenied"; // Stránka při zamítnutém přístupu
    });

// Přidání správy košíku pomocí Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor();

// Přidání MVC (Controllers + Views)
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Vytvoření administrátora při spuštění aplikace
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

    // Ujistěte se, že databáze existuje
    context.Database.EnsureCreated();

    // Zkontrolujte, zda admin uživatel již existuje
    if (!context.Users.Any(u => u.Email == "admin@admin.cz"))
    {
        var adminUser = new User
        {
            Name = "Administrator",
            Email = "admin@admin.cz",
            Role = "Admin"
        };

        // Nastavení hashovaného hesla
        adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, "123");

        // Přidání uživatele do databáze
        context.Users.Add(adminUser);
        context.SaveChanges();
    }
}

// Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession(); // Middleware pro správu Session

// Middleware pro autentizaci a autorizaci
app.UseAuthentication();
app.UseAuthorization();

// Mapování rout
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

app.UseEndpoints(endpoints => endpoints.MapControllers());

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Unhandled Exception: {ex.Message}");
        throw;
    }
});

