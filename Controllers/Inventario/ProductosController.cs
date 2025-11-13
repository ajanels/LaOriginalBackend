using System.Linq;
using System.Text;
using System.Globalization;
using LaOriginalBackend.Data;
using LaOriginalBackend.Dtos;
using LaOriginalBackend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration; // <-- para leer Uploads:Root

namespace LaOriginalBackend.Controllers.Inventario
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductosController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly string _uploadsRoot; // <-- base externa para /uploads

        public ProductosController(AppDbContext db, IWebHostEnvironment env, IConfiguration cfg)
        {
            _db = db;
            _env = env;

            // Debe coincidir con el fallback de Program.cs
            var configured = cfg["Uploads:Root"];
            _uploadsRoot = !string.IsNullOrWhiteSpace(configured)
                ? configured
                : Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "LaOriginal", "uploads"
                  );
        }

        // ===== LISTA =====
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductoListDto>>> Get(
            [FromQuery] string? term = null,
            [FromQuery] int? categoriaId = null,
            [FromQuery] bool soloActivos = true)
        {
            var q = _db.Productos
                .AsNoTracking()
                .Include(p => p.Categoria)
                .Include(p => p.Presentaciones)
                .AsQueryable();

            if (soloActivos) q = q.Where(p => p.Activo);

            if (!string.IsNullOrWhiteSpace(term))
            {
                var t = term.Trim().ToLower();
                q = q.Where(p => p.Nombre.ToLower().Contains(t) ||
                                 (p.Codigo != null && p.Codigo.ToLower().Contains(t)));
            }

            if (categoriaId.HasValue)
                q = q.Where(p => p.CategoriaId == categoriaId);

            var list = await q
                .OrderBy(p => p.Nombre)
                .Select(p => new {
                    p.Id,
                    p.Nombre,
                    p.Codigo,
                    Categoria = p.Categoria != null ? p.Categoria.Nombre : null,
                    Activo = p.Activo,
                    Presentaciones = p.Presentaciones.Count,
                    FotoUrl = p.FotoUrl,
                    Precio = p.Presentaciones.Where(pr => pr.EsPrincipal).Select(pr => pr.PrecioVentaDefault).FirstOrDefault(),
                    Stock = _db.ProductoStocks.Where(s => s.Presentacion.ProductoId == p.Id).Sum(s => (decimal?)s.Cantidad) ?? 0
                })
                .ToListAsync();

            return Ok(list);
        }

        // ===== DETALLE =====
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProductoDetailDto>> GetById(int id)
        {
            var p = await _db.Productos
                .AsNoTracking()
                .Include(x => x.Categoria)
                .Include(x => x.Presentaciones)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (p is null) return NotFound();

            var principal = p.Presentaciones?.FirstOrDefault(pr => pr.EsPrincipal);

            int? proveedorId = null;
            if (principal != null)
            {
                var catProv = await _db.ProveedoresPresentaciones
                    .AsNoTracking()
                    .Where(pp => pp.PresentacionId == principal.Id && pp.Activo)
                    .OrderBy(pp => pp.ProveedorId)
                    .FirstOrDefaultAsync();

                if (catProv != null) proveedorId = catProv.ProveedorId;
            }

            var dto = new ProductoDetailDto
            {
                Id = p.Id,
                Nombre = p.Nombre,
                Codigo = p.Codigo,
                Activo = p.Activo,
                CategoriaId = p.CategoriaId,
                Categoria = p.Categoria?.Nombre,
                FotoUrl = p.FotoUrl,
                PrecioCompraDefault = principal?.PrecioCompraDefault,
                PrecioVentaDefault = principal?.PrecioVentaDefault,
                ProveedorId = proveedorId
            };

            return Ok(dto);
        }

        // ===== CREAR =====
        [HttpPost]
        public async Task<ActionResult<ProductoDetailDto>> Create([FromBody] ProductoCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(dto.FotoUrl))
                return BadRequest("La imagen (FotoUrl) es obligatoria.");
            if (dto.PrecioCompraDefault <= 0 || dto.PrecioVentaDefault <= 0)
                return BadRequest("Los precios de compra y venta deben ser mayores a 0.");
            if (dto.PrecioVentaDefault < dto.PrecioCompraDefault)
                return BadRequest("El precio de venta no puede ser menor al precio de compra.");

            var categoria = await _db.Categorias
                .FirstOrDefaultAsync(c => c.Id == dto.CategoriaId && c.Activo);
            if (categoria is null) return BadRequest("Categoría inválida o inactiva.");

            var proveedorValido = await _db.Proveedores.AsNoTracking()
                .AnyAsync(p => p.Id == dto.ProveedorId && p.Activo);
            if (!proveedorValido) return BadRequest("Proveedor inválido o inactivo.");

            var nameExists = await _db.Productos
                .AnyAsync(p => p.Nombre.ToLower() == dto.Nombre.Trim().ToLower());
            if (nameExists) return Conflict(new { message = "Ya existe un producto con ese nombre." });

            var pref = await EnsureCategoriaPrefijoAsync(categoria);
            var codigo = await GenerarCodigoAsync(dto.CategoriaId, pref);

            using var tx = await _db.Database.BeginTransactionAsync();

            for (int intento = 0; intento < 2; intento++)
            {
                try
                {
                    var producto = new Producto
                    {
                        Nombre = dto.Nombre.Trim(),
                        Codigo = codigo,
                        Activo = dto.Activo,
                        CategoriaId = dto.CategoriaId,
                        FotoUrl = dto.FotoUrl
                    };
                    _db.Productos.Add(producto);
                    await _db.SaveChangesAsync();

                    var unidadMedidaId = await GetUnidadMedidaDefaultIdAsync();
                    if (unidadMedidaId == null)
                        return BadRequest("No hay Unidad de Medida por defecto. Crea una 'Unidad' o define un símbolo 'U'.");

                    var principal = new Presentacion
                    {
                        ProductoId = producto.Id,
                        Nombre = "Unidad",
                        UnidadMedidaId = unidadMedidaId.Value,
                        Factor = 1m,
                        PrecioCompraDefault = dto.PrecioCompraDefault,
                        PrecioVentaDefault = dto.PrecioVentaDefault,
                        EsPrincipal = true,
                        Activo = true
                    };
                    _db.Presentaciones.Add(principal);
                    await _db.SaveChangesAsync();

                    var precioLista = dto.PrecioCompraDefault;
                    var yaExiste = await _db.ProveedoresPresentaciones
                        .AnyAsync(x => x.ProveedorId == dto.ProveedorId && x.PresentacionId == principal.Id);

                    if (!yaExiste)
                    {
                        var row = new ProveedorPresentacion
                        {
                            ProveedorId = dto.ProveedorId,
                            PresentacionId = principal.Id,
                            CodigoProveedor = null,
                            PrecioLista = precioLista,
                            Activo = true,
                            Notas = null
                        };
                        _db.ProveedoresPresentaciones.Add(row);
                        await _db.SaveChangesAsync();
                    }

                    await tx.CommitAsync();
                    return await GetById(producto.Id);
                }
                catch (DbUpdateException)
                {
                    if (intento == 0)
                    {
                        codigo = await GenerarCodigoAsync(dto.CategoriaId, pref);
                        continue;
                    }
                    throw;
                }
            }

            return Problem("No fue posible generar un código único para el producto.", statusCode: 500);
        }

        // ===== ACTUALIZAR =====
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] ProductoUpdateDto dto)
        {
            if (id != dto.Id) return BadRequest();

            var entity = await _db.Productos
                .Include(p => p.Presentaciones)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (entity is null) return NotFound();

            var categoria = await _db.Categorias
                .FirstOrDefaultAsync(c => c.Id == dto.CategoriaId && c.Activo);
            if (categoria is null) return BadRequest("Categoría inválida o inactiva.");

            var nameExists = await _db.Productos.AnyAsync(p =>
                p.Id != id && p.Nombre.ToLower() == dto.Nombre.Trim().ToLower());
            if (nameExists) return Conflict(new { message = "Ya existe otro producto con ese nombre." });

            if (string.IsNullOrWhiteSpace(dto.FotoUrl))
                return BadRequest("La imagen (FotoUrl) es obligatoria.");

            entity.Nombre = dto.Nombre.Trim();
            entity.Activo = dto.Activo;
            entity.CategoriaId = dto.CategoriaId;
            entity.FotoUrl = dto.FotoUrl;

            if (dto.PrecioCompraDefault.HasValue || dto.PrecioVentaDefault.HasValue)
            {
                var principal = entity.Presentaciones.FirstOrDefault(pr => pr.EsPrincipal);
                if (principal == null)
                {
                    var unidadMedidaId = await GetUnidadMedidaDefaultIdAsync();
                    if (unidadMedidaId == null)
                        return BadRequest("No hay Unidad de Medida por defecto. Crea una 'Unidad' o define un símbolo 'U'.");

                    principal = new Presentacion
                    {
                        ProductoId = entity.Id,
                        Nombre = "Unidad",
                        UnidadMedidaId = unidadMedidaId.Value,
                        Factor = 1m,
                        PrecioCompraDefault = 0m,
                        PrecioVentaDefault = 0m,
                        EsPrincipal = true,
                        Activo = true
                    };
                    _db.Presentaciones.Add(principal);
                    await _db.SaveChangesAsync();
                }

                var nuevoCompra = dto.PrecioCompraDefault ?? principal.PrecioCompraDefault;
                var nuevoVenta = dto.PrecioVentaDefault ?? principal.PrecioVentaDefault;

                if (nuevoCompra <= 0 || nuevoVenta <= 0)
                    return BadRequest("Los precios deben ser mayores a 0.");
                if (nuevoVenta < nuevoCompra)
                    return BadRequest("El precio de venta no puede ser menor al precio de compra.");

                if (dto.PrecioCompraDefault.HasValue)
                    principal.PrecioCompraDefault = dto.PrecioCompraDefault.Value;
                if (dto.PrecioVentaDefault.HasValue)
                    principal.PrecioVentaDefault = dto.PrecioVentaDefault.Value;

                var links = await _db.ProveedoresPresentaciones
                    .Where(x => x.PresentacionId == principal.Id && x.Activo)
                    .ToListAsync();

                if (links.Count > 0 && dto.PrecioCompraDefault.HasValue)
                {
                    foreach (var row in links)
                        row.PrecioLista = dto.PrecioCompraDefault.Value;
                }
            }

            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPut("{id:int}/precios-default")]
        public async Task<IActionResult> UpdatePreciosDefault(int id, [FromBody] ProductoPreciosDefaultUpdateDto dto)
        {
            if (dto.PrecioCompraDefault <= 0 || dto.PrecioVentaDefault <= 0)
                return BadRequest("Los precios deben ser mayores a 0.");
            if (dto.PrecioVentaDefault < dto.PrecioCompraDefault)
                return BadRequest("El precio de venta no puede ser menor al precio de compra.");

            var principal = await _db.Presentaciones
                .FirstOrDefaultAsync(pr => pr.ProductoId == id && pr.EsPrincipal);

            if (principal is null)
                return NotFound("No se encontró la presentación principal del producto.");

            principal.PrecioCompraDefault = dto.PrecioCompraDefault;
            principal.PrecioVentaDefault = dto.PrecioVentaDefault;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // ===== ELIMINAR =====
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _db.Productos
                .Include(p => p.Presentaciones)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (entity is null) return NotFound();

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var presentIds = entity.Presentaciones.Select(pr => pr.Id).ToList();
                if (presentIds.Count > 0)
                {
                    var provLinks = await _db.ProveedoresPresentaciones
                        .Where(pp => presentIds.Contains(pp.PresentacionId))
                        .ToListAsync();

                    if (provLinks.Count > 0)
                    {
                        _db.ProveedoresPresentaciones.RemoveRange(provLinks);
                        await _db.SaveChangesAsync();
                    }
                }

                if (entity.Presentaciones.Count > 0)
                {
                    _db.Presentaciones.RemoveRange(entity.Presentaciones);
                    await _db.SaveChangesAsync();
                }

                _db.Productos.Remove(entity);
                await _db.SaveChangesAsync();

                await tx.CommitAsync();
                return NoContent();
            }
            catch (DbUpdateException ex)
            {
                await tx.RollbackAsync();

                var msg = ex.InnerException?.Message ?? ex.Message;
                if (msg.Contains("FK_ProveedoresPresentaciones_Presentaciones_PresentacionId"))
                    return Conflict(new { message = "No se puede eliminar: el producto está asociado al catálogo de uno o más proveedores." });
                return Problem(detail: ex.Message, title: "No se pudo eliminar el producto.", statusCode: 500);
            }
        }

        // ===== SUBIR IMAGEN (carpeta EXTERNA mapeada como /uploads) =====
        [HttpPost("imagen")]
        [RequestSizeLimit(5_000_000)]
        public async Task<ActionResult<object>> UploadImagen(IFormFile? file)
        {
            if (file is null || file.Length == 0) return BadRequest("Archivo vacío.");

            var allowed = new[] { "image/jpeg", "image/png", "image/webp" };
            if (!allowed.Contains(file.ContentType)) return BadRequest("Formato no soportado (JPG, PNG, WEBP).");

            var dir = Path.Combine(_uploadsRoot, "productos");      // => C:\LaOriginal\uploads\productos
            Directory.CreateDirectory(dir);

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var name = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(dir, name);

            await using (var fs = new FileStream(fullPath, FileMode.Create))
                await file.CopyToAsync(fs);

            // URL ABSOLUTA hacia el host del API
            var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
            var url = $"{baseUrl}/uploads/productos/{name}";
            return Ok(new { url });
        }

        // ===== Helpers =====
        private async Task<string> EnsureCategoriaPrefijoAsync(Categoria categoria)
        {
            var current = LettersUpper(categoria.Prefijo);
            if (current.Length > 2) current = current.Substring(0, 2);

            var usados = (await _db.Categorias.AsNoTracking()
                    .Where(c => c.Id != categoria.Id && c.Prefijo != null && c.Activo)
                    .Select(c => c.Prefijo!)
                    .ToListAsync())
                .Select(LettersUpper)
                .Select(p => p.Length >= 2 ? p.Substring(0, 2) : p)
                .ToHashSet();

            if (current.Length == 2 && !usados.Contains(current))
            {
                if (categoria.Prefijo != current)
                {
                    categoria.Prefijo = current;
                    await _db.SaveChangesAsync();
                }
                return current;
            }

            var candidate = GenerateTwoLetterPrefixFromName(categoria.Nombre);

            if (usados.Contains(candidate))
            {
                var first = candidate[0];

                var altFromWords = TwoInitialsFromWords(categoria.Nombre);
                if (altFromWords != null && !usados.Contains(altFromWords))
                {
                    candidate = altFromWords;
                }
                else
                {
                    var altInName = FirstPlusNextDistinct(categoria.Nombre);
                    if (altInName != null && !usados.Contains(altInName))
                    {
                        candidate = altInName;
                    }
                    else
                    {
                        candidate = null!;
                        for (char b = 'A'; b <= 'Z'; b++)
                        {
                            var cand = $"{first}{b}";
                            if (!usados.Contains(cand))
                            {
                                candidate = cand;
                                break;
                            }
                        }
                        if (candidate == null)
                        {
                            for (char a = 'A'; a <= 'Z' && candidate == null; a++)
                                for (char b = 'A'; b <= 'Z' && candidate == null; b++)
                                {
                                    var cand = $"{a}{b}";
                                    if (!usados.Contains(cand)) candidate = cand;
                                }
                        }
                    }
                }
            }

            categoria.Prefijo = candidate;
            await _db.SaveChangesAsync();
            return candidate;
        }

        private async Task<string> GenerarCodigoAsync(int categoriaId, string prefijo2)
        {
            var pref = LettersUpper(prefijo2);
            if (pref.Length != 2)
                throw new InvalidOperationException("El prefijo de la categoría debe ser de 2 letras.");

            var existentes = await _db.Productos
                .AsNoTracking()
                .Where(p => p.CategoriaId == categoriaId && p.Codigo != null && p.Codigo.StartsWith(pref))
                .Select(p => p.Codigo!)
                .ToListAsync();

            int max = 0;
            foreach (var code in existentes)
            {
                var suf = new string(code.Skip(pref.Length).ToArray());
                if (int.TryParse(suf, out var n) && n > max) max = n;
            }
            var next = max + 1;

            var pad = next <= 99 ? 2 : 3;
            return $"{pref}{next.ToString().PadLeft(pad, '0')}";
        }

        private async Task<int?> GetUnidadMedidaDefaultIdAsync()
        {
            var um = await _db.Unidades
                .AsNoTracking()
                .Where(u => u.Activo)
                .OrderBy(u => u.Id)
                .FirstOrDefaultAsync(u =>
                    u.Nombre == "Unidad" ||
                    u.Nombre == "UNIDAD" ||
                    u.Simbolo == "u" ||
                    u.Simbolo == "U");

            return um?.Id;
        }

        private static string LettersUpper(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            var clean = RemoveDiacritics(s);
            var sb = new StringBuilder();
            foreach (var ch in clean)
            {
                if (char.IsLetter(ch)) sb.Append(char.ToUpperInvariant(ch));
            }
            return sb.ToString();
        }

        private static string RemoveDiacritics(string? text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var ch in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark) sb.Append(ch);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        private static string GenerateTwoLetterPrefixFromName(string? nombreCategoria)
        {
            var letters = LettersUpper(nombreCategoria);
            if (letters.Length >= 2) return letters.Substring(0, 2);
            if (letters.Length == 1) return $"{letters[0]}X";
            return "XX";
        }

        private static string? TwoInitialsFromWords(string? nombreCategoria)
        {
            if (string.IsNullOrWhiteSpace(nombreCategoria)) return null;
            var clean = RemoveDiacritics(nombreCategoria).ToUpperInvariant();
            var words = clean.Split(new[] { ' ', '-', '_', '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length >= 2 && char.IsLetter(words[0][0]) && char.IsLetter(words[1][0]))
                return $"{words[0][0]}{words[1][0]}";
            return null;
        }

        private static string? FirstPlusNextDistinct(string? nombreCategoria)
        {
            var letters = LettersUpper(nombreCategoria);
            if (letters.Length == 0) return null;
            var first = letters[0];
            for (int i = 1; i < letters.Length; i++)
            {
                if (letters[i] != first)
                    return $"{first}{letters[i]}";
            }
            return null;
        }
    }
}
