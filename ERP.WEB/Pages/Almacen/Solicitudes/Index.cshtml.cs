using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ERP.WEB.Data;
using ERP.WEB.Models.Produccion;

namespace ERP.WEB.Pages.Almacen.Solicitudes
{
    [Authorize]
    [ValidateAntiForgeryToken]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<OrdenProduccion> SolicitudesPendientes { get; set; } = new();
        public List<OrdenProduccion> DespachosRealizados { get; set; } = new();

        public Dictionary<int, string> NombresProductos { get; set; } = new();
        public Dictionary<int, decimal> StocksActuales { get; set; } = new();

        [TempData]
        public string? MensajeExito { get; set; }

        [TempData]
        public string? MensajeError { get; set; }

        public async Task OnGetAsync()
        {
            // 1. Obtener solicitudes que Producción envió a Almacén
            SolicitudesPendientes = await _context.OrdenesProduccion
                .Include(o => o.Cotizacion)
                .Include(o => o.Consumos)
                .Where(o => o.Estado == "Solicitado a Almacén")
                .OrderByDescending(o => o.FechaEmision)
                .AsNoTracking()
                .ToListAsync();

            // 2. Obtener historial reciente de despachos aprobados
            DespachosRealizados = await _context.OrdenesProduccion
                .Include(o => o.Cotizacion)
                .Where(o => o.Estado == "En Proceso" || o.Estado == "Finalizada")
                .OrderByDescending(o => o.FechaEmision)
                .Take(20)
                .AsNoTracking()
                .ToListAsync();

            // 3. Cargar nombres y stocks de productos para la vista rápida
            var todosInsumoIds = SolicitudesPendientes
                .SelectMany(s => s.Consumos)
                .Select(c => c.InsumoID)
                .Distinct()
                .ToList();

            if (todosInsumoIds.Any())
            {
                NombresProductos = await _context.Productos
                    .Where(p => todosInsumoIds.Contains(p.ProductoID))
                    .AsNoTracking()
                    .ToDictionaryAsync(p => p.ProductoID, p => p.Nombre);

                StocksActuales = await _context.Productos
                    .Where(p => todosInsumoIds.Contains(p.ProductoID))
                    .AsNoTracking()
                    .ToDictionaryAsync(p => p.ProductoID, p => p.StockActual);
            }
        }

        // ACCIÓN: Almacén aprueba la solicitud, descuenta Kardex y genera despacho
        public async Task<IActionResult> OnPostAprobarDespachoAsync(int ordenId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var orden = await _context.OrdenesProduccion
                    .Include(o => o.Consumos)
                    .FirstOrDefaultAsync(o => o.OrdenProduccionID == ordenId);

                if (orden == null || orden.Estado != "Solicitado a Almacén")
                {
                    MensajeError = "La solicitud no se encuentra en estado 'Solicitado a Almacén'.";
                    return RedirectToPage();
                }

                // Validar Stock en Almacén
                var insumoIds = orden.Consumos.Select(c => c.InsumoID).Distinct().ToList();
                var productosInsumos = await _context.Productos
                    .Where(p => insumoIds.Contains(p.ProductoID))
                    .ToListAsync();

                foreach (var insumo in orden.Consumos)
                {
                    var prod = productosInsumos.FirstOrDefault(p => p.ProductoID == insumo.InsumoID);
                    decimal stockActual = prod?.StockActual ?? 0;

                    if (stockActual < insumo.CantidadRequerida)
                    {
                        MensajeError = $"No se puede despachar. Stock insuficiente para el Insumo ID #{insumo.InsumoID} (Requerido: {insumo.CantidadRequerida:N2}, Stock Almacén: {stockActual:N2}).";
                        return RedirectToPage();
                    }
                }

                // Descontar físicamente del Kardex (_context.Productos)
                foreach (var insumo in orden.Consumos)
                {
                    var prod = productosInsumos.First(p => p.ProductoID == insumo.InsumoID);
                    prod.StockActual -= insumo.CantidadRequerida;
                    insumo.CantidadConsumida = insumo.CantidadRequerida;
                }

                // Pasar la orden a 'En Proceso' para que Producción empiece a trabajar
                orden.Estado = "En Proceso";
                orden.Observaciones = $"Despachado por Almacén el {DateTime.Now:dd/MM/yyyy HH:mm}";

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                MensajeExito = $"¡Solicitud OP-{(orden.OrdenProduccionID.ToString("D5"))} APROBADA y DESPACHADA! Se descontó el inventario en Almacén.";

                // Redirigir directamente a la generación e impresión del Vale de Salida
                return RedirectToPage("/Produccion/Ordenes/NotaSalida", new { id = ordenId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                MensajeError = $"Error al procesar el despacho: {ex.Message}";
                return RedirectToPage();
            }
        }
    }
}