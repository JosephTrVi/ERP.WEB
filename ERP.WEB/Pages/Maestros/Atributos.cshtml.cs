using ERP.WEB.Data;
using ERP.WEB.Models.Inventario;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ERP.WEB.Pages.Maestros
{
    [Authorize]
    public class AtributosModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public AtributosModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Atributo> ListaAtributos { get; set; } = new();

        [BindProperty]
        public Atributo AtributoForm { get; set; } = new();

        // FILTROS
        [BindProperty(SupportsGet = true)]
        public int? CategoriaID { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? SubcategoriaID { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Busqueda { get; set; }

        public SelectList SelectCategorias { get; set; } = null!;
        public SelectList SelectSubcategorias { get; set; } = null!;

        [TempData]
        public string? MensajeExito { get; set; }

        [TempData]
        public string? MensajeError { get; set; }

        public async Task OnGetAsync()
        {
            await CargarCombosFiltroAsync();

            var query = _context.Atributos.AsNoTracking().AsQueryable();

            // 1. Filtro por Búsqueda de Código o Nombre
            if (!string.IsNullOrWhiteSpace(Busqueda))
            {
                string term = Busqueda.Trim().ToLower();
                query = query.Where(a => a.NombreAtributo.ToLower().Contains(term) ||
                                         a.AtributoID.ToString().Contains(term));
            }

            // 2. Filtro si los atributos están asociados a productos de cierta Categoría / Subcategoría
            if (SubcategoriaID.HasValue && SubcategoriaID.Value > 0)
            {
                var atributosSubcatIDs = await _context.ProductoAtributos
                    .Where(pa => pa.Producto != null && pa.Producto.SubcategoriaID == SubcategoriaID.Value)
                    .Select(pa => pa.AtributoID)
                    .Distinct()
                    .ToListAsync();

                query = query.Where(a => atributosSubcatIDs.Contains(a.AtributoID));
            }
            else if (CategoriaID.HasValue && CategoriaID.Value > 0)
            {
                var atributosCatIDs = await _context.ProductoAtributos
                    .Where(pa => pa.Producto != null && pa.Producto.CategoriaID == CategoriaID.Value)
                    .Select(pa => pa.AtributoID)
                    .Distinct()
                    .ToListAsync();

                query = query.Where(a => atributosCatIDs.Contains(a.AtributoID));
            }

            ListaAtributos = await query
                .OrderBy(a => a.NombreAtributo)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostGuardarAsync()
        {
            if (string.IsNullOrWhiteSpace(AtributoForm.NombreAtributo))
            {
                MensajeError = "El nombre de la especificación/atributo es obligatorio.";
                return RedirectToPage();
            }

            try
            {
                if (AtributoForm.AtributoID == 0)
                {
                    AtributoForm.Estado = true;
                    _context.Atributos.Add(AtributoForm);
                    MensajeExito = $"Atributo '{AtributoForm.NombreAtributo}' registrado con éxito.";
                }
                else
                {
                    var atributoBD = await _context.Atributos.FindAsync(AtributoForm.AtributoID);
                    if (atributoBD != null)
                    {
                        atributoBD.NombreAtributo = AtributoForm.NombreAtributo.Trim();
                        atributoBD.UnidadMedida = AtributoForm.UnidadMedida?.Trim();
                        atributoBD.TipoDato = AtributoForm.TipoDato;
                        _context.Atributos.Update(atributoBD);
                        MensajeExito = $"Atributo '{AtributoForm.NombreAtributo}' actualizado correctamente.";
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                MensajeError = $"Error al procesar la operación: {ex.Message}";
            }

            return RedirectToPage(new { CategoriaID, SubcategoriaID, Busqueda });
        }

        public async Task<IActionResult> OnPostCambiarEstadoAsync(int id)
        {
            var atributo = await _context.Atributos.FindAsync(id);
            if (atributo != null)
            {
                atributo.Estado = !atributo.Estado;
                _context.Atributos.Update(atributo);
                await _context.SaveChangesAsync();
                MensajeExito = $"Estado del atributo '{atributo.NombreAtributo}' modificado correctamente.";
            }

            return RedirectToPage(new { CategoriaID, SubcategoriaID, Busqueda });
        }

        private async Task CargarCombosFiltroAsync()
        {
            var categorias = await _context.Categorias
                .Where(c => c.Estado)
                .OrderBy(c => c.Nombre)
                .Select(c => new { c.CategoriaID, Nombre = $"[{c.Codigo}] {c.Nombre}" })
                .ToListAsync();

            SelectCategorias = new SelectList(categorias, "CategoriaID", "Nombre", CategoriaID);

            var querySubcat = _context.SubCategorias.Where(s => s.Estado).AsQueryable();
            if (CategoriaID.HasValue && CategoriaID.Value > 0)
            {
                querySubcat = querySubcat.Where(s => s.CategoriaID == CategoriaID.Value);
            }

            var subcategorias = await querySubcat
                .OrderBy(s => s.Nombre)
                .Select(s => new { s.SubCategoriaID, Nombre = $"[{s.Codigo}] {s.Nombre}" })
                .ToListAsync();

            SelectSubcategorias = new SelectList(subcategorias, "SubCategoriaID", "Nombre", SubcategoriaID);
        }
    }
}