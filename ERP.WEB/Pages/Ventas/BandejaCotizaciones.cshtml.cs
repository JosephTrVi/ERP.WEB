using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ERP.WEB.Data;
using ERP.WEB.Models.Ventas;

namespace ERP.WEB.Pages.Ventas
{
    [Authorize]
    public class BandejaCotizacionesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public BandejaCotizacionesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<CotizacionesCabecera> ListaCotizaciones { get; set; } = new();

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
            var query = _context.CotizacionesCabecera
                .Include(c => c.Detalles)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(EstadoFiltro))
            {
                query = query.Where(c => c.Estado == EstadoFiltro);
            }

            if (!string.IsNullOrWhiteSpace(Busqueda))
            {
                string t = Busqueda.Trim().ToLower();
                query = query.Where(c => c.Codigo.ToLower().Contains(t) || c.ClienteNombre.ToLower().Contains(t));
            }

            ListaCotizaciones = await query.OrderByDescending(c => c.FechaEmision).ToListAsync();
        }
    }
}