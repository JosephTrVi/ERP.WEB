using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
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

        public Dictionary<int, decimal> StocksProductos { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? Anio { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? Mes { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? BuscarCliente { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? BuscarArticulo { get; set; }

        public List<SelectListItem> ListaAnios { get; set; } = new();
        public List<SelectListItem> ListaMeses { get; set; } = new();

        [TempData]
        public string? MensajeExito { get; set; }

        [TempData]
        public string? MensajeError { get; set; }

        public async Task OnGetAsync()
        {
            CargarCombosFiltro();

            // 1. Obtener la lista de CotizacionID que YA tienen una Orden de Producción generada
            var cotizacionesProcesadasIds = await _context.OrdenesProduccion
                .Select(o => o.CotizacionID)
                .Distinct()
                .ToListAsync();

            // 2. Consulta de Cotizaciones Aprobadas EXCLUYENDO las que ya fueron procesadas como OP
            var queryCot = _context.CotizacionesCabecera
                .Include(c => c.Detalles)
                .Where(c => c.Estado != null && c.Estado.Trim().ToLower() == "aprobada")
                .Where(c => !cotizacionesProcesadasIds.Contains(c.CotizacionID)) // <--- FILTRO CLAVE: Excluye cotizaciones con OP
                .AsQueryable();

            if (Anio.HasValue && Anio.Value > 0)
            {
                queryCot = queryCot.Where(c => (c.FechaAprobacion.HasValue && c.FechaAprobacion.Value.Year == Anio.Value)
                                            || (!c.FechaAprobacion.HasValue && c.FechaEmision.Year == Anio.Value));
            }

            if (Mes.HasValue && Mes.Value > 0)
            {
                queryCot = queryCot.Where(c => (c.FechaAprobacion.HasValue && c.FechaAprobacion.Value.Month == Mes.Value)
                                            || (!c.FechaAprobacion.HasValue && c.FechaEmision.Month == Mes.Value));
            }

            if (!string.IsNullOrWhiteSpace(BuscarCliente))
            {
                string term = BuscarCliente.Trim().ToLower();
                queryCot = queryCot.Where(c => c.ClienteNombre != null && c.ClienteNombre.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(BuscarArticulo))
            {
                string termArt = BuscarArticulo.Trim().ToLower();
                queryCot = queryCot.Where(c => c.Detalles.Any(d => d.DescripcionItem != null && d.DescripcionItem.ToLower().Contains(termArt)));
            }

            CotizacionesAprobadas = await queryCot
                .OrderByDescending(c => c.FechaAprobacion ?? c.FechaEmision)
                .AsNoTracking()
                .ToListAsync();

            // 3. Consultar Stock Actual usando _context.Productos
            var productoIds = CotizacionesAprobadas
                .SelectMany(c => c.Detalles)
                .Where(d => d.ProductoID.HasValue)
                .Select(d => d.ProductoID!.Value)
                .Distinct()
                .ToList();

            if (productoIds.Any())
            {
                StocksProductos = await _context.Productos
                    .Where(a => productoIds.Contains(a.ProductoID))
                    .AsNoTracking()
                    .ToDictionaryAsync(a => a.ProductoID, a => a.StockActual);
            }

            // 4. Consulta de Órdenes de Producción registradas en Planta
            var queryOrdenes = _context.OrdenesProduccion
                .Include(o => o.Cotizacion)
                .AsQueryable();

            if (Anio.HasValue && Anio.Value > 0)
            {
                queryOrdenes = queryOrdenes.Where(o => o.FechaEmision.Year == Anio.Value);
            }

            if (Mes.HasValue && Mes.Value > 0)
            {
                queryOrdenes = queryOrdenes.Where(o => o.FechaEmision.Month == Mes.Value);
            }

            if (!string.IsNullOrWhiteSpace(BuscarCliente))
            {
                string term = BuscarCliente.Trim().ToLower();
                queryOrdenes = queryOrdenes.Where(o => o.Cotizacion != null && o.Cotizacion.ClienteNombre != null && o.Cotizacion.ClienteNombre.ToLower().Contains(term));
            }

            OrdenesGeneradas = await queryOrdenes
                .OrderByDescending(o => o.FechaEmision)
                .AsNoTracking()
                .ToListAsync();
        }

        private void CargarCombosFiltro()
        {
            int anioActual = DateTime.Now.Year;
            ListaAnios.Add(new SelectListItem { Value = "", Text = "-- Todos los años --" });
            for (int i = anioActual; i >= anioActual - 4; i--)
            {
                ListaAnios.Add(new SelectListItem { Value = i.ToString(), Text = i.ToString() });
            }

            ListaMeses = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "-- Todos los meses --" },
                new SelectListItem { Value = "1", Text = "Enero" },
                new SelectListItem { Value = "2", Text = "Febrero" },
                new SelectListItem { Value = "3", Text = "Marzo" },
                new SelectListItem { Value = "4", Text = "Abril" },
                new SelectListItem { Value = "5", Text = "Mayo" },
                new SelectListItem { Value = "6", Text = "Junio" },
                new SelectListItem { Value = "7", Text = "Julio" },
                new SelectListItem { Value = "8", Text = "Agosto" },
                new SelectListItem { Value = "9", Text = "Septiembre" },
                new SelectListItem { Value = "10", Text = "Octubre" },
                new SelectListItem { Value = "11", Text = "Noviembre" },
                new SelectListItem { Value = "12", Text = "Diciembre" }
            };
        }

        public async Task<IActionResult> OnPostGenerarOrdenAsync(int cotizacionId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // VALIDACIÓN DE SEGURIDAD EN BACKEND: Verificar si ya tiene una OP creada
                bool yaTieneOrden = await _context.OrdenesProduccion.AnyAsync(o => o.CotizacionID == cotizacionId);
                if (yaTieneOrden)
                {
                    MensajeError = "La cotización seleccionada ya fue procesada anteriormente y cuenta con una Órden de Producción.";
                    return RedirectToPage();
                }

                var cotizacion = await _context.CotizacionesCabecera
                    .Include(c => c.Detalles)
                    .FirstOrDefaultAsync(c => c.CotizacionID == cotizacionId && c.Estado == "Aprobada");

                if (cotizacion == null)
                {
                    MensajeError = "La cotización seleccionada no existe o no se encuentra en estado Aprobada.";
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

                // Cambiar el estado comercial de la cotización para evitar reprocesos
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