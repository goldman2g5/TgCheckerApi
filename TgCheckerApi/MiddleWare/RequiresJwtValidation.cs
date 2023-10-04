using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TgCheckerApi.Models.BaseModels;
using System.Data.Entity;

namespace TgCheckerApi.MiddleWare
{
    public class RequiresJwtValidation : ActionFilterAttribute
    {
        private readonly string _secret = "GoIdAdObEyTeViZhEvShIh";

        public override async void OnActionExecuting(ActionExecutingContext context)
        {
            var token = context.HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

            if (token == null || !IsValidToken(token, out ClaimsPrincipal claimsPrincipal))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var uniqueKeyClaim = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == "key")?.Value;

            if (string.IsNullOrEmpty(uniqueKeyClaim))
            {
                context.Result = new ForbidResult();
                return;
            }

            context.HttpContext.User = claimsPrincipal;

            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync($"http://localhost:7256/api/Auth/ValidateUniqueKey/{uniqueKeyClaim}");

                if (!response.IsSuccessStatusCode)
                {
                    context.Result = new NotFoundObjectResult("User does not exist");
                    return;
                }
            }

            base.OnActionExecuting(context);
        }

        private bool IsValidToken(string token, out ClaimsPrincipal claimsPrincipal)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));

            if (!tokenHandler.CanReadToken(token))
            {
                claimsPrincipal = null;
                return false;
            }

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = false,
                ValidateAudience = false
            };

            try
            {
                claimsPrincipal = tokenHandler.ValidateToken(token, validationParameters, out var rawValidatedToken);
                return true;
            }
            catch (SecurityTokenException)
            {
                claimsPrincipal = null;
                return false;
            }
        }
    }
}

