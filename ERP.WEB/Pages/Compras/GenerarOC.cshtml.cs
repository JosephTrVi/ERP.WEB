using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ERP.WEB.Data;
using ERP.WEB.Models.Logistica;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ERP.WEB.Pages.Compras
{
    [Authorize]
    public class GenerarOCModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public GenerarOCModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public OrdenesCompraCabecera InputOC { get; set; } = new();

        public RequerimientoCabecera RequerimientoBase { get; set; } = null!;

        [BindProperty]
        public string ItemsJson { get; set; } = "[]";

        [BindProperty]
        public string? NombreProveedor { get; set; }

        [TempData]
        public string? MensajeError { get; set; }

        [TempData]
        public string? MensajeExito { get; set; }

        public async Task<IActionResult> OnGetAsync(int requerimientoId)
        {
            var req = await _context.RequerimientosCabecera
                .Include(r => r.Detalles)
                    .ThenInclude(d => d.Producto)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.RequerimientoID == requerimientoId && r.Estado == "Aprobado");

            if (req == null)
            {
                MensajeError = "El requerimiento no existe o no se encuentra en estado Aprobado.";
                return RedirectToPage("/Compras/BandejaRequerimientos");
            }

            RequerimientoBase = req;

            int anioActual = DateTime.Now.Year;
            int correlativo = await _context.OrdenesCompraCabecera
                .Where(o => o.FechaEmision.Year == anioActual)
                .CountAsync() + 1;

            InputOC.Codigo = $"OC-{anioActual}-{correlativo.ToString("D4")}";
            InputOC.RequerimientoID = req.RequerimientoID;
            InputOC.FechaEmision = DateTime.Now;
            InputOC.FechaEntrega = req.FechaRequerida;
            InputOC.Moneda = req.TipoRequerimiento == "Importacion" ? "USD" : "PEN";
            InputOC.FormaPago = "Crédito 30 días";

            return Page();
        }

        // HANDLER AJAX: BUSCAR PROVEEDORES REGISTRADOS
        public async Task<JsonResult> OnGetBuscarProveedoresAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return new JsonResult(new List<object>());

            string t = query.Trim().ToLower();

            var resultados = await _context.Proveedores
                .Where(p => p.Estado && (p.NumDocumento.ToLower().Contains(t) || p.RazonSocial.ToLower().Contains(t)))
                .Take(10)
                .Select(p => new
                {
                    proveedorID = p.ProveedorID,
                    numDocumento = p.NumDocumento,
                    razonSocial = p.RazonSocial,
                    monedaHabitual = p.MonedaHabitual,
                    formaPagoHabitual = p.FormaPagoHabitual
                })
                .AsNoTracking()
                .ToListAsync();

            return new JsonResult(resultados);
        }

        // HANDLER AJAX: CREAR PROVEEDOR EXPRESS
        public async Task<JsonResult> OnPostCrearProveedorExpressAsync([FromBody] Proveedor nuevoProv)
        {
            if (string.IsNullOrWhiteSpace(nuevoProv.NumDocumento) || string.IsNullOrWhiteSpace(nuevoProv.RazonSocial))
            {
                return new JsonResult(new { success = false, message = "El RUC/Documento y la Razón Social son requeridos." });
            }

            bool existe = await _context.Proveedores.AnyAsync(p => p.NumDocumento == nuevoProv.NumDocumento);
            if (existe)
            {
                var provExistente = await _context.Proveedores.FirstOrDefaultAsync(p => p.NumDocumento == nuevoProv.NumDocumento);
                return new JsonResult(new { success = true, proveedor = provExistente, message = "El proveedor ya existía y fue seleccionado." });
            }

            nuevoProv.Estado = true;
            nuevoProv.FechaRegistro = DateTime.Now;

            _context.Proveedores.Add(nuevoProv);
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true, proveedor = nuevoProv });
        }

        // HANDLER AJAX: CONSULTAR SUNAT VÍA OPENRUC
        public async Task<JsonResult> OnGetConsultarRucAsync(string ruc)
        {
            if (string.IsNullOrWhiteSpace(ruc) || ruc.Length != 11)
            {
                return new JsonResult(new { success = false, message = "El RUC debe contener exactamente 11 dígitos." });
            }

            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "ERP-System-App");

                var response = await httpClient.GetAsync($"https://openruc.com/api/ruc/{ruc}");

                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadFromJsonAsync<OpenRucDTO>();
                    if (data != null && !string.IsNullOrWhiteSpace(data.RazonSocial))
                    {
                        return new JsonResult(new
                        {
                            success = true,
                            razonSocial = data.RazonSocial,
                            direccion = data.Direccion,
                            ubigeoID = data.Ubigeo
                        });
                    }
                }
                return new JsonResult(new { success = false, message = "No se encontraron datos en SUNAT." });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = $"Error de consulta: {ex.Message}" });
            }
        }

        public async Task<IActionResult> OnPostGuardarOCAsync()
        {
            if (string.IsNullOrWhiteSpace(ItemsJson) || ItemsJson == "[]")
            {
                MensajeError = "Debe especificar los precios de los ítems a comprar.";
                return RedirectToPage(new { requerimientoId = InputOC.RequerimientoID });
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var detallesDTO = JsonSerializer.Deserialize<List<ItemOCDTO>>(ItemsJson, options);

            if (detallesDTO == null || !detallesDTO.Any())
            {
                MensajeError = "El detalle de la Orden de Compra es inválido.";
                return RedirectToPage(new { requerimientoId = InputOC.RequerimientoID });
            }

            decimal subTotal = detallesDTO.Sum(d => d.Cantidad * d.PrecioUnitario);
            decimal igv = subTotal * 0.18m;
            decimal total = subTotal + igv;

            InputOC.SubTotal = subTotal;
            InputOC.IGV = igv;
            InputOC.Total = total;
            InputOC.Estado = "Emitido";

            _context.OrdenesCompraCabecera.Add(InputOC);
            await _context.SaveChangesAsync();

            foreach (var item in detallesDTO)
            {
                _context.OrdenesCompraDetalle.Add(new OrdenesCompraDetalle
                {
                    OrdenCompraID = InputOC.OrdenCompraID,
                    ProductoID = item.ProductoID > 0 ? item.ProductoID : null,
                    DescripcionItem = item.DescripcionItem,
                    Cantidad = item.Cantidad,
                    UnidadMedida = item.UnidadMedida,
                    PrecioUnitario = item.PrecioUnitario,
                    SubTotalItem = item.Cantidad * item.PrecioUnitario
                });
            }

            var reqOrigen = await _context.RequerimientosCabecera.FindAsync(InputOC.RequerimientoID);
            if (reqOrigen != null)
            {
                reqOrigen.Estado = "Atendido";
                _context.RequerimientosCabecera.Update(reqOrigen);
            }

            await _context.SaveChangesAsync();

            MensajeExito = $"Orden de Compra {InputOC.Codigo} generada exitosamente por un Total de {InputOC.Moneda} {total:N2}.";
            return RedirectToPage("/Compras/BandejaRequerimientos");
        }

        public class ItemOCDTO
        {
            public int ProductoID { get; set; }
            public string DescripcionItem { get; set; } = string.Empty;
            public decimal Cantidad { get; set; }
            public string UnidadMedida { get; set; } = "UND";
            public decimal PrecioUnitario { get; set; }
        }

        public class OpenRucDTO
        {
            [JsonPropertyName("razon_social")]
            public string RazonSocial { get; set; } = string.Empty;

            [JsonPropertyName("direccion")]
            public string Direccion { get; set; } = string.Empty;

            [JsonPropertyName("ubigeo")]
            public string Ubigeo { get; set; } = string.Empty;
        }
    }
}