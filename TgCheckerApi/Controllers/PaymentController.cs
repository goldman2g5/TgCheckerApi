using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using TgCheckerApi.Models.BaseModels;
using Yandex.Checkout.V3;
using TgCheckerApi.MiddleWare;
using TgCheckerApi.Services;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;

namespace TgCheckerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly AsyncClient _asyncClient;
        private readonly TgDbContext _context;
        private readonly UserService _userService;

        public PaymentController(TgDbContext context)
        {
            var client = new Client("306141", "test_aFnqFN78UeQ7Hsi-xe5W5Cwcd5IzAJwHF43PsghF45c");
            _asyncClient = client.MakeAsync();
            _context = context;
            _userService = new UserService(context);
        }

        public class PaymentRequest
        {
            public decimal Amount { get; set; }
            public string Currency { get; set; }
            public string ReturnUrl { get; set; }
            public string Description { get; set; }

            public int ChannelId { get; set; }
            // Include additional fields as necessary
        }

        [RequiresJwtValidation]
        [HttpPost]
        public async Task<IActionResult> CreatePaymentAsync([FromBody] PaymentRequest paymentRequest)
        {
            var uniqueKeyClaim = User.FindFirst(c => c.Type == "key")?.Value;

            var user = await _userService.GetUserWithRelations(uniqueKeyClaim);

            var channel = await _context.Channels.FindAsync(paymentRequest.ChannelId);
            if (channel == null)
            {
                return NotFound();
            }

            if (!_userService.UserHasAccessToChannel(user, channel))
            {
                return Unauthorized();
            }

            // Create an instance of NewPayment class with necessary details
            var newPayment = new NewPayment
            {
                Amount = new Amount
                {
                    Value = paymentRequest.Amount,
                    Currency = paymentRequest.Currency
                },
                Confirmation = new Confirmation
                {
                    Type = ConfirmationType.Redirect,
                    ReturnUrl = paymentRequest.ReturnUrl
                },
                Description = paymentRequest.Description
                // Add other fields based on your requirements
            };

            // Try creating a payment asynchronously
            try
            {
                var payment = await _asyncClient.CreatePaymentAsync(newPayment);

                // Check if confirmation URL is available
                if (payment.Confirmation?.ConfirmationUrl != null)
                {
                    // Create a new Payment entity and populate it with data from 'payment'
                    var paymentRecord = new Models.BaseModels.Payment
                    {
                        Id = Guid.Parse(payment.Id),
                        Status = payment.Status.ToString(),
                        Paid = payment.Paid,
                        CreatedAt = payment.CreatedAt,
                        CapturedAt = payment.CapturedAt, // Assuming these are DateTime?
                        ExpiresAt = payment.ExpiresAt,
                        AmountValue = payment.Amount.Value,
                        AmountCurrency = payment.Amount.Currency,
                        Description = payment.Description,
                        Capture = payment.Capture,
                        ClientIp = payment.ClientIp,
                        UserId = user.Id, // Hardcoded for example; replace with actual data
                        ChannelId = channel.Id, // Hardcoded for example; replace with actual data
                        FullJson = JsonConvert.SerializeObject(payment) // Serialize the full payment object

                        // ... Populate other fields ...
                    };

                    // Insert the new record into the database
                    _context.Payments.Add(paymentRecord);
                    await _context.SaveChangesAsync();

                    return Ok(new { PaymentId = payment.Id, RedirectUrl = payment.Confirmation.ConfirmationUrl });
                }
                else
                {
                    return BadRequest("Confirmation URL is not available.");
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("capture/{paymentId}")]
        public async Task<IActionResult> CapturePaymentAsync(string paymentId)
        {
            try
            {
                // Capture the payment asynchronously
                var capturedPayment = await _asyncClient.CapturePaymentAsync(paymentId);

                if (capturedPayment != null)
                {
                    // Find the payment in the database
                    var paymentToUpdate = await _context.Payments.FirstOrDefaultAsync(p => p.Id == Guid.Parse(paymentId));
                    if (paymentToUpdate != null)
                    {
                        // Update the necessary fields in the payment record
                        paymentToUpdate.Status = capturedPayment.Status.ToString();
                        paymentToUpdate.CapturedAt = DateTime.UtcNow; // or use capturedPayment.CapturedAt if available
                        paymentToUpdate.Capture = true; // Assuming capture is successful
                        paymentToUpdate.CaptureJson = JsonConvert.SerializeObject(capturedPayment); // Serialize the captured payment details

                        _context.Payments.Update(paymentToUpdate);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        return NotFound($"Payment with ID {paymentId} not found.");
                    }
                }
                else
                {
                    return BadRequest("Failed to capture payment.");
                }

                // Return a JSON object with the captured payment details
                return new JsonResult(capturedPayment);
            }
            catch (Exception ex)
            {
                // Handle any exceptions and return an appropriate message or error code.
                return BadRequest(ex.Message);
            }
        }
    }

    
}