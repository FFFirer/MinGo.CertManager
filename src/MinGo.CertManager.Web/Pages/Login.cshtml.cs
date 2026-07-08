using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MinGo.CertManager.Infrastructure.Data;

namespace MinGo.CertManager.Web.Pages;

public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILogger<LoginModel> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required(AllowEmptyStrings = false, ErrorMessage = "请输入邮箱或用户名")]
        [Display(Name = "邮箱 / 用户名")]
        public string Email { get; set; } = "";

        [Required(AllowEmptyStrings = false, ErrorMessage = "请输入密码")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [Display(Name = "记住我")]
        public bool RememberMe { get; set; }
    }

    public async Task OnGetAsync(string? returnUrl = null)
    {
        if (!string.IsNullOrEmpty(ErrorMessage))
        {
            ModelState.AddModelError(string.Empty, ErrorMessage);
        }

        returnUrl ??= Url.Content("~/");

        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        if (ModelState.IsValid)
        {
            // 先用邮箱查找，再用用户名查找（兼容旧数据）
            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                user = await _userManager.FindByNameAsync(Input.Email);
            }

            if (user != null)
            {
                var result = await _signInManager.PasswordSignInAsync(
                    user.UserName!,
                    Input.Password,
                    Input.RememberMe,
                    lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User {Email} logged in successfully", Input.Email);
                    return LocalRedirect(returnUrl);
                }

                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User {Email} account locked out", Input.Email);
                    ModelState.AddModelError(string.Empty, "账户已被锁定，请稍后再试。");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "密码错误。");
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "账号不存在。");
            }
        }

        return Page();
    }
}
