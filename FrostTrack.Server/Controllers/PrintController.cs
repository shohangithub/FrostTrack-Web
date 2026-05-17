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
        /// Generate booking invoice HTML
        /// </summary>
        /// <param name="bookingId">The booking ID</param>
        /// <returns>HTML content for the booking invoice</returns>
        [HttpGet("booking-invoice/{bookingId}")]
        public async Task<ActionResult<string>> GenerateBookingInvoiceHtml(Guid bookingId)
        {
            try
            {
                var bookingData = await _printService.GetBookingInvoiceDataAsync(bookingId);

                var branchId = 1;
                var printSettings = await _printService.GetPrintSettingsByBranchAsync(branchId);

                var html = await _printService.GenerateBookingInvoiceHtmlAsync(bookingData, printSettings);
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
        /// Get booking invoice data for preview
        /// </summary>
        /// <param name="bookingId">The booking ID</param>
        /// <returns>Booking invoice data</returns>
        [HttpGet("booking-data/{bookingId}")]
        public async Task<ActionResult<BookingInvoiceData>> GetBookingInvoiceData(Guid bookingId)
        {
            try
            {
                var data = await _printService.GetBookingInvoiceDataAsync(bookingId);
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