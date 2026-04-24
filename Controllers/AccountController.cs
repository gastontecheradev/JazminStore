using Jazmin.Data;
using Jazmin.Models;
using Jazmin.Models.ViewModels;
using Jazmin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Jazmin.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userMgr;
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly IEmailService _email;
    private readonly ICartService _cart;
    private readonly IConfiguration _config;

    public AccountController(
        UserManager<ApplicationUser> userMgr,
        SignInManager<ApplicationUser> signIn,
        IEmailService email,
        ICartService cart,
        IConfiguration config)
    {
        _userMgr = userMgr;
        _signIn = signIn;
        _email = email;
        _cart = cart;
        _config = config;
    }

    // ---------- LOGIN ----------
    [HttpGet, AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        ViewData["ReturnUrl"] = vm.ReturnUrl;
        if (!ModelState.IsValid) return View(vm);

        var result = await _signIn.PasswordSignInAsync(vm.Email, vm.Password, vm.RememberMe, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            var user = await _userMgr.FindByEmailAsync(vm.Email);
            if (user != null) await _cart.MergeGuestCartOnLoginAsync(HttpContext, user.Id);

            if (user != null && await _userMgr.IsInRoleAsync(user, DbSeeder.AdminRole))
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });

            if (!string.IsNullOrEmpty(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
                return Redirect(vm.ReturnUrl);
            return RedirectToAction("Index", "Home");
        }
        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Cuenta bloqueada. Intentá nuevamente más tarde.");
            return View(vm);
        }
        ModelState.AddModelError(string.Empty, "Email o contraseña incorrectos.");
        return View(vm);
    }

    // ---------- REGISTER ----------
    [HttpGet, AllowAnonymous]
    public IActionResult Register(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new RegisterViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel vm)
    {
        ViewData["ReturnUrl"] = vm.ReturnUrl;
        if (!ModelState.IsValid) return View(vm);

        var user = new ApplicationUser
        {
            UserName = vm.Email,
            Email = vm.Email,
            FullName = vm.FullName,
            Phone = vm.Phone,
            EmailConfirmed = true
        };
        var result = await _userMgr.CreateAsync(user, vm.Password);
        if (result.Succeeded)
        {
            await _userMgr.AddToRoleAsync(user, DbSeeder.CustomerRole);

            // Welcome email
            var siteName = _config["Site:Name"] ?? "JAZMÍN";
            var html = $@"
<div style='font-family:Arial,sans-serif;max-width:520px;margin:auto;color:#1E2A1A;background:#FFF8F8;padding:32px'>
    <h1 style='font-family:Georgia,serif;color:#3A5230;letter-spacing:4px'>{siteName}</h1>
    <h2 style='color:#3A5230'>¡Bienvenida, {vm.FullName}!</h2>
    <p>Tu cuenta fue creada con éxito. Ya podés empezar a comprar.</p>
    <p style='color:#9A6070;font-size:13px;margin-top:32px'>Un abrazo ♥</p>
</div>";
            _ = _email.SendAsync(vm.Email, $"Bienvenida a {siteName}", html);

            await _signIn.SignInAsync(user, isPersistent: false);
            await _cart.MergeGuestCartOnLoginAsync(HttpContext, user.Id);

            if (!string.IsNullOrEmpty(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
                return Redirect(vm.ReturnUrl);
            return RedirectToAction("Index", "Home");
        }

        foreach (var err in result.Errors)
            ModelState.AddModelError(string.Empty, TranslateIdentityError(err));
        return View(vm);
    }

    // ---------- LOGOUT ----------
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    // ---------- PROFILE ----------
    [HttpGet, Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await _userMgr.GetUserAsync(User);
        if (user == null) return NotFound();
        return View(new ProfileViewModel
        {
            FullName = user.FullName,
            Email = user.Email ?? "",
            Phone = user.Phone,
            Address = user.Address,
            City = user.City,
            Department = user.Department,
            PostalCode = user.PostalCode
        });
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var user = await _userMgr.GetUserAsync(User);
        if (user == null) return NotFound();

        user.FullName = vm.FullName;
        user.Phone = vm.Phone;
        user.Address = vm.Address;
        user.City = vm.City;
        user.Department = vm.Department;
        user.PostalCode = vm.PostalCode;

        var result = await _userMgr.UpdateAsync(user);
        if (result.Succeeded)
        {
            TempData["ProfileMessage"] = "Datos actualizados ✓";
            return RedirectToAction(nameof(Profile));
        }
        foreach (var err in result.Errors) ModelState.AddModelError(string.Empty, err.Description);
        return View(vm);
    }

    // ---------- CHANGE PASSWORD ----------
    [HttpGet, Authorize]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var user = await _userMgr.GetUserAsync(User);
        if (user == null) return NotFound();

        var result = await _userMgr.ChangePasswordAsync(user, vm.OldPassword, vm.NewPassword);
        if (result.Succeeded)
        {
            await _signIn.RefreshSignInAsync(user);
            TempData["ProfileMessage"] = "Contraseña actualizada ✓";
            return RedirectToAction(nameof(Profile));
        }
        foreach (var err in result.Errors) ModelState.AddModelError(string.Empty, TranslateIdentityError(err));
        return View(vm);
    }

    // ---------- FORGOT PASSWORD ----------
    [HttpGet, AllowAnonymous]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var user = await _userMgr.FindByEmailAsync(vm.Email);
        // Always pretend success (avoid email enumeration)
        if (user != null)
        {
            var token = await _userMgr.GeneratePasswordResetTokenAsync(user);
            var callback = Url.Action(nameof(ResetPassword), "Account",
                new { email = vm.Email, token }, Request.Scheme)!;

            var html = $@"
<div style='font-family:Arial,sans-serif;max-width:520px;margin:auto;color:#1E2A1A;background:#FFF8F8;padding:32px'>
    <h1 style='font-family:Georgia,serif;color:#3A5230;letter-spacing:4px'>JAZMÍN</h1>
    <h2 style='color:#3A5230'>Restablecer tu contraseña</h2>
    <p>Hiciste una solicitud para restablecer tu contraseña.</p>
    <p><a href='{callback}' style='display:inline-block;background:#3A5230;color:white;padding:12px 24px;border-radius:24px;text-decoration:none'>Restablecer contraseña</a></p>
    <p style='color:#9A6070;font-size:12px'>Si no pediste esto, ignorá este email.</p>
</div>";
            await _email.SendAsync(vm.Email, "Restablecer contraseña - JAZMÍN", html);
        }
        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [HttpGet, AllowAnonymous]
    public IActionResult ForgotPasswordConfirmation() => View();

    [HttpGet, AllowAnonymous]
    public IActionResult ResetPassword(string email, string token)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token)) return BadRequest();
        return View(new ResetPasswordViewModel { Email = email, Token = token });
    }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var user = await _userMgr.FindByEmailAsync(vm.Email);
        if (user == null) return RedirectToAction(nameof(ResetPasswordConfirmation));

        var result = await _userMgr.ResetPasswordAsync(user, vm.Token, vm.Password);
        if (result.Succeeded) return RedirectToAction(nameof(ResetPasswordConfirmation));

        foreach (var err in result.Errors) ModelState.AddModelError(string.Empty, TranslateIdentityError(err));
        return View(vm);
    }

    [HttpGet, AllowAnonymous]
    public IActionResult ResetPasswordConfirmation() => View();

    [HttpGet, AllowAnonymous]
    public IActionResult AccessDenied() => View();

    private static string TranslateIdentityError(IdentityError err) => err.Code switch
    {
        "DuplicateUserName" => "Ya existe una cuenta con ese email.",
        "DuplicateEmail" => "Ya existe una cuenta con ese email.",
        "PasswordTooShort" => "La contraseña es muy corta.",
        "PasswordRequiresDigit" => "La contraseña debe incluir al menos un número.",
        "PasswordRequiresLower" => "La contraseña debe incluir una minúscula.",
        "PasswordMismatch" => "La contraseña actual es incorrecta.",
        "InvalidToken" => "El enlace ya no es válido. Pedí uno nuevo.",
        _ => err.Description
    };
}
