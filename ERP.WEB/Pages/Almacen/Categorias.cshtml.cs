using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ERP.WEB.Data;
using ERP.WEB.Models.Inventario;

namespace ERP.WEB.Pages.Almacen
{
    [Authorize]
    public class CategoriasModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CategoriasModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Categoria> ListaCategorias { get; set; } = new();
        public List<SubCategoria> ListaSubCategorias { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? CategoriaSeleccionadaID { get; set; }

        [BindProperty]
        public Categoria? InputCategoria { get; set; }

        [BindProperty]
        public SubCategoria? InputSubCategoria { get; set; }

        [TempData]
        public string? MensajeExito { get; set; }

        [TempData]
        public string? MensajeError { get; set; }

        public async Task OnGetAsync()
        {
            ListaCategorias = await _context.Categorias.AsNoTracking().ToListAsync();

            if (CategoriaSeleccionadaID.HasValue && CategoriaSeleccionadaID > 0)
            {
                ListaSubCategorias = await _context.SubCategorias
                    .Where(s => s.CategoriaID == CategoriaSeleccionadaID.Value)
                    .AsNoTracking()
                    .ToListAsync();
            }
        }

        public async Task<JsonResult> OnGetSiguienteCodigoCategoriaAsync()
        {
            var ultimaCat = await _context.Categorias
                .OrderByDescending(c => c.CategoriaID)
                .FirstOrDefaultAsync();

            int correlativo = 1;

            if (ultimaCat != null && !string.IsNullOrEmpty(ultimaCat.Codigo))
            {
                if (int.TryParse(ultimaCat.Codigo, out int ultimoNum))
                {
                    correlativo = ultimoNum + 1;
                }
            }

            string siguienteCodigo = correlativo.ToString("D2");
            return new JsonResult(new { codigo = siguienteCodigo });
        }

        public async Task<JsonResult> OnGetSiguienteCodigoSubCategoriaAsync(int categoriaId)
        {
            var ultimaSubCat = await _context.SubCategorias
                .Where(s => s.CategoriaID == categoriaId)
                .OrderByDescending(s => s.SubCategoriaID)
                .FirstOrDefaultAsync();

            int correlativo = 1;

            if (ultimaSubCat != null && !string.IsNullOrEmpty(ultimaSubCat.Codigo))
            {
                if (int.TryParse(ultimaSubCat.Codigo, out int ultimoNum))
                {
                    correlativo = ultimoNum + 1;
                }
            }

            string siguienteCodigo = correlativo.ToString("D2");
            return new JsonResult(new { codigo = siguienteCodigo });
        }

        public async Task<IActionResult> OnPostGuardarCategoriaAsync()
        {
            // Eliminar todas las entradas de InputSubCategoria del ModelState
            var subCatKeys = ModelState.Keys.Where(k => k.StartsWith("InputSubCategoria")).ToList();
            foreach (var key in subCatKeys)
            {
                ModelState.Remove(key);
            }

            if (InputCategoria == null || string.IsNullOrWhiteSpace(InputCategoria.Nombre))
            {
                MensajeError = "El nombre de la categoría es obligatorio.";
                await OnGetAsync();
                return Page();
            }

            if (!ModelState.IsValid)
            {
                var errores = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                MensajeError = $"[ERROR DE VALIDACIÓN]: {errores}";
                await OnGetAsync();
                return Page();
            }

            if (InputCategoria.CategoriaID == 0)
            {
                _context.Categorias.Add(InputCategoria);
                MensajeExito = "Categoría registrada correctamente.";
            }
            else
            {
                var catBD = await _context.Categorias.FindAsync(InputCategoria.CategoriaID);
                if (catBD != null)
                {
                    catBD.Codigo = InputCategoria.Codigo;
                    catBD.Nombre = InputCategoria.Nombre;
                    catBD.Descripcion = InputCategoria.Descripcion;
                    catBD.Estado = InputCategoria.Estado;

                    _context.Categorias.Update(catBD);
                    MensajeExito = "Categoría actualizada correctamente.";
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToPage(new { CategoriaSeleccionadaID = InputCategoria.CategoriaID > 0 ? InputCategoria.CategoriaID : (int?)null });
        }

        public async Task<IActionResult> OnPostGuardarSubCategoriaAsync()
        {
            // Eliminar todas las entradas de InputCategoria y referencias de navegación del ModelState
            var catKeys = ModelState.Keys.Where(k => k.StartsWith("InputCategoria") || k.StartsWith("InputSubCategoria.Categoria")).ToList();
            foreach (var key in catKeys)
            {
                ModelState.Remove(key);
            }

            if (InputSubCategoria == null || string.IsNullOrWhiteSpace(InputSubCategoria.Nombre))
            {
                MensajeError = "El nombre de la subcategoría es obligatorio.";
                await OnGetAsync();
                return Page();
            }

            if (!ModelState.IsValid)
            {
                var errores = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                MensajeError = $"[ERROR DE VALIDACIÓN]: {errores}";
                await OnGetAsync();
                return Page();
            }

            try
            {
                if (InputSubCategoria.SubCategoriaID == 0)
                {
                    _context.SubCategorias.Add(InputSubCategoria);
                    await _context.SaveChangesAsync();
                    MensajeExito = "SubCategoría registrada correctamente.";
                }
                else
                {
                    var subCatBD = await _context.SubCategorias.FindAsync(InputSubCategoria.SubCategoriaID);
                    if (subCatBD != null)
                    {
                        subCatBD.Codigo = InputSubCategoria.Codigo;
                        subCatBD.Nombre = InputSubCategoria.Nombre;
                        subCatBD.Estado = InputSubCategoria.Estado;

                        _context.SubCategorias.Update(subCatBD);
                        await _context.SaveChangesAsync();
                        MensajeExito = "SubCategoría actualizada correctamente.";
                    }
                }

                return RedirectToPage(new { CategoriaSeleccionadaID = InputSubCategoria.CategoriaID });
            }
            catch (Exception ex)
            {
                MensajeError = $"[ERROR DE BASE DE DATOS]: {ex.InnerException?.Message ?? ex.Message}";
                await OnGetAsync();
                return Page();
            }
        }

        // HANDLER AJAX: Obtener subcategorías para el panel derecho sin recargar la página
        public async Task<JsonResult> OnGetObtenerSubCategoriasAsync(int categoriaId)
        {
            var subcategorias = await _context.SubCategorias
                .Where(s => s.CategoriaID == categoriaId)
                .Select(s => new {
                    subCategoriaID = s.SubCategoriaID,
                    categoriaID = s.CategoriaID,
                    codigo = s.Codigo,
                    nombre = s.Nombre,
                    estado = s.Estado
                })
                .AsNoTracking()
                .ToListAsync();

            return new JsonResult(subcategorias);
        }
    }
}