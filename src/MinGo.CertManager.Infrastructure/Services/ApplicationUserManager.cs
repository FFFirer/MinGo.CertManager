using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MinGo.CertManager.Core.Constants;
using MinGo.CertManager.Infrastructure.Data;

namespace MinGo.CertManager.Infrastructure.Services;

/// <summary>
/// 自定义 UserManager，在创建用户时自动赋予 "User" 角色。
/// 确保所有通过 CreateAsync 创建的用户都默认拥有普通用户角色。
/// </summary>
public class ApplicationUserManager : UserManager<ApplicationUser>
{
    public ApplicationUserManager(
        IUserStore<ApplicationUser> store,
        IOptions<IdentityOptions> optionsAccessor,
        IPasswordHasher<ApplicationUser> passwordHasher,
        IEnumerable<IUserValidator<ApplicationUser>> userValidators,
        IEnumerable<IPasswordValidator<ApplicationUser>> passwordValidators,
        ILookupNormalizer keyNormalizer,
        IdentityErrorDescriber errors,
        IServiceProvider services,
        ILogger<ApplicationUserManager> logger)
        : base(store, optionsAccessor, passwordHasher, userValidators, passwordValidators,
              keyNormalizer, errors, services, logger)
    {
    }

    /// <summary>
    /// 创建用户后自动赋予 "User" 角色。
    /// Admin 用户也会先获得 "User" 角色，再通过调用方显式添加 "Admin" 角色。
    /// </summary>
    public override async Task<IdentityResult> CreateAsync(ApplicationUser user)
    {
        var result = await base.CreateAsync(user);
        if (result.Succeeded)
        {
            await AddToRoleAsync(user, RoleConstants.User);
        }
        return result;
    }
}
