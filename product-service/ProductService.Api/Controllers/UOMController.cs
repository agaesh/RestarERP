using Microsoft.AspNetCore.Mvc;
using RestarProduct.DTOs;
using RestarProduct.Exceptions;
using RestarProduct.Interfaces;

namespace RestarProduct.Controllers;

[ApiController]
[Route("uoms")]
public sealed class UOMController(
    IUOMService uomService,
    ILogger<UOMController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UOMDTO>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var uoms = await uomService.GetAllAsync(pageNumber, pageSize, cancellationToken);
        logger.LogInformation(
            "GET /uoms?pageNumber={PageNumber}&pageSize={PageSize} returned {UOMCount} UOMs",
            pageNumber,
            pageSize,
            uoms.Count);
        return Ok(uoms);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UOMDTO>> GetById(int id, CancellationToken cancellationToken)
    {
        var uom = await uomService.GetByIdAsync(id, cancellationToken);
        return uom is null ? NotFound() : Ok(uom);
    }

    [HttpPost]
    public async Task<ActionResult<UOMDTO>> Create(CreateUOMDTO request, CancellationToken cancellationToken)
    {
        try
        {
            var uom = await uomService.CreateAsync(request, cancellationToken);
            logger.LogInformation("POST /uoms created UOM {UOMId}", uom.id);
            return CreatedAtAction(nameof(GetById), new { id = uom.id }, uom);
        }
        catch (UOMCodeAlreadyExistsException ex)
        {
            return Conflict(new { success = false, message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateUOMDTO request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await uomService.UpdateAsync(id, request, cancellationToken);
            return updated ? NoContent() : NotFound(new
            {
                success = false,
                message = $"UOM {id} was not found."
            });
        }
        catch (UOMCodeAlreadyExistsException ex)
        {
            return Conflict(new { success = false, message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await uomService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
