using Application.Contractors;
using Application.ReponseDTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrostTrack.Server.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CashBookController : ControllerBase
    {
        private readonly ICashBookService _cashBookService;

        public CashBookController(ICashBookService cashBookService)
        {
            _cashBookService = cashBookService;
        }

        [HttpGet]
        public async Task<ActionResult<CashBookResponse>> GetCashBook(
            [FromQuery] DateTime reportDate,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await _cashBookService.GetCashBookAsync(reportDate, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while generating cash book report.", error = ex.Message });
            }
        }

        [HttpGet("range")]
        public async Task<ActionResult<CashBookRangeResponse>> GetCashBookRange(
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await _cashBookService.GetCashBookRangeAsync(fromDate, toDate, cancellationToken);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while generating cash book range report.", error = ex.Message });
            }
        }
    }
}
