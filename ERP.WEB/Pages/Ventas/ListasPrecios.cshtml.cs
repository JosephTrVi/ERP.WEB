using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ERP.WEB.Data;
using ERP.WEB.Models.Ventas;

namespace ERP.WEB.Pages.Ventas
{
    [Authorize]
    public class ListasPreciosModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ListasPreciosModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<ListasPreciosCabecera> ListaListas { get; set; } = new();

        [TempData]
        public string? MensajeExito { get; set; }

        [TempData]
        public string? MensajeError { get; set; }

        public async Task OnGetAsync()
        {
            ListaListas = await _context.ListasPreciosCabecera
                .Include(l => l.Detalles)
                .AsNoTracking()
                .OrderByDescending(l => l.Anio)
                .ThenByDescending(l => l.Mes)
                .ToListAsync();
        }

        // HANDLER: COPIAR / CLONAR LISTA DE UN MES A OTRO
        public async Task<IActionResult> OnPostClonarListaAsync(int listaOrigenId, int mesDestino, int anioDestino)
        {
            var origen = await _context.ListasPreciosCabecera
                .Include(l => l.Detalles)
                .FirstOrDefaultAsync(l => l.ListaPrecioID == listaOrigenId);

            if (origen == null)
            {
                MensajeError = "La lista origen no existe.";
                return RedirectToPage();
            }

            bool existeDestino = await _context.ListasPreciosCabecera
                .AnyAsync(l => l.VendedorID == origen.VendedorID && l.Anio == anioDestino && l.Mes == mesDestino);

            if (existeDestino)
            {
                MensajeError = $"Ya existe una lista registrada para el mes {mesDestino}/{anioDestino}.";
                return RedirectToPage();
            }

            var nuevaLista = new ListasPreciosCabecera
            {
                VendedorID = origen.VendedorID,
                Anio = anioDestino,
                Mes = mesDestino,
                NombreLista = $"Lista {mesDestino}/{anioDestino} - Vendedor #{origen.VendedorID}",
                Estado = true,
                FechaRegistro = DateTime.Now
            };

            _context.ListasPreciosCabecera.Add(nuevaLista);
            await _context.SaveChangesAsync();

            foreach (var det in origen.Detalles)
            {
                _context.ListasPreciosDetalle.Add(new ListasPreciosDetalle
                {
                    ListaPrecioID = nuevaLista.ListaPrecioID,
                    ProductoID = det.ProductoID,
                    PrecioVentaSinIGV = det.PrecioVentaSinIGV,
                    Moneda = det.Moneda
                });
            }

            await _context.SaveChangesAsync();
            MensajeExito = $"Lista clonada exitosamente para el periodo {mesDestino}/{anioDestino}.";
            return RedirectToPage();
        }
    }
}