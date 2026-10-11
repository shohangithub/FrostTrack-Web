using Application.Contractors;
using Application.ReponseDTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrostTrack.Server.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class LedgerBookController : ControllerBase
    {
        private readonly ILedgerBookService _ledgerBookService;

        public LedgerBookController(ILedgerBookService ledgerBookService)
        {
            _ledgerBookService = ledgerBookService;
        }

        /// <summary>All Transaction Groups: opening, debit, credit and closing for the period.</summary>
        [HttpGet("summary")]
        public async Task<ActionResult<LedgerBookSummaryResponse>> GetSummary(
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate,
            CancellationToken cancellationToken)
        {
            try
            {
                return Ok(await _ledgerBookService.GetSummaryAsync(fromDate, toDate, cancellationToken));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>One Transaction Group's ledger with opening balance and running balance.</summary>
        [HttpGet("group/{groupId:guid}")]
        public async Task<ActionResult<LedgerBookGroupResponse>> GetGroupLedger(
            Guid groupId,
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate,
            CancellationToken cancellationToken)
        {
            try
            {
                return Ok(await _ledgerBookService.GetGroupLedgerAsync(groupId, fromDate, toDate, cancellationToken));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Application.Common.NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}
