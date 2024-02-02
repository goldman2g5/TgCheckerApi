using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using TgCheckerApi.Models;
using TgCheckerApi.Models.BaseModels;
using Yandex.Checkout.V3;

namespace TgCheckerApi.Services
{
    public class YooKassaService
    {
        private readonly TgDbContext _context;    // Replace with your actual DbContext
        private readonly AsyncClient _asyncClient;

        public YooKassaService(TgDbContext context)
        {
            var client = new Client("306141", "test_aFnqFN78UeQ7Hsi-xe5W5Cwcd5IzAJwHF43PsghF45c");
            _asyncClient = client.MakeAsync();
            _context = context;
        }

        public async Task<Yandex.Checkout.V3.Payment> CapturePaymentAsync(string paymentId)
        {
            var capturedPayment = await _asyncClient.CapturePaymentAsync(paymentId);
            // Add any additional logic if needed
            return capturedPayment;
        }
            
        public async Task<Models.BaseModels.Payment> UpdatePaymentRecordAsync(string paymentId, Yandex.Checkout.V3.Payment capturedPayment)
        {
            if (capturedPayment == null) return null;

            var paymentToUpdate = await _context.Payments
                                    .Include(p => p.Channel) // Include navigation property
                                    .FirstOrDefaultAsync(p => p.Id == Guid.Parse(paymentId));

            if (paymentToUpdate != null)
            {
                // Update the necessary fields in the payment record
                paymentToUpdate.Status = capturedPayment.Status.ToString();
                paymentToUpdate.CapturedAt = capturedPayment.CapturedAt;
                paymentToUpdate.ExpiresAt = capturedPayment.ExpiresAt;
                paymentToUpdate.Capture = capturedPayment.Capture;
                paymentToUpdate.Paid = capturedPayment.Paid;
                paymentToUpdate.CaptureJson = JsonConvert.SerializeObject(capturedPayment);

                _context.Payments.Update(paymentToUpdate);
                await _context.SaveChangesAsync();
            }

            return paymentToUpdate;
        }

        public Yandex.Checkout.V3.Payment DecodeWebhookRequest(
            string requestMethod, string requestContentType, Stream requestBody)
        {
            if (requestMethod == null) throw new ArgumentNullException(nameof(requestMethod));
            if (requestContentType == null) throw new ArgumentNullException(nameof(requestContentType));
            if (requestBody == null) throw new ArgumentNullException(nameof(requestBody));

            var message = Client.ParseMessage(requestMethod, requestContentType, requestBody);
            if (message == null)
            {
                throw new NullReferenceException(nameof(message));
            }
            var payment = message.Object;
            return payment;
        }

    }
}
