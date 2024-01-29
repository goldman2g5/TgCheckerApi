using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using TgCheckerApi.Models.BaseModels;
using Yandex.Checkout.V3;
using TgCheckerApi.MiddleWare;
using TgCheckerApi.Services;
using Microsoft.EntityFrameworkCore;
using TgCheckerApi.Utility;
using TgCheckerApi.Models;
using Serilog;

namespace TgCheckerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly AsyncClient _asyncClient;
        private readonly TgDbContext _context;
        private readonly UserService _userService;
        private readonly SubscriptionService _subscriptionService;
        private readonly YooKassaService _yooKassaService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(TgDbContext context, YooKassaService yooKassaService, ILogger<PaymentController> logger)
        {
            var client = new Client("306141", "test_aFnqFN78UeQ7Hsi-xe5W5Cwcd5IzAJwHF43PsghF45c");
            _asyncClient = client.MakeAsync();
            _context = context;
            _subscriptionService = new SubscriptionService(context);
            _userService = new UserService(context);
            _yooKassaService = yooKassaService;
            _logger = logger;
        }

        public class PaymentRequest
        {
            public decimal Amount { get; set; }
            public string Currency { get; set; }
            public string ReturnUrl { get; set; }
            public string Description { get; set; }

            public int ChannelId { get; set; }

            public int SubType { get; set; }

            public int Duration { get; set; }
            // Include additional fields as necessary
        }

        [BypassApiKey]
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

            bool isPriceValid = false;
            Dictionary<int, PriceDetail> pricing = null;

            switch (paymentRequest.SubType)
            {
                case 1: // Lite
                    pricing = SubscriptionService.LiteSubPricing;
                    break;
                case 2: // Pro
                    pricing = SubscriptionService.ProSubPricing;
                    break;
                case 3: // Super
                    pricing = SubscriptionService.SuperSubPricing;
                    break;
                default:
                    return BadRequest("Invalid subscription type.");
            }

            if (pricing != null && pricing.TryGetValue(paymentRequest.Duration, out PriceDetail priceDetail))
            {
                // Compare the requested amount with the expected price
                // Assuming Amount is the default price or discounted price
                isPriceValid = (paymentRequest.Amount == priceDetail.DefaultPrice) ||
                               (paymentRequest.Amount == priceDetail.DiscountedPrice);
            }

            if (!isPriceValid)
            {
                return BadRequest("Invalid amount for the specified subscription type and duration.");
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
                        CapturedAt = payment.CapturedAt,
                        ExpiresAt = payment.ExpiresAt,
                        AmountValue = payment.Amount.Value,
                        AmountCurrency = payment.Amount.Currency,
                        Description = payment.Description,
                        Capture = payment.Capture,
                        ClientIp = payment.ClientIp,
                        UserId = user.Id,
                        ChannelId = channel.Id,
                        Duration = paymentRequest.Duration,
                        SubtypeId = paymentRequest.SubType,
                        FullJson = JsonConvert.SerializeObject(payment)
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
                var capturedPayment = await _yooKassaService.CapturePaymentAsync(paymentId);
                if (capturedPayment == null)
                {
                    return BadRequest("Failed to capture payment.");
                }

                var paymentToUpdate = await _yooKassaService.UpdatePaymentRecordAsync(paymentId);
                if (paymentToUpdate == null)
                {
                    return NotFound($"Payment with ID {paymentId} not found.");
                }

                if (capturedPayment.Status.ToString() == "Succeeded")
                {
                    return await ProcessSubscriptionAsync(paymentToUpdate);
                }
                else
                {
                    return new JsonResult(new
                    {
                        Status = capturedPayment.Status.ToString(),
                        ChannelId = paymentToUpdate.ChannelId,
                        Message = "Payment capture was not successful."
                    });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private async Task<IActionResult> ProcessSubscriptionAsync(Models.BaseModels.Payment paymentToUpdate)
        {
            string message;

            if (!(paymentToUpdate.SubGiven ?? false))
            {
                var subscriptionResult = await _subscriptionService.HandleSubscription(paymentToUpdate.ChannelId, paymentToUpdate.SubtypeId, paymentToUpdate.Duration, paymentToUpdate.UserId);

                if (subscriptionResult is OkObjectResult okResult)
                {
                    message = okResult.Value.ToString();
                    paymentToUpdate.SubGiven = true;
                    await _context.SaveChangesAsync(); // Consider moving this inside the subscription service
                }
                else
                {
                    return subscriptionResult; // Returns BadRequest or NotFound based on the HandleSubscription method.
                }
            }
            else
            {
                message = "Subscription already given for this payment.";
            }

            return new JsonResult(new
            {
                Status = "Succeeded",
                ChannelId = paymentToUpdate.ChannelId,
                Message = message
            });
        }

        public class YookassaWebhookEvent
        {
            public string Id { get; set; }
            public string Event { get; set; }
            public string Url { get; set; }

        }

        [HttpPost("Webhook")]
        public async Task<IActionResult> Receive()
        {
            var body = new byte[Request.ContentLength ?? 0];
            _ = await Request.Body.ReadAsync(body);
            var payment = _yooKassaService.DecodeWebhookRequest(
                Request.Method, Request.ContentType!, new MemoryStream(body));
            _logger.LogInformation($"{payment.Id}, {payment.Status}");
            if (payment.Status == PaymentStatus.WaitingForCapture)
            {
                await _yooKassaService.CapturePaymentAsync(payment.Id);
            }

            return Ok();
        }

        private async Task<Channel> FindChannelById(int id)
        {
            return await _context.Channels.FindAsync(id);
        }
    }

    
}