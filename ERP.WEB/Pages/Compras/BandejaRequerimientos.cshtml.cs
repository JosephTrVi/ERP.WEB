using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ERP.WEB.Data;
using ERP.WEB.Models.Logistica;

namespace ERP.WEB.Pages.Compras
{
    [Authorize]
    public class BandejaRequerimientosModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public BandejaRequerimientosModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<RequerimientoCabecera> RequerimientosAprobados { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? TipoFiltro { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Busqueda { get; set; }

        [TempData]
        public string? MensajeExito { get; set; }

        [TempData]
        public string? MensajeError { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.RequerimientosCabecera
                .Include(r => r.Detalles)
                    .ThenInclude(d => d.Producto)
                .Where(r => r.Estado == "Aprobado")
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(TipoFiltro))
            {
                query = query.Where(r => r.TipoRequerimiento == TipoFiltro);
            }

            if (!string.IsNullOrWhiteSpace(Busqueda))
            {
                string t = Busqueda.Trim().ToLower();
                query = query.Where(r => r.Codigo.ToLower().Contains(t) || (r.Observaciones != null && r.Observaciones.ToLower().Contains(t)));
            }

            RequerimientosAprobados = await query
                .OrderByDescending(r => r.FechaAprobacion)
                .ToListAsync();
        }
    }
}