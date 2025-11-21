using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Threading.Tasks;
using ClaimsManagementApp.Models;
using ClaimsManagementApp.Services;

namespace ClaimsManagementApp.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        public IActionResult Login(string role = "")
        {
            var model = new LoginViewModel();
            ViewBag.Role = role;
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = _authService.Login(model.Username, model.Password);
                if (user != null)
                {
                    // Use fully qualified name for Claims
                    var claims = new[]
                    {
                        new System.Security.Claims.Claim(ClaimTypes.Name, user.Username),
                        new System.Security.Claims.Claim(ClaimTypes.Email, user.Email),
                        new System.Security.Claims.Claim(ClaimTypes.Role, user.Role),
                        new System.Security.Claims.Claim("UserId", user.Id.ToString())
                    };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                    // Redirect based on role - UPDATED LOGIC
                    return user.Role.ToLower() switch
                    {
                        "coordinator" => RedirectToAction("PendingClaims", "Coordinator"),
                        "manager" => RedirectToAction("PendingClaims", "Manager"),
                        "hr" => RedirectToAction("Dashboard", "HR"),
                        "lecturer" => RedirectToAction("Index", "Lecturer"),
                        _ => RedirectToAction("Index", "Home")
                    };
                }

                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new User
                {
                    Username = model.Username,
                    Email = model.Email
                };

                if (_authService.Register(user, model.Password))
                {
                    // Auto-login after registration
                    var loginModel = new LoginViewModel
                    {
                        Username = model.Username,
                        Password = model.Password
                    };
                    return await Login(loginModel);
                }

                ModelState.AddModelError(string.Empty, "Registration failed. Username or email may already exist.");
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}