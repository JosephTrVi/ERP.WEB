using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ERP.WEB.Data;
using ERP.WEB.Models.Produccion;

namespace ERP.WEB.Pages.Produccion.Ordenes
{
    [Authorize]
    [ValidateAntiForgeryToken]
    public class DetalleModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetalleModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public OrdenProduccion Orden { get; set; } = default!;

        public Dictionary<int, decimal> StocksInsumos { get; set; } = new();
        public Dictionary<int, string> NombresInsumos { get; set; } = new();
        public Dictionary<int, string> SkuInsumos { get; set; } = new();

        [TempData]
        public string? MensajeExito { get; set; }

        [TempData]
        public string? MensajeError { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var orden = await _context.OrdenesProduccion
                .Include(o => o.Cotizacion)
                .Include(o => o.Consumos)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrdenProduccionID == id);

            if (orden == null)
            {
                MensajeError = "La Orden de Producción solicitada no existe.";
                return RedirectToPage("/Produccion/Ordenes/Index");
            }

            Orden = orden;

            var insumoIds = Orden.Consumos
                .Select(c => c.InsumoID)
                .Distinct()
                .ToList();

            if (insumoIds.Any())
            {
                StocksInsumos = await _context.Productos
                    .Where(a => insumoIds.Contains(a.ProductoID))
                    .AsNoTracking()
                    .ToDictionaryAsync(a => a.ProductoID, a => a.StockActual);

                NombresInsumos = await _context.Productos
                    .Where(a => insumoIds.Contains(a.ProductoID))
                    .AsNoTracking()
                    .ToDictionaryAsync(a => a.ProductoID, a => a.Nombre);

                SkuInsumos = await _context.Productos
                    .Where(a => insumoIds.Contains(a.ProductoID))
                    .AsNoTracking()
                    .ToDictionaryAsync(a => a.ProductoID, a => a.SKU ?? "S/SKU");
            }

            return Page();
        }

        // --- HANDLER 1: INICIAR PRODUCCIÓN (Paso de Pendiente -> En Proceso) ---
        public async Task<IActionResult> OnPostIniciarAsync(int ordenId)
        {
            var orden = await _context.OrdenesProduccion
                .Include(o => o.Consumos)
                .FirstOrDefaultAsync(o => o.OrdenProduccionID == ordenId);

            if (orden == null)
            {
                MensajeError = "La orden de producción no existe.";
                return RedirectToPage(new { id = ordenId });
            }

            if (orden.Estado != "Pendiente")
            {
                MensajeError = "Solo se pueden iniciar órdenes en estado Pendiente.";
                return RedirectToPage(new { id = ordenId });
            }

            // ESCENARIO DE VALIDACIÓN: Verificar si hay stock suficiente de TODOS los insumos
            var insumoIds = orden.Consumos.Select(c => c.InsumoID).Distinct().ToList();
            var stocks = await _context.Productos
                .Where(p => insumoIds.Contains(p.ProductoID))
                .ToDictionaryAsync(p => p.ProductoID, p => p.StockActual);

            foreach (var insumo in orden.Consumos)
            {
                decimal stockActual = stocks.ContainsKey(insumo.InsumoID) ? stocks[insumo.InsumoID] : 0;
                if (stockActual < insumo.CantidadRequerida)
                {
                    MensajeError = $"No se puede iniciar la producción. Stock insuficiente para el Insumo ID #{insumo.InsumoID} (Requerido: {insumo.CantidadRequerida:N2}, Stock: {stockActual:N2}).";
                    return RedirectToPage(new { id = ordenId });
                }
            }

            orden.Estado = "En Proceso";
            _context.OrdenesProduccion.Update(orden);
            await _context.SaveChangesAsync();

            MensajeExito = $"Orden OP-{(orden.OrdenProduccionID.ToString("D5"))} ha pasado a estado EN PROCESO.";
            return RedirectToPage(new { id = ordenId });
        }

        // --- HANDLER 2: FINALIZAR PRODUCCIÓN & DESCONTAR KARDEX ---
        public async Task<IActionResult> OnPostFinalizarAsync(int ordenId, decimal cantidadRealProducida, string? observacionesLote)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var orden = await _context.OrdenesProduccion
                    .Include(o => o.Consumos)
                    .FirstOrDefaultAsync(o => o.OrdenProduccionID == ordenId);

                if (orden == null || orden.Estado != "En Proceso")
                {
                    MensajeError = "Solo se pueden finalizar órdenes que estén 'En Proceso'.";
                    return RedirectToPage(new { id = ordenId });
                }

                if (cantidadRealProducida <= 0)
                {
                    MensajeError = "Debe ingresar una cantidad real producida mayor a 0.";
                    return RedirectToPage(new { id = ordenId });
                }

                // 1. DESCONTAR INSUMOS DEL STOCK (_context.Productos)
                var insumoIds = orden.Consumos.Select(c => c.InsumoID).Distinct().ToList();
                var productosInsumos = await _context.Productos
                    .Where(p => insumoIds.Contains(p.ProductoID))
                    .ToListAsync();

                foreach (var insumo in orden.Consumos)
                {
                    var prodInsumo = productosInsumos.FirstOrDefault(p => p.ProductoID == insumo.InsumoID);
                    if (prodInsumo != null)
                    {
                        prodInsumo.StockActual -= insumo.CantidadRequerida; // Descuento de inventario
                        insumo.CantidadConsumida = insumo.CantidadRequerida; // Registro de consumo real
                    }
                }

                // 2. INCREMENTAR STOCK DEL PRODUCTO PRODUCIDO (Ej: Carne Sazonada)
                var productoFabricado = await _context.Productos
                    .FirstOrDefaultAsync(p => p.ProductoID == orden.ProductoID);

                if (productoFabricado != null)
                {
                    productoFabricado.StockActual += cantidadRealProducida; // Ingreso a inventario de producto semielaborado/terminado
                }

                // 3. ACTUALIZAR ESTADO DE LA ORDEN
                orden.Estado = "Finalizada";
                orden.Observaciones = string.IsNullOrWhiteSpace(observacionesLote)
                    ? $"Fabricación completada. Cantidad real: {cantidadRealProducida:N2}"
                    : $"Lote: {observacionesLote} | Cantidad real: {cantidadRealProducida:N2}";

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                MensajeExito = $"¡Orden OP-{(orden.OrdenProduccionID.ToString("D5"))} FINALIZADA con éxito! Se descontaron los insumos y se ingresaron {cantidadRealProducida:N2} al inventario.";
                return RedirectToPage(new { id = ordenId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                MensajeError = $"Error al finalizar la producción: {ex.Message}";
                return RedirectToPage(new { id = ordenId });
            }
        }

        // HANDLER: Producción solicita formalmente los insumos a Almacén
        public async Task<IActionResult> OnPostSolicitarInsumosAsync(int ordenId)
        {
            var orden = await _context.OrdenesProduccion
                .Include(o => o.Consumos)
                .FirstOrDefaultAsync(o => o.OrdenProduccionID == ordenId);

            if (orden == null)
            {
                MensajeError = "La orden de producción no existe.";
                return RedirectToPage(new { id = ordenId });
            }

            if (orden.Estado != "Pendiente")
            {
                MensajeError = "La solicitud solo se puede enviar para órdenes en estado Pendiente.";
                return RedirectToPage(new { id = ordenId });
            }

            // Cambiar estado a 'Solicitado a Almacén'
            orden.Estado = "Solicitado a Almacén";
            orden.Observaciones = $"Solicitud de insumos enviada a Almacén el {DateTime.Now:dd/MM/yyyy HH:mm}";

            _context.OrdenesProduccion.Update(orden);
            await _context.SaveChangesAsync();

            MensajeExito = $"Solicitud de insumos para la OP-{(orden.OrdenProduccionID.ToString("D5"))} enviada con éxito a Almacén.";
            return RedirectToPage(new { id = ordenId });
        }
    }
}