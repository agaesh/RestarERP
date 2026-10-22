using Microsoft.AspNetCore.Mvc;
using RestarProduct.DTOs;
using RestarProduct.Enums;
using RestarProduct.Exceptions;
using RestarProduct.Interfaces;

namespace RestarProduct.Controllers;

[ApiController]
[Route("{ownerType:regex(products|raw-materials)}/{ownerId:int:min(1)}/multi-uoms")]
public sealed class MultiUOMController(
    IMultiUOMService service,
    ILogger<MultiUOMController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MultiUOMDTO>>> GetAll(
        string ownerType,
        int ownerId,
        CancellationToken cancellationToken)
    {
        var multiUOMs = await service.GetByReferenceAsync(
            ToRefType(ownerType),
            ownerId,
            cancellationToken);
        return multiUOMs is null ? NotFound() : Ok(multiUOMs);
    }

    [HttpGet("{multiUOMId:int:min(1)}", Name = "GetMultiUOM")]
    public async Task<ActionResult<MultiUOMDTO>> GetById(
        string ownerType,
        int ownerId,
        int multiUOMId,
        CancellationToken cancellationToken)
    {
        var multiUOM = await service.GetByIdAsync(
            multiUOMId,
            ToRefType(ownerType),
            ownerId,
            cancellationToken);
        return multiUOM is null ? NotFound() : Ok(multiUOM);
    }

    [HttpPost]
    public async Task<ActionResult<MultiUOMDTO>> Create(
        string ownerType,
        int ownerId,
        [FromBody] CreateMultiUOMDTO request,
        CancellationToken cancellationToken)
    {
        var refType = ToRefType(ownerType);
        try
        {
            var created = await service.CreateAsync(refType, ownerId, request, cancellationToken);
            logger.LogInformation(
                "Created MultiUOM setup {MultiUOMId} for {OwnerType} {OwnerId}",
                created.id,
                ownerType,
                ownerId);
            return CreatedAtRoute(
                "GetMultiUOM",
                new { ownerType, ownerId, multiUOMId = created.id },
                created);
        }
        catch (MultiUOMOwnerNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (MultiUOMAlreadyExistsException ex)
        {
            return Conflict(new { success = false, message = ex.Message });
        }
        catch (MultiUOMSetupNotFoundException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (MultiUOMReferenceMismatchException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPut("{multiUOMId:int:min(1)}")]
    public async Task<IActionResult> Update(
        string ownerType,
        int ownerId,
        int multiUOMId,
        [FromBody] UpdateMultiUOMDTO request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await service.UpdateAsync(
                multiUOMId,
                ToRefType(ownerType),
                ownerId,
                request,
                cancellationToken);
            return updated
                ? NoContent()
                : NotFound(new
                {
                    success = false,
                    message = $"MultiUOM setup {multiUOMId} was not found for this owner."
                });
        }
        catch (MultiUOMAlreadyExistsException ex)
        {
            return Conflict(new { success = false, message = ex.Message });
        }
        catch (MultiUOMSetupNotFoundException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (MultiUOMReferenceMismatchException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpDelete("{multiUOMId:int:min(1)}")]
    public async Task<IActionResult> Delete(
        string ownerType,
        int ownerId,
        int multiUOMId,
        CancellationToken cancellationToken)
    {
        var deleted = await service.DeleteAsync(
            multiUOMId,
            ToRefType(ownerType),
            ownerId,
            cancellationToken);
        return deleted
            ? NoContent()
            : NotFound(new
            {
                success = false,
                message = $"MultiUOM setup {multiUOMId} was not found for this owner."
            });
    }

    private static MultiUOMRefType ToRefType(string ownerType) => ownerType switch
    {
        "products" => MultiUOMRefType.Product,
        "raw-materials" => MultiUOMRefType.RawMaterial,
        _ => throw new ArgumentOutOfRangeException(nameof(ownerType), ownerType, "Unsupported MultiUOM owner type.")
    };
}
