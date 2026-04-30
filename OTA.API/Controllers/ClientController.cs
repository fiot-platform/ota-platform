using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OTA.API.Helpers;
using OTA.API.Models.DTOs;
using OTA.API.Services.Interfaces;

namespace OTA.API.Controllers
{
    /// <summary>
    /// Client (customer organisation) management endpoints — full CRUD.
    /// </summary>
    [ApiController]
    [Route("api/clients")]
    [Authorize]
    [Produces("application/json")]
    public class ClientController : ControllerBase
    {
        private readonly IClientService _clientService;
        private readonly ILogger<ClientController> _logger;

        public ClientController(IClientService clientService, ILogger<ClientController> logger)
        {
            _clientService = clientService ?? throw new ArgumentNullException(nameof(clientService));
            _logger        = logger        ?? throw new ArgumentNullException(nameof(logger));
        }

        private string CurrentUserId =>
            User.FindFirstValue("userId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        /// <summary>Returns the next available sequential client code (e.g. "CUSTOM_00003"). Requires CanViewClients.</summary>
        [HttpGet("next-code")]
        [Authorize(Policy = "CanViewClients")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetNextCode(CancellationToken cancellationToken = default)
        {
            var code = await _clientService.GetNextCodeAsync(cancellationToken);
            return Ok(ApiResponse<string>.Ok(code));
        }

        /// <summary>Returns a paginated list of clients. Requires CanViewClients.</summary>
        [HttpGet]
        [Authorize(Policy = "CanViewClients")]
        [ProducesResponseType(typeof(ApiResponse<List<ClientDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetClients(
            [FromQuery] string? search   = null,
            [FromQuery] int     page     = 1,
            [FromQuery] int     pageSize = 25,
            CancellationToken   cancellationToken = default)
        {
            var result     = await _clientService.GetClientsAsync(search ?? string.Empty, page, pageSize, cancellationToken);
            var pagination = PaginationInfo.Create(page, pageSize, result.TotalCount);
            return Ok(ApiResponse<List<ClientDto>>.Ok(result.Items, "Clients retrieved successfully.", pagination));
        }

        /// <summary>Returns a single client by its MongoDB ObjectId. Requires CanViewClients.</summary>
        [HttpGet("{id}")]
        [Authorize(Policy = "CanViewClients")]
        [ProducesResponseType(typeof(ApiResponse<ClientDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetClientById(string id, CancellationToken cancellationToken = default)
        {
            var client = await _clientService.GetClientByIdAsync(id, cancellationToken);
            if (client is null)
                return NotFound(ApiResponse<ClientDto>.Fail($"Client '{id}' was not found."));

            return Ok(ApiResponse<ClientDto>.Ok(client));
        }

        /// <summary>Creates a new client. Requires CanManageClients.</summary>
        [HttpPost]
        [Authorize(Policy = "CanManageClients")]
        [ProducesResponseType(typeof(ApiResponse<ClientDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateClient(
            [FromBody] CreateClientRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<ClientDto>.Fail("Validation failed.", errors));
            }

            var created = await _clientService.CreateClientAsync(request, CurrentUserId, cancellationToken);
            return CreatedAtAction(nameof(GetClientById), new { id = created.Id },
                ApiResponse<ClientDto>.Ok(created, "Client created successfully."));
        }

        /// <summary>Updates a client's mutable attributes. Requires CanManageClients.</summary>
        [HttpPut("{id}")]
        [Authorize(Policy = "CanManageClients")]
        [ProducesResponseType(typeof(ApiResponse<ClientDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateClient(
            string id,
            [FromBody] UpdateClientRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<ClientDto>.Fail("Validation failed.", errors));
            }

            var updated = await _clientService.UpdateClientAsync(id, request, CurrentUserId, cancellationToken);
            return Ok(ApiResponse<ClientDto>.Ok(updated, "Client updated successfully."));
        }

        /// <summary>Deletes a client permanently. Requires SuperAdmin or PlatformAdmin.</summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "SuperAdmin,PlatformAdmin")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteClient(string id, CancellationToken cancellationToken = default)
        {
            await _clientService.DeleteClientAsync(id, cancellationToken);
            return Ok(ApiResponse.OkNoData("Client deleted successfully."));
        }
    }
}
