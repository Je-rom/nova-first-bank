using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NovaWallet.Dtos.Transfer;
using NovaWallet.Interfaces;

namespace NovaWallet.Controllers
{
    [ApiController]
    [Authorize]
    [Route("transfers")]
    public class TransfersController : ControllerBase
    {
        private readonly ITransferService _transferService;

        public TransfersController(ITransferService transferService)
        {
            _transferService = transferService;
        }

        [HttpPost]
        [ProducesResponseType(typeof(TransferResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status402PaymentRequired)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult<TransferResponseDto>> CreateTransfer(
            [FromBody] TransferRequestDto request,
            [FromHeader(Name = "Idempotency-Key")] string idempotencyKey)
        {

            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var result = await _transferService.ProcessTransferAsync(request, idempotencyKey);
            return Ok(result);
        }
    }
}