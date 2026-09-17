using Microsoft.AspNetCore.Mvc;
using RestarProduct.DTOs;
using RestarProduct.Interfaces;

namespace RestarProduct.Controllers;

[ApiController]
[Route("raw-materials")]
public class RawMaterialController(
    IRawMaterialService rawMaterialService,
    ILogger<RawMaterialController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RawMaterialDTO>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var materials = await rawMaterialService.GetAllAsync(pageNumber, pageSize, cancellationToken);
        logger.LogInformation("GET /raw-materials?pageNumber={PageNumber}&pageSize={PageSize} returned {RawMaterialCount} raw materials", pageNumber, pageSize, materials.Count);
        return Ok(materials);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RawMaterialDTO>> GetById(int id, CancellationToken cancellationToken)
    {
        var material = await rawMaterialService.GetByIdAsync(id, cancellationToken);
        logger.LogInformation("GET /raw-materials/{RawMaterialId} completed. Found: {Found}", id, material is not null);
        return material is null ? NotFound() : Ok(material);
    }

    [HttpPost]
    public async Task<ActionResult<RawMaterialDTO>> Create([FromBody] CreateRawMaterialDTO rawMaterial, CancellationToken cancellationToken)
    {
        try
        {
            var createdMaterial = await rawMaterialService.CreateAsync(rawMaterial, cancellationToken);
            logger.LogInformation("POST /raw-materials created raw material {RawMaterialId}", createdMaterial.id);
            return CreatedAtAction(nameof(GetById), new { id = createdMaterial.id }, createdMaterial);
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRawMaterialDTO rawMaterial, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await rawMaterialService.UpdateAsync(id, rawMaterial, cancellationToken);
            logger.LogInformation("PUT /raw-materials/{RawMaterialId} completed. Updated: {Updated}", id, updated);
            return updated ? NoContent() : NotFound();
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await rawMaterialService.DeleteAsync(id, cancellationToken);
        logger.LogInformation("DELETE /raw-materials/{RawMaterialId} completed. Deleted: {Deleted}", id, deleted);
        return deleted ? NoContent() : NotFound();
    }
}
