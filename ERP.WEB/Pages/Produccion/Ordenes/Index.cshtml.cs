using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ERP.WEB.Data;
using ERP.WEB.Models.Ventas;
using ERP.WEB.Models.Produccion;

namespace ERP.WEB.Pages.Produccion.Ordenes
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

        public List<CotizacionesCabecera> CotizacionesAprobadas { get; set; } = new();
        public List<OrdenProduccion> OrdenesGeneradas { get; set; } = new();

        [TempData]
        public string? MensajeExito { get; set; }

        [TempData]
        public string? MensajeError { get; set; }

        public async Task OnGetAsync()
        {
            // 1. Obtener Cotizaciones Aprobadas incluyendo sus detalles e información de Stock
            CotizacionesAprobadas = await _context.CotizacionesCabecera
                .Include(c => c.Detalles)
                .Where(c => c.Estado != null && c.Estado.Trim().ToLower() == "aprobada")
                .OrderByDescending(c => c.FechaAprobacion ?? c.FechaEmision)
                .AsNoTracking()
                .ToListAsync();

            // 2. Obtener la lista de Órdenes de Producción generadas
            OrdenesGeneradas = await _context.OrdenesProduccion
                .Include(o => o.Cotizacion)
                .OrderByDescending(o => o.FechaEmision)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostGenerarOrdenAsync(int cotizacionId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var cotizacion = await _context.CotizacionesCabecera
                    .Include(c => c.Detalles)
                    .FirstOrDefaultAsync(c => c.CotizacionID == cotizacionId && c.Estado == "Aprobada");

                if (cotizacion == null)
                {
                    MensajeError = "La cotización seleccionada no existe o no se encuentra aprobada.";
                    return RedirectToPage();
                }

                foreach (var detalle in cotizacion.Detalles)
                {
                    if (!detalle.ProductoID.HasValue) continue;

                    var receta = await _context.Recetas
                        .Include(r => r.Detalles)
                        .FirstOrDefaultAsync(r => r.ProductoTerminadoID == detalle.ProductoID.Value && r.Estado);

                    var nuevaOrden = new OrdenProduccion
                    {
                        CotizacionID = cotizacion.CotizacionID,
                        ProductoID = detalle.ProductoID.Value,
                        CantidadPlanificada = detalle.Cantidad,
                        FechaEmision = DateTime.Now,
                        Estado = "Pendiente",
                        Observaciones = $"Orden generada desde Cotización #{cotizacion.Codigo}"
                    };

                    _context.OrdenesProduccion.Add(nuevaOrden);
                    await _context.SaveChangesAsync();

                    if (receta != null && receta.Detalles != null && receta.Detalles.Any())
                    {
                        foreach (var recDetalle in receta.Detalles)
                        {
                            var consumo = new OrdenProduccionConsumo
                            {
                                OrdenProduccionID = nuevaOrden.OrdenProduccionID,
                                InsumoID = recDetalle.InsumoID,
                                CantidadRequerida = recDetalle.CantidadRequerida * detalle.Cantidad,
                                CantidadConsumida = 0,
                                UnidadMedida = recDetalle.UnidadMedida
                            };

                            _context.OrdenesProduccionConsumos.Add(consumo);
                        }
                    }
                }

                cotizacion.Estado = "En Producción";
                _context.CotizacionesCabecera.Update(cotizacion);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                MensajeExito = $"Explosión de Materiales completada. Se generaron las Órdenes de Producción para la Cotización #{cotizacion.Codigo}.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                MensajeError = $"Error al procesar la explosión de materiales: {ex.Message}";
                return RedirectToPage();
            }
        }
    }
}