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
        public Categoria InputCategoria { get; set; } = new();

        [BindProperty]
        public SubCategoria InputSubCategoria { get; set; } = new();

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
            var categoriaPadre = await _context.Categorias.FindAsync(categoriaId);
            string prefijoCat = categoriaPadre != null && !string.IsNullOrEmpty(categoriaPadre.Codigo)
                ? categoriaPadre.Codigo.PadLeft(2, '0')
                : categoriaId.ToString("D2");

            var ultimaSubCat = await _context.SubCategorias
                .Where(s => s.CategoriaID == categoriaId)
                .OrderByDescending(s => s.SubCategoriaID)
                .FirstOrDefaultAsync();

            int correlativo = 1;

            if (ultimaSubCat != null && !string.IsNullOrEmpty(ultimaSubCat.Codigo))
            {
                string sufijo = ultimaSubCat.Codigo.Length >= 2
                    ? ultimaSubCat.Codigo.Substring(ultimaSubCat.Codigo.Length - 2)
                    : ultimaSubCat.Codigo;

                if (int.TryParse(sufijo, out int ultimoNum))
                {
                    correlativo = ultimoNum + 1;
                }
            }

            string siguienteCodigo = $"{prefijoCat}{correlativo.ToString("D2")}";
            return new JsonResult(new { codigo = siguienteCodigo });
        }

        // =========================================================
        // HANDLER: GUARDAR CATEGORÍA
        // =========================================================
        public async Task<IActionResult> OnPostGuardarCategoriaAsync()
        {
            // Limpiar ModelState por completo para evitar que la validación de SubCategoría interfiera
            ModelState.Clear();

            if (InputCategoria == null || string.IsNullOrWhiteSpace(InputCategoria.Codigo) || string.IsNullOrWhiteSpace(InputCategoria.Nombre))
            {
                MensajeError = "El Código y Nombre de la categoría son obligatorios.";
                await OnGetAsync();
                return Page();
            }

            // Validar manualmente la entidad de Categoría
            if (!TryValidateModel(InputCategoria, nameof(InputCategoria)))
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
                    catBD.EsReceta = InputCategoria.EsReceta;
                    catBD.EsComponente = InputCategoria.EsComponente;

                    _context.Categorias.Update(catBD);
                    MensajeExito = "Categoría actualizada correctamente.";
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToPage(new { CategoriaSeleccionadaID = InputCategoria.CategoriaID > 0 ? InputCategoria.CategoriaID : (int?)null });
        }

        // =========================================================
        // HANDLER: GUARDAR SUBCATEGORÍA
        // =========================================================
        public async Task<IActionResult> OnPostGuardarSubCategoriaAsync()
        {
            // Limpiar ModelState por completo para evitar que la validación de Categoría interfiera
            ModelState.Clear();

            if (InputSubCategoria == null || string.IsNullOrWhiteSpace(InputSubCategoria.Codigo) || string.IsNullOrWhiteSpace(InputSubCategoria.Nombre))
            {
                MensajeError = "El Código y Nombre de la subcategoría son obligatorios.";
                await OnGetAsync();
                return Page();
            }

            // Validar manualmente solo la entidad de SubCategoría
            if (!TryValidateModel(InputSubCategoria, nameof(InputSubCategoria)))
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
                        subCatBD.EsReceta = InputSubCategoria.EsReceta;
                        subCatBD.EsComponente = InputSubCategoria.EsComponente;

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

        public async Task<JsonResult> OnGetObtenerSubCategoriasAsync(int categoriaId)
        {
            var subcategorias = await _context.SubCategorias
                .Where(s => s.CategoriaID == categoriaId)
                .Select(s => new {
                    subCategoriaID = s.SubCategoriaID,
                    categoriaID = s.CategoriaID,
                    codigo = s.Codigo,
                    nombre = s.Nombre,
                    estado = s.Estado,
                    esReceta = s.EsReceta,
                    esComponente = s.EsComponente
                })
                .AsNoTracking()
                .ToListAsync();

            return new JsonResult(subcategorias);
        }

        public async Task<JsonResult> OnGetSubCategoriasPorCategoriaAsync(int categoriaId)
        {
            return await OnGetObtenerSubCategoriasAsync(categoriaId);
        }
    }
}