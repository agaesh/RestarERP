using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RestarProduct.DTOs;
using RestarProduct.Interfaces;

namespace RestarProduct.Controllers;

[ApiController]
[Route("products")]
public class ProductController(
	IProductService productService,
	ILogger<ProductController> logger) : ControllerBase
{
	[HttpGet]
	public async Task<ActionResult<IReadOnlyList<ProductDTO>>> GetAll(
		CancellationToken cancellationToken)
	{
		var products = await productService.GetAllAsync(cancellationToken);
		logger.LogInformation("GET /products returned {ProductCount} products", products.Count);
		return Ok(products);
	}

	[HttpGet("{id:int}")]
	public async Task<ActionResult<ProductDTO>> GetById(
		int id,
		CancellationToken cancellationToken)
	{
		var product = await productService.GetByIdAsync(id, cancellationToken);
		logger.LogInformation("GET /products/{ProductId} completed. Found: {Found}", id, product is not null);
		return product is null ? NotFound() : Ok(product);
	}

	[HttpPost]
	public async Task<ActionResult<ProductDTO>> Create(
		CreateProductDTO product,
		CancellationToken cancellationToken)
	{
		var createdProduct = await productService.CreateAsync(product, cancellationToken);
		logger.LogInformation("POST /products created product {ProductId}", createdProduct.id);
		return CreatedAtAction(nameof(GetById), new { id = createdProduct.id }, createdProduct);
	}

	[HttpPut("{id:int}")]
	public async Task<IActionResult> Update(
		int id,
		UpdateProductDTO product,
		CancellationToken cancellationToken)
	{
		var updated = await productService.UpdateAsync(id, product, cancellationToken);
		logger.LogInformation("PUT /products/{ProductId} completed. Updated: {Updated}", id, updated);
		return updated ? NoContent() : NotFound();
	}

	[HttpDelete("{id:int}")]
	public async Task<IActionResult> Delete(
		int id,
		CancellationToken cancellationToken)
	{
		var deleted = await productService.DeleteAsync(id, cancellationToken);
		logger.LogInformation("DELETE /products/{ProductId} completed. Deleted: {Deleted}", id, deleted);
		return deleted ? NoContent() : NotFound();
	}
}
