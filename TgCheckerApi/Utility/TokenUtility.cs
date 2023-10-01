using Microsoft.IdentityModel.Tokens;
using System.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TgCheckerApi.Models;

namespace TgCheckerApi.Utility
{
    public static class TokenUtility
    {
        public static string CreateToken(SendMessagePayload payload)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, payload.Username),
                new Claim("userId", payload.UserId.ToString()),
                new Claim("key",  payload.Unique_key) 
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("GoIdAdObEyTeViZhEvShIh"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var token = new JwtSecurityToken(
                 claims: claims,
                 expires: DateTime.Now.AddDays(7),
                 signingCredentials: creds
             );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }
}
