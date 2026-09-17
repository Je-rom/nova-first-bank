using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NovaWallet.Dtos.Wallet;
using NovaWallet.Interfaces;

namespace NovaWallet.Controllers
{
    [ApiController]
    [Authorize]
    [Route("wallets")]
    public class WalletsController : ControllerBase
    {
        private readonly IWalletService _walletService;

        public WalletsController(IWalletService walletService)
        {
            _walletService = walletService;
        }

        [HttpPost]
        [ProducesResponseType(typeof(WalletResponseDto), StatusCodes.Status201Created)]
        public async Task<ActionResult<WalletResponseDto>> CreateWallet([FromBody] CreateWalletRequestDto request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var wallet = await _walletService.CreateWalletAsync(request);
            return CreatedAtAction(nameof(GetBalance), new { walletId = wallet.Id }, wallet);
        }

        [HttpGet("{walletId:guid}/balance")]
        [ProducesResponseType(typeof(BalanceResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BalanceResponseDto>> GetBalance(Guid walletId)
        {
            var balance = await _walletService.GetBalanceAsync(walletId);
            return Ok(balance);
        }

        [HttpPost("{walletId:guid}/credit")]
        [ProducesResponseType(typeof(BalanceResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BalanceResponseDto>> Credit(
            Guid walletId, [FromBody] CreditWalletRequestDto request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var result = await _walletService.CreditWalletAsync(walletId, request);
            return Ok(result);
        }

        [HttpGet("{walletId:guid}/statement")]
        [ProducesResponseType(typeof(WalletStatementResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<WalletStatementResponseDto>> GetStatement(
            Guid walletId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var statement = await _walletService.GetStatementAsync(walletId, page, pageSize);
            return Ok(statement);
        }
    }
}