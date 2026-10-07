using ERP.WEB.Data;
using ERP.WEB.Models.Ventas;
using ERP.WEB.Models.Inventario;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ERP.WEB.Pages.Ventas
{
    [Authorize]
    public class NuevaCotizacionModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public NuevaCotizacionModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public CotizacionInputModel InputCotiz { get; set; } = new();

        [BindProperty]
        public string ItemsJson { get; set; } = "[]";

        public SelectList SelectSubCategoriasPT { get; set; } = null!;

        [TempData]
        public string? MensajeExito { get; set; }

        [TempData]
        public string? MensajeError { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Autogenerar código correlativo de cotización
            int correlativo = await _context.CotizacionesCabecera.CountAsync() + 1;
            InputCotiz.Codigo = $"COT-{DateTime.Now.Year}-{correlativo:D4}";
            InputCotiz.FechaEntregaAprox = DateTime.Now.AddDays(7);
            InputCotiz.TipoCambio = 3.7500m;

            await CargarCombosAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostGuardarCotizacionAsync()
        {
            // 1. Validar anotaciones del modelo
            if (!ModelState.IsValid)
            {
                var errores = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                MensajeError = $"Por favor complete todos los campos obligatorios: {errores}";
                await CargarCombosAsync();
                return Page();
            }

            // 2. Validar selección de Cliente
            if (InputCotiz.ClienteID <= 0 || string.IsNullOrWhiteSpace(InputCotiz.ClienteNombre))
            {
                MensajeError = $"Debe seleccionar un cliente válido para la cotización. (ID Recibido: {InputCotiz.ClienteID})";
                await CargarCombosAsync();
                return Page();
            }

            // 3. Validar Ítems
            List<ItemCotizacionDTO>? items = null;
            try
            {
                items = JsonSerializer.Deserialize<List<ItemCotizacionDTO>>(ItemsJson);
            }
            catch
            {
                MensajeError = "El formato de los detalles de la cotización es inválido.";
                await CargarCombosAsync();
                return Page();
            }

            if (items == null || !items.Any())
            {
                MensajeError = "Debe agregar al menos un ítem a la cotización.";
                await CargarCombosAsync();
                return Page();
            }

            try
            {
                decimal subtotal = items.Sum(i => i.Cantidad * i.PrecioUnitarioSinIGV);
                decimal igv = subtotal * 0.18m;
                decimal total = subtotal + igv;

                var nuevaCotizacion = new CotizacionesCabecera
                {
                    Codigo = InputCotiz.Codigo,
                    ClienteID = InputCotiz.ClienteID,
                    ClienteNombre = InputCotiz.ClienteNombre,
                    ClienteDireccion = InputCotiz.ClienteDireccion,
                    FechaEmision = DateTime.Now,
                    FechaEntregaAprox = InputCotiz.FechaEntregaAprox,
                    FormaPago = InputCotiz.FormaPago,
                    Moneda = InputCotiz.Moneda,
                    TipoCambio = InputCotiz.TipoCambio,
                    SubTotal = subtotal,
                    IGV = igv,
                    Total = total,
                    Estado = "Pendiente",
                    Detalles = items.Select(i => new CotizacionesDetalle
                    {
                        ProductoID = i.ProductoID > 0 ? i.ProductoID : (int?)null,
                        DescripcionItem = i.DescripcionItem,
                        UnidadMedida = i.UnidadMedida,
                        Cantidad = i.Cantidad,
                        PrecioUnitarioSinIGV = i.PrecioUnitarioSinIGV,
                        SubTotalItem = i.Cantidad * i.PrecioUnitarioSinIGV
                    }).ToList()
                };

                _context.CotizacionesCabecera.Add(nuevaCotizacion);
                await _context.SaveChangesAsync();

                MensajeExito = $"Cotización {nuevaCotizacion.Codigo} registrada correctamente.";
                return RedirectToPage("/Ventas/BandejaCotizaciones");
            }
            catch (Exception ex)
            {
                string mensajeInterno = ex.InnerException != null ? $" ({ex.InnerException.Message})" : "";
                MensajeError = $"Error en la base de datos al guardar: {ex.Message}{mensajeInterno}";
                await CargarCombosAsync();
                return Page();
            }
        }

        // HANDLERS AJAX

        public async Task<JsonResult> OnGetBuscarClientesAsync(string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
            {
                return new JsonResult(new List<object>());
            }

            string term = q.Trim().ToLower();

            var clientes = await _context.Clientes
                .Where(c => c.Estado &&
                           (c.NumDocumento.ToLower().Contains(term) || c.RazonSocial.ToLower().Contains(term)))
                .Take(10)
                .Select(c => new
                {
                    id = c.ClienteID,
                    documento = c.NumDocumento,
                    nombre = c.RazonSocial,
                    direccion = c.Direccion ?? ""
                })
                .ToListAsync();

            return new JsonResult(clientes);
        }

        public async Task<IActionResult> OnPostCrearClienteExpressAsync([FromBody] ClienteExpressDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.RazonSocial))
            {
                return new JsonResult(new { success = false, message = "La Razón Social / Nombre Completo es obligatorio." });
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(dto.Documento))
                {
                    bool existe = await _context.Clientes.AnyAsync(c => c.NumDocumento == dto.Documento.Trim());
                    if (existe)
                    {
                        return new JsonResult(new { success = false, message = "Ya existe un cliente registrado con ese número de documento." });
                    }
                }

                var nuevoCliente = new Cliente
                {
                    TipoDocumento = string.IsNullOrWhiteSpace(dto.TipoDocumento)
                        ? (dto.Documento?.Length == 11 ? "RUC" : "DNI")
                        : dto.TipoDocumento,
                    NumDocumento = dto.Documento?.Trim() ?? "",
                    RazonSocial = dto.RazonSocial.Trim(),
                    Direccion = dto.Direccion?.Trim(),
                    Estado = true,
                    FechaRegistro = DateTime.Now
                };

                _context.Clientes.Add(nuevoCliente);
                await _context.SaveChangesAsync();

                return new JsonResult(new
                {
                    success = true,
                    clienteID = nuevoCliente.ClienteID,
                    documento = nuevoCliente.NumDocumento,
                    razonSocial = nuevoCliente.RazonSocial,
                    direccion = nuevoCliente.Direccion
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = $"Error al registrar cliente: {ex.Message}" });
            }
        }

        public async Task<JsonResult> OnGetBuscarProductosTerminadosAsync(int subCategoriaId, string query)
        {
            if (subCategoriaId <= 0 && string.IsNullOrWhiteSpace(query))
            {
                return new JsonResult(new List<object>());
            }

            // Filtrar únicamente productos cuya CategoriaID sea 5 (Productos Terminados)
            var q = _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.SubCategoria)
                .Where(p => p.Estado && p.CategoriaID == 5)
                .AsQueryable();

            if (subCategoriaId > 0)
            {
                q = q.Where(p => p.SubcategoriaID == subCategoriaId);
            }

            if (!string.IsNullOrWhiteSpace(query))
            {
                string term = query.Trim().ToLower();
                q = q.Where(p => p.SKU.ToLower().Contains(term) || p.Nombre.ToLower().Contains(term));
            }

            var lista = await q.OrderBy(p => p.Nombre)
                .Take(20)
                .Select(p => new
                {
                    productoID = p.ProductoID,
                    sku = p.SKU,
                    nombre = p.Nombre,
                    categoriaSubCat = $"{p.Categoria!.Nombre} / {p.SubCategoria!.Nombre}",
                    unidadMedida = p.UnidadMedida,
                    stockActual = p.StockActual,
                    precioSinIGV = p.PrecioVenta
                })
                .ToListAsync();

            return new JsonResult(lista);
        }

        public async Task<JsonResult> OnGetGenerarSkuPTAsync(int subCategoriaId)
        {
            var subCat = await _context.SubCategorias
                .Include(s => s.Categoria)
                .FirstOrDefaultAsync(s => s.SubCategoriaID == subCategoriaId);

            if (subCat == null) return new JsonResult(new { sku = "" });

            int count = await _context.Productos.CountAsync(p => p.SubcategoriaID == subCategoriaId) + 1;
            string prefCat = subCat.Categoria?.CodigoPrefijo ?? "05";
            string prefSub = subCat.CodigoPrefijo ?? "01";

            string sku = $"{prefCat}{prefSub}{count:D4}";
            return new JsonResult(new { sku });
        }

        public async Task<IActionResult> OnPostCrearProductoExpressAsync([FromBody] ProductoExpressDTO dto)
        {
            if (dto.SubcategoriaID <= 0 || string.IsNullOrWhiteSpace(dto.Nombre))
            {
                return new JsonResult(new { success = false, message = "Datos incompletos." });
            }

            try
            {
                var subCat = await _context.SubCategorias.FindAsync(dto.SubcategoriaID);
                if (subCat == null) return new JsonResult(new { success = false, message = "Subcategoría inválida." });

                var nuevoProd = new Producto
                {
                    CategoriaID = subCat.CategoriaID,
                    SubcategoriaID = dto.SubcategoriaID,
                    SKU = dto.SKU,
                    Nombre = dto.Nombre,
                    UnidadMedida = dto.UnidadMedida,
                    ControlaStock = dto.ControlaStock,
                    StockActual = 0,
                    PrecioVenta = 0,
                    Estado = true,
                    FechaRegistro = DateTime.Now
                };

                _context.Productos.Add(nuevoProd);
                await _context.SaveChangesAsync();

                return new JsonResult(new
                {
                    success = true,
                    producto = new
                    {
                        productoID = nuevoProd.ProductoID,
                        sku = nuevoProd.SKU,
                        nombre = nuevoProd.Nombre,
                        unidadMedida = nuevoProd.UnidadMedida,
                        precioSinIGV = 0.00m
                    }
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        private async Task CargarCombosAsync()
        {
            // Filtrar únicamente subcategorías pertenecientes a CategoriaID = 5 (PT)
            var subCats = await _context.SubCategorias
                .Include(s => s.Categoria)
                .Where(s => s.Estado && s.Categoria != null && (s.Categoria.CodigoPrefijo == "05" || s.CategoriaID == 5))
                .OrderBy(s => s.Nombre)
                .Select(s => new {
                    s.SubCategoriaID,
                    Nombre = $"[{s.CodigoPrefijo ?? s.Codigo}] {s.Nombre}"
                })
                .ToListAsync();

            SelectSubCategoriasPT = new SelectList(subCats, "SubCategoriaID", "Nombre");
        }

        // DTOs INTERNOS
        public class CotizacionInputModel
        {
            public string Codigo { get; set; } = string.Empty;
            public int ClienteID { get; set; }
            public string ClienteNombre { get; set; } = string.Empty;
            public string ClienteDireccion { get; set; } = string.Empty;
            public DateTime FechaEntregaAprox { get; set; }
            public string FormaPago { get; set; } = "Crédito 30 días";
            public string Moneda { get; set; } = "PEN";
            public decimal TipoCambio { get; set; } = 3.7500m;
        }

        public class ItemCotizacionDTO
        {
            public int ProductoID { get; set; }
            public string DescripcionItem { get; set; } = string.Empty;
            public string UnidadMedida { get; set; } = "UND";
            public decimal Cantidad { get; set; }
            public decimal PrecioUnitarioSinIGV { get; set; }
        }

        public class ClienteExpressDTO
        {
            public string? Documento { get; set; }
            public string RazonSocial { get; set; } = null!;
            public string? Direccion { get; set; }
            public string? TipoDocumento { get; set; }
        }

        public class ProductoExpressDTO
        {
            public int SubcategoriaID { get; set; }
            public string SKU { get; set; } = string.Empty;
            public string Nombre { get; set; } = string.Empty;
            public string UnidadMedida { get; set; } = "UND";
            public bool ControlaStock { get; set; }
        }
    }
}