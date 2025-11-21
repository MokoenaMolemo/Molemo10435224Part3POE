using Microsoft.AspNetCore.Authentication.Cookies;
using ClaimsManagementApp.Services;

var builder = WebApplication.CreateBuilder(args);

// services to the container.
builder.Services.AddControllersWithViews();

//  authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Home/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
    });

// Register 
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IClaimService, ClaimService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IReportService, ReportService>(); // Add this line

var app = builder.Build();

// Configure the HTTP request pipeline.
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

app.Run();
