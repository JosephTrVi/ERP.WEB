using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ERP.WEB.Data;
using ERP.WEB.Models.Logistica;

namespace ERP.WEB.Pages.Almacen
{
    [Authorize]
    public class BandejaRecepcionModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public BandejaRecepcionModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<OrdenesCompraCabecera> OrdenesPendientes { get; set; } = new();

        [TempData]
        public string? MensajeExito { get; set; }

        public async Task OnGetAsync()
        {
            // Cargar únicamente órdenes Emitidas o Pendientes de Atención
            OrdenesPendientes = await _context.OrdenesCompraCabecera
                .Include(o => o.Detalles)
                .Where(o => o.Estado == "Emitido" || o.Estado == "Pendiente de Atención")
                .AsNoTracking()
                .OrderByDescending(o => o.FechaEmision)
                .ToListAsync();
        }
    }
}