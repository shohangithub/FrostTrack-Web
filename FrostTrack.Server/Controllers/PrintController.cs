using Microsoft.AspNetCore.Mvc;
using Application.Contractors;
using Application.ReponseDTO;
using Application.RequestDTO;

namespace FrostTrack.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PrintController : ControllerBase
    {
        private readonly IPrintService _printService;

        public PrintController(IPrintService printService)
        {
            _printService = printService;
        }

        /// <summary>
        /// Get print settings for a specific branch
        /// </summary>
        /// <param name="branchId">The branch ID</param>
        /// <returns>Print settings for the branch</returns>
        [HttpGet("settings/{branchId}")]
        public async Task<ActionResult<PrintSettingsResponse>> GetPrintSettings(int branchId)
        {
            try
            {
                var settings = await _printService.GetPrintSettingsByBranchAsync(branchId);
                return Ok(settings);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Create or update print settings for a branch
        /// </summary>
        /// <param name="branchId">The branch ID</param>
        /// <param name="settings">The print settings</param>
        /// <returns>Updated print settings</returns>
        [HttpPost("settings/{branchId}")]
        public async Task<ActionResult<PrintSettingsResponse>> CreateOrUpdatePrintSettings(int branchId, [FromBody] PrintSettingsResponse settings)
        {
            try
            {
                var updatedSettings = await _printService.CreateOrUpdatePrintSettingsAsync(branchId, settings);
                return Ok(updatedSettings);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Generate payment receipt HTML
        /// </summary>
        /// <param name="paymentId">The payment ID</param>
        /// <param name="request">Print options</param>
        /// <returns>HTML content for the receipt</returns>
        [HttpPost("payment-receipt/{paymentId}")]
        public async Task<ActionResult<string>> GeneratePaymentReceiptHtml(int paymentId, [FromBody] PrintReceiptRequest? request = null)
        {
            try
            {
                // Get payment data
                var paymentData = await _printService.GetPaymentReceiptDataAsync(paymentId);

                // Get print settings (use branch from payment or default to 1)
                var branchId = 1; // You might want to get this from the payment data
                var printSettings = await _printService.GetPrintSettingsByBranchAsync(branchId);

                // Override settings if specified in request
                if (request != null)
                {
                    if (!string.IsNullOrEmpty(request.PaperSize))
                        printSettings.PaperSize = request.PaperSize;
                    if (!string.IsNullOrEmpty(request.Orientation))
                        printSettings.Orientation = request.Orientation;
                    if (!string.IsNullOrEmpty(request.FontSize))
                        printSettings.FontSize = request.FontSize;
                    if (request.Copies > 0)
                        printSettings.DefaultCopies = request.Copies;

                    printSettings.ShowLogo = request.ShowLogo;
                    printSettings.ShowBranchInfo = request.ShowBranchInfo;

                    if (!string.IsNullOrEmpty(request.CustomFooterText))
                        printSettings.FooterText = request.CustomFooterText;
                }

                var html = await _printService.GeneratePaymentReceiptHtmlAsync(paymentData, printSettings);
                return Ok(html);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Generate payment receipt PDF
        /// </summary>
        /// <param name="paymentId">The payment ID</param>
        /// <param name="request">Print options</param>
        /// <returns>PDF file</returns>
        [HttpPost("payment-receipt-pdf/{paymentId}")]
        public async Task<ActionResult> GeneratePaymentReceiptPdf(int paymentId, [FromBody] PrintReceiptRequest? request = null)
        {
            try
            {
                // Get payment data
                var paymentData = await _printService.GetPaymentReceiptDataAsync(paymentId);

                // Get print settings
                var branchId = 1; // You might want to get this from the payment data
                var printSettings = await _printService.GetPrintSettingsByBranchAsync(branchId);

                // Override settings if specified in request
                if (request != null)
                {
                    if (!string.IsNullOrEmpty(request.PaperSize))
                        printSettings.PaperSize = request.PaperSize;
                    if (!string.IsNullOrEmpty(request.Orientation))
                        printSettings.Orientation = request.Orientation;
                    if (!string.IsNullOrEmpty(request.FontSize))
                        printSettings.FontSize = request.FontSize;
                    if (request.Copies > 0)
                        printSettings.DefaultCopies = request.Copies;
                }

                var pdf = await _printService.GeneratePaymentReceiptPdfAsync(paymentData, printSettings);
                return File(pdf, "application/pdf", $"payment-receipt-{paymentData.PaymentNumber}.pdf");
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (NotImplementedException ex)
            {
                return BadRequest(new { message = "PDF generation not implemented yet. " + ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get payment receipt data for preview
        /// </summary>
        /// <param name="paymentId">The payment ID</param>
        /// <returns>Payment receipt data</returns>
        [HttpGet("payment-data/{paymentId}")]
        public async Task<ActionResult<PaymentReceiptData>> GetPaymentReceiptData(int paymentId)
        {
            try
            {
                var data = await _printService.GetPaymentReceiptDataAsync(paymentId);
                return Ok(data);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}