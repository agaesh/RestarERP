using Microsoft.AspNetCore.Mvc;
using RestarProduct.DTOs;
using RestarProduct.Interfaces;

namespace RestarProduct.Controllers;

[ApiController]
[Route("products")]
public class ProductController(IProductService productService) : ControllerBase
{
	[HttpGet]
	public async Task<ActionResult<IReadOnlyList<ProductDTO>>> GetAll(
		CancellationToken cancellationToken)
	{
		return Ok(await productService.GetAllAsync(cancellationToken));
	}

	[HttpGet("{id:int}")]
	public async Task<ActionResult<ProductDTO>> GetById(
		int id,
		CancellationToken cancellationToken)
	{
		var product = await productService.GetByIdAsync(id, cancellationToken);
		return product is null ? NotFound() : Ok(product);
	}

	[HttpPost]
	public async Task<ActionResult<ProductDTO>> Create(
		CreateProductDTO product,
		CancellationToken cancellationToken)
	{
		var createdProduct = await productService.CreateAsync(product, cancellationToken);
		return CreatedAtAction(nameof(GetById), new { id = createdProduct.id }, createdProduct);
	}

	[HttpPut("{id:int}")]
	public async Task<IActionResult> Update(
		int id,
		UpdateProductDTO product,
		CancellationToken cancellationToken)
	{
		return await productService.UpdateAsync(id, product, cancellationToken)
			? NoContent()
			: NotFound();
	}

	[HttpDelete("{id:int}")]
	public async Task<IActionResult> Delete(
		int id,
		CancellationToken cancellationToken)
	{
		return await productService.DeleteAsync(id, cancellationToken)
			? NoContent()
			: NotFound();
	}
}
