using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ERP.WEB.Data;
using ERP.WEB.Models.Logistica;

namespace ERP.WEB.Pages.Compras
{
    [Authorize]
    public class ProveedoresModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ProveedoresModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Proveedor> ListaProveedores { get; set; } = new();

        [BindProperty]
        public Proveedor InputProveedor { get; set; } = new();

        [TempData]
        public string? MensajeExito { get; set; }

        [TempData]
        public string? MensajeError { get; set; }

        public async Task OnGetAsync()
        {
            ListaProveedores = await _context.Proveedores
                .AsNoTracking()
                .OrderBy(p => p.RazonSocial)
                .ToListAsync();
        }

        public async Task<JsonResult> OnGetBuscarUbigeoAsync(string term)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 3)
                return new JsonResult(new List<object>());

            string t = term.Trim().ToLower();

            // Consulta la tabla Maestros.Ubigeo
            var resultados = await _context.Database
                .SqlQueryRaw<UbigeoResult>("SELECT UbigeoID, Departamento, Provincia, Distrito FROM Maestros.Ubigeo WHERE LOWER(Distrito) LIKE {0} OR LOWER(Provincia) LIKE {0} OR UbigeoID LIKE {0}", $"%{t}%")
                .Take(15)
                .ToListAsync();

            return new JsonResult(resultados);
        }

        public async Task<IActionResult> OnPostGuardarAsync()
        {
            if (string.IsNullOrWhiteSpace(InputProveedor.NumDocumento) || string.IsNullOrWhiteSpace(InputProveedor.RazonSocial))
            {
                MensajeError = "El número de documento y la Razón Social son requeridos.";
                return RedirectToPage();
            }

            if (InputProveedor.ProveedorID == 0)
            {
                bool existe = await _context.Proveedores.AnyAsync(p => p.NumDocumento == InputProveedor.NumDocumento);
                if (existe)
                {
                    MensajeError = "Ya existe un proveedor registrado con el mismo número de documento.";
                    return RedirectToPage();
                }

                _context.Proveedores.Add(InputProveedor);
                MensajeExito = $"Proveedor '{InputProveedor.RazonSocial}' registrado correctamente.";
            }
            else
            {
                _context.Proveedores.Update(InputProveedor);
                MensajeExito = $"Proveedor '{InputProveedor.RazonSocial}' actualizado correctamente.";
            }

            await _context.SaveChangesAsync();
            return RedirectToPage();
        }

        public class UbigeoResult
        {
            public string UbigeoID { get; set; } = string.Empty;
            public string Departamento { get; set; } = string.Empty;
            public string Provincia { get; set; } = string.Empty;
            public string Distrito { get; set; } = string.Empty;
        }
    }
}