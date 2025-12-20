using Application.Contractors;
using Microsoft.AspNetCore.Mvc;

namespace FrostTrack.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LedgerBookController : ControllerBase
    {
        private readonly ILedgerBookService _ledgerBookService;

        public LedgerBookController(ILedgerBookService ledgerBookService)
        {
            _ledgerBookService = ledgerBookService;
        }

        [HttpGet]
        public async Task<IActionResult> GetGeneralLedger(
            [FromQuery] DateTime reportDate,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await _ledgerBookService.GetGeneralLedgerAsync(reportDate, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
