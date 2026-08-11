using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using LivestockManager.Infrastructure.Identity;
using LivestockManager.Web.Models.AccountViewModels;

namespace LivestockManager.Web.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        model.ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var credential = (model.UserName ?? string.Empty).Trim();

        ApplicationUser? user = null;
        if (!string.IsNullOrWhiteSpace(credential))
        {
            user = await _userManager.FindByNameAsync(credential);
            if (user == null && credential.Contains('@'))
            {
                user = await _userManager.FindByEmailAsync(credential);
            }
        }

        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        if (!user.IsEnabled)
        {
            ModelState.AddModelError(string.Empty, "This account has been disabled. Please contact your administrator.");
            return View(model);
        }

        var signInUser = user.UserName ?? credential;
        var result = await _signInManager.PasswordSignInAsync(
            signInUser,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            user.LastLoginAt = DateTimeOffset.UtcNow;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("User logged in: {UserName}", user.UserName);
            return RedirectToLocal(returnUrl);
        }
        if (result.IsLockedOut)
        {
            _logger.LogWarning("User account locked out: {UserName}", user.UserName);
            ModelState.AddModelError(string.Empty, "This account has been locked out. Please try again later.");
            return View(model);
        }
        else
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(string? returnUrl = null)
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out.");

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction(nameof(HomeController.Index), "Home");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return RedirectToAction(nameof(HomeController.AccessDenied), "Home");
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction(nameof(HomeController.Index), "Home");
    }
}
