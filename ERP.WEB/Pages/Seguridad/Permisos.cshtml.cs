using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ERP.WEB.Data;
using ERP.WEB.Models.Seguridad;

namespace ERP.WEB.Pages.Seguridad
{
    public class PermisosModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public PermisosModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Permiso> ListaPermisos { get; set; } = new List<Permiso>();

        public async Task OnGetAsync()
        {
            ListaPermisos = await _context.Permisos.AsNoTracking().ToListAsync();
        }
    }
}