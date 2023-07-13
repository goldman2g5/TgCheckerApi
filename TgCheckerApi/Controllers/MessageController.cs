using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
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
            string token = GenerateUniqueToken();

            SendMessageResponse response = new SendMessageResponse()
            {
                Token = token,
                Username = payload.Username,
                UserId = payload.UserId
            };

            await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveMessage", JsonConvert.SerializeObject(response));
            return Ok();
        }

        public class TokenPayload
        {
            public string EncryptedToken { get; set; }
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

        private string GenerateUniqueToken()
        {
            string token = GenerateRandomToken();

            // Check if the generated token is unique
            while (!IsTokenUnique(token))
            {
                // If the token is not unique, generate a new one
                token = GenerateRandomToken();
            }

            return token;
        }

        private string GenerateRandomToken()
        {
            // Generate a random string using a combination of characters or numbers
            string characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            Random random = new Random();
            char[] tokenChars = new char[32];

            for (int i = 0; i < tokenChars.Length; i++)
            {
                tokenChars[i] = characters[random.Next(characters.Length)];
            }

            return new string(tokenChars);
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
