using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using Yandex.Checkout.V3;

namespace TgCheckerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly AsyncClient _asyncClient;

        public PaymentController()
        {
            var client = new Client("306141", "test_aFnqFN78UeQ7Hsi-xe5W5Cwcd5IzAJwHF43PsghF45c");
            _asyncClient = client.MakeAsync();
        }

        public class PaymentRequest
        {
            public decimal Amount { get; set; }
            public string Currency { get; set; }
            public string ReturnUrl { get; set; }
            public string Description { get; set; }
            // Include additional fields as necessary
        }

        [HttpPost]
        public async Task<IActionResult> CreatePaymentAsync([FromBody] PaymentRequest paymentRequest)
        {
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

                // Instead of redirecting, return a JSON object with the payment details
                return new JsonResult(payment);
            }
            catch (Exception ex)
            {
                // Handle any exceptions and return an appropriate message or error code.
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