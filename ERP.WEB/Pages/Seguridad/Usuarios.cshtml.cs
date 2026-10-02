using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ERP.WEB.Data;
using ERP.WEB.Models.DTOs;
using ERP.WEB.Models.Seguridad;
using ERP.WEB.Services;

namespace ERP.WEB.Pages.Seguridad
{
    public class UsuariosModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public UsuariosModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Usuario> ListaUsuarios { get; set; } = new List<Usuario>();

        [BindProperty]
        public CrearUsuarioDto NuevoUsuario { get; set; } = new CrearUsuarioDto();

        [TempData]
        public string? MensajeExito { get; set; }

        public async Task OnGetAsync()
        {
            ListaUsuarios = await _context.Usuarios
                .Include(u => u.Rol)
                .Include(u => u.Empleado) // <-- Cargar datos del Empleado vinculado
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostCrearAsync()
        {
            if (!ModelState.IsValid)
            {
                ListaUsuarios = await _context.Usuarios.Include(u => u.Rol).AsNoTracking().ToListAsync();
                return Page();
            }

            // Crear el nuevo usuario con el Hash encriptado
            var usuario = new Usuario
            {
                PersonalID = 1, // Asignación temporal o según selección de Personal
                Username = NuevoUsuario.UserName,
                PasswordHash = Encriptador.HashPassword(NuevoUsuario.Password),
                RolID = 1, // Rol Administrador por defecto
                Estado = true,
                FechaCreacion = DateTime.Now
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            MensajeExito = $"Usuario '{usuario.Username}' creado correctamente.";
            return RedirectToPage();
        }
    }
}