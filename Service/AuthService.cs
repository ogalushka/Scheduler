using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.Options;
using Scheduler.Db;
using Scheduler.Db.Models;
using System.Security.Claims;

namespace Scheduler.Service
{
    public class AuthService
    {
        private const string ClaimIdName = "Id";
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IRepository<Guid, UserEntity> userRepository;

        public AuthService(IHttpContextAccessor httpContextAccessor, IRepository<Guid, UserEntity> userRepository)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.userRepository = userRepository;
        }

        public async Task HandleFacebookAuthOnCreateTicket(OAuthCreatingTicketContext ctx)
        {
            var authHandlerProvider = ctx.HttpContext.RequestServices.GetRequiredService<IAuthenticationHandlerProvider>();
            var authHandler = await authHandlerProvider.GetHandlerAsync(ctx.HttpContext, CookieAuthenticationDefaults.AuthenticationScheme);
            var authResult = await authHandler!.AuthenticateAsync();
            if (authResult.Succeeded)
            {
                var identity = ctx.Principal!.Identities.First(i => i.AuthenticationType == FacebookDefaults.AuthenticationScheme);
                identity.AddClaims(authResult.Principal.Claims);
            }
        }

        public async Task HandleGoogleAuthOnCreateTicket(OAuthCreatingTicketContext ctx) {
            var googleClaims = ctx.Principal!.Identities.First(i => i.AuthenticationType == GoogleDefaults.AuthenticationScheme).Claims;
            var googleId = googleClaims.First(c => c.Type == ClaimTypes.NameIdentifier).Value;
            var googleMail = googleClaims.First(c => c.Type == ClaimTypes.Email).Value;

            var userId = Guid.Empty;

            var googleAuthUser = await userRepository.Get(u => u.GoogleId == googleId);

            // google id has account associated already
            if (googleAuthUser != null)
            {
                ctx.Principal = Convert(googleAuthUser);
                return;
            }

            var authHandlerProvider = ctx.HttpContext.RequestServices.GetRequiredService<IAuthenticationHandlerProvider>();
            var authHandler = await authHandlerProvider.GetHandlerAsync(ctx.HttpContext, CookieAuthenticationDefaults.AuthenticationScheme);
            var authResult = await authHandler!.AuthenticateAsync();

            if (authResult.Succeeded)
            {
                userId = Guid.Parse(authResult.Principal.Claims.First(c => c.Type == ClaimIdName).Value);
            }
            else
            {
                userId = Guid.NewGuid();
            }

            var user = await userRepository.Get(userId);
            if (user == null)
            {
                user = CreateUser(userId);
                user.GoogleId = googleId;
                user.GoogleMail = googleMail;
                await userRepository.Create(user);
            }
            else
            {
                user.GoogleId = googleId;
                user.GoogleMail = googleMail;
                await userRepository.Update(user);
            }
            ctx.Principal = Convert(user);
        }

        public async Task<UserEntity> GetAndSaveUser()
        {
            var user = await GetLoggedInUser();
            if (!user.SavedUser)
            {
                await SaveUser(user);
            }

            return user;
        }

        public async Task<UserEntity> GetLoggedInUser()
        {
            var userId = await GetLoggedInUserId();
            if (userId == null)
            {
                var newUser = CreateUser();
                await httpContextAccessor.HttpContext!.SignInAsync(Convert(newUser));
                return newUser;
            }

            var user = await userRepository.Get(userId.Value);
            if (user == null)
            {
                user = CreateUser(userId);
            }
            else
            {
                user.SavedUser = true;
            }

            return user;
        }

        public UserEntity CreateUser(Guid? userId = null)
        {
            var userEntity = new UserEntity();

            userEntity.PublicId = Guid.NewGuid();
            userEntity.Id = userId == null ? Guid.NewGuid() : userId.Value;
            userEntity.Attending = new List<Guid>();
            userEntity.Following = new List<Guid>();
            userEntity.SavedUser = false;

            return userEntity;
        }

        private async Task<UserEntity> SaveUser(UserEntity userEntity)
        {
            if (userEntity.PublicId == Guid.Empty)
            {
                userEntity.PublicId = Guid.NewGuid();
            }

            await userRepository.Create(userEntity);
            userEntity.SavedUser = true;

            return userEntity;
        }

        private async Task<Guid?> GetLoggedInUserId()
        {
            var claim = httpContextAccessor.HttpContext!.User.Claims.FirstOrDefault(kv => kv.Type == ClaimIdName);
            if (claim == null)
            {
                return null;
            }

            if (!Guid.TryParse(claim.Value, out var id))
            {
                return null;
            }

            await RenewSessionExpiration(httpContextAccessor.HttpContext.User);

            return id;
        }

        private async Task RenewSessionExpiration(ClaimsPrincipal claimsPrincipal)
        {
            var opt = httpContextAccessor.HttpContext!.RequestServices
                .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
                .Get(CookieAuthenticationDefaults.AuthenticationScheme);

            var cookie = opt.CookieManager.GetRequestCookie(httpContextAccessor.HttpContext!, opt.Cookie.Name!);

            var authTicket = opt.TicketDataFormat.Unprotect(cookie);
            if (authTicket == null)
            {
                return;
            }

            var timeTillExpiration = authTicket.Properties.ExpiresUtc - DateTime.UtcNow;
            if (timeTillExpiration < TimeSpan.FromDays(30))
            {
                await httpContextAccessor.HttpContext!.SignInAsync(claimsPrincipal);
            }
        }

        public static ClaimsPrincipal Convert(UserEntity user)
        {
            var claims = new List<Claim>()
            {
                new Claim(ClaimIdName, user.Id.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            return new ClaimsPrincipal(identity);
        }
    }
}
