using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ERP.WEB.Data;
using ERP.WEB.Models.Logistica;

namespace ERP.WEB.Pages.Solicitudes
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<RequerimientoCabecera> ListaRequerimientos { get; set; } = new();

        // Filtros opcionales
        [BindProperty(SupportsGet = true)]
        public string? TipoFiltro { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? EstadoFiltro { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Busqueda { get; set; }

        public bool EsAdmin { get; set; } = false;

        public async Task OnGetAsync()
        {
            // ID del usuario logueado (Simulado o desde User.Identity / Claims)
            int usuarioActualID = 1;

            // Evaluar si el usuario tiene rol Admin
            EsAdmin = User.IsInRole("Admin") || User.IsInRole("Administrador");

            var query = _context.RequerimientosCabecera
                .Include(r => r.Detalles)
                .AsNoTracking()
                .AsQueryable();

            // REGLA DE NEGOCIO: Si no es Admin, solo ve sus propios requerimientos
            if (!EsAdmin)
            {
                query = query.Where(r => r.SolicitanteID == usuarioActualID);
            }

            // Filtros opcionales
            if (!string.IsNullOrWhiteSpace(TipoFiltro))
            {
                query = query.Where(r => r.TipoRequerimiento == TipoFiltro);
            }

            if (!string.IsNullOrWhiteSpace(EstadoFiltro))
            {
                query = query.Where(r => r.Estado == EstadoFiltro);
            }

            if (!string.IsNullOrWhiteSpace(Busqueda))
            {
                string t = Busqueda.Trim().ToLower();
                query = query.Where(r => r.Codigo.ToLower().Contains(t) || (r.Observaciones != null && r.Observaciones.ToLower().Contains(t)));
            }

            ListaRequerimientos = await query
                .OrderByDescending(r => r.FechaSolicitud)
                .ToListAsync();
        }
    }
}