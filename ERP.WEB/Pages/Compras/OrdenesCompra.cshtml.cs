using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ERP.WEB.Data;
using ERP.WEB.Models.Logistica;

namespace ERP.WEB.Pages.Compras
{
    [Authorize]
    public class OrdenesCompraModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public OrdenesCompraModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<OrdenesCompraCabecera> ListaOrdenes { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? EstadoFiltro { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Busqueda { get; set; }

        [TempData]
        public string? MensajeExito { get; set; }

        [TempData]
        public string? MensajeError { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.OrdenesCompraCabecera
                .Include(o => o.Detalles)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(EstadoFiltro))
            {
                query = query.Where(o => o.Estado == EstadoFiltro);
            }

            if (!string.IsNullOrWhiteSpace(Busqueda))
            {
                string t = Busqueda.Trim().ToLower();
                query = query.Where(o => o.Codigo.ToLower().Contains(t));
            }

            ListaOrdenes = await query.OrderByDescending(o => o.FechaEmision).ToListAsync();
        }

        // HANDLER PARA EDICIÓN DE PRECIOS Y CANTIDADES DESDE LOGÍSTICA
        public async Task<IActionResult> OnPostActualizarDatosOCAsync(int ordenCompraId, Dictionary<int, decimal> cantidades, Dictionary<int, decimal> precios)
        {
            var oc = await _context.OrdenesCompraCabecera
                .Include(o => o.Detalles)
                .FirstOrDefaultAsync(o => o.OrdenCompraID == ordenCompraId);

            if (oc == null || oc.Estado == "Atendido" || oc.Estado == "Anulado")
            {
                MensajeError = "La Orden de Compra no existe o no se puede modificar en su estado actual.";
                return RedirectToPage();
            }

            foreach (var det in oc.Detalles)
            {
                if (cantidades.ContainsKey(det.OrdenCompraDetalleID))
                {
                    det.Cantidad = cantidades[det.OrdenCompraDetalleID];
                }
                if (precios.ContainsKey(det.OrdenCompraDetalleID))
                {
                    det.PrecioUnitario = precios[det.OrdenCompraDetalleID];
                }
                det.SubTotalItem = det.Cantidad * det.PrecioUnitario;
            }

            // Recalcular Totales de Cabecera
            oc.SubTotal = oc.Detalles.Sum(d => d.SubTotalItem);
            oc.IGV = oc.SubTotal * 0.18m;
            oc.Total = oc.SubTotal + oc.IGV;

            _context.OrdenesCompraCabecera.Update(oc);
            await _context.SaveChangesAsync();

            MensajeExito = $"Orden de Compra {oc.Codigo} actualizada correctamente.";
            return RedirectToPage();
        }
    }
}