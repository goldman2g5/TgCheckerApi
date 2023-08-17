using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using TgCheckerApi.Models.BaseModels;
using TgCheckerApi.Websockets;

namespace TgCheckerApi.Controllers
{
    
    [Route("api/[controller]")]
    [ApiController]
    public class MessageController : ControllerBase
    {
        private readonly IHubContext<ChatHub> _hubContext;

        public MessageController(IHubContext<ChatHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public class SendMessagePayload
        {
            public string Username { get; set; }
            public int UserId { get; set; }
        }

        public class SendMessageResponse
        {
            public string Token { get; set; }
            public string Username { get; set; }
            public int UserId { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromQuery] string connectionId, [FromBody] SendMessagePayload payload)
        {
            string token = CreateToken(payload);

            SendMessageResponse response = new SendMessageResponse()
            {
                Token = token,
                Username = payload.Username,
                UserId = payload.UserId
            };

            await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveMessage", JsonConvert.SerializeObject(response));
            return Ok(token);
        }

        public class TokenPayload
        {
            public string EncryptedToken { get; set; }
        }

        private string CreateToken(SendMessagePayload payload)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, payload.Username),
                new Claim("userId", payload.UserId.ToString())
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("super secret key"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var token = new JwtSecurityToken(
                 claims: claims,
                 expires: DateTime.Now.AddDays(7),
                 signingCredentials: creds
             );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using var hmac = new HMACSHA512();
            passwordSalt = hmac.Key;
            passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        }

        private bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
        {
            using var hmac = new HMACSHA512(passwordSalt);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            return computedHash.SequenceEqual(passwordHash);
        }

        [HttpPost("DecryptToken")]
        public IActionResult DecryptToken([FromBody] TokenPayload payload)
        {
            try
            {
                Tuple<string, string, string> decryptedToken = Encryptor.DecryptToken(payload.EncryptedToken);

                return Ok(new
                {
                    UserId = decryptedToken.Item2,
                    Username = decryptedToken.Item3
                });
            }
            catch (Exception ex)
            {
                // Handle decryption error
                return BadRequest("Invalid token");
            }
        }

        private bool IsTokenUnique(string token)
        {
            // Check if the token already exists in your system
            // Implement your logic to verify the uniqueness of the token
            // You might need to query your database or use another method
            // to ensure the generated token is unique.

            // Example check (pseudo-code):
            // return !_tokenRepository.Exists(token);

            // Make sure to replace the above example with your own implementation.
            return true; // Change this line accordingly
        }
    }
}
