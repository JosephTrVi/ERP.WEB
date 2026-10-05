using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ERP.WEB.Data;
using ERP.WEB.Models.DTOs;
using ERP.WEB.Services;

namespace ERP.WEB.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly SunatTcService _sunatTcService;

        // Inyección del DbContext y del servicio SunatTcService
        public LoginModel(ApplicationDbContext context, SunatTcService sunatTcService)
        {
            _context = context;
            _sunatTcService = sunatTcService;
        }

        [BindProperty]
        public LoginDto Input { get; set; } = new LoginDto();

        public string? ReturnUrl { get; set; }

        public void OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (!ModelState.IsValid) return Page();

            string user = Input.UserName?.Trim() ?? "";
            string passCalculada = Encriptador.HashPassword(Input.Password?.Trim() ?? "");

            // TEST 1: Verificar si el usuario existe solo por Username
            var us = await _context.Usuarios.FirstOrDefaultAsync(u => u.Username == user);
            if (us == null)
            {
                ModelState.AddModelError("", $"[DIAGNÓSTICO 1]: No se encontró ningún usuario con Username '{user}' en la tabla Seguridad.Usuarios.");
                return Page();
            }

            // TEST 2: Verificar si la contraseña coincide
            if (us.PasswordHash.Trim().ToLower() != passCalculada.ToLower())
            {
                ModelState.AddModelError("", $"[DIAGNÓSTICO 2]: El usuario existe pero la clave no coincide.\nCalculado C#: '{passCalculada}'\nGuardado en BD: '{us.PasswordHash}'");
                return Page();
            }

            // TEST 3: Verificar estado
            if (!us.Estado)
            {
                ModelState.AddModelError("", "[DIAGNÓSTICO 3]: El usuario existe y la clave es correcta, pero el campo 'Estado' es FALSE (0).");
                return Page();
            }

            // TEST 4: Probar la relación con Rol y Empleado
            var usCompleto = await _context.Usuarios
                .Include(u => u.Rol)
                .Include(u => u.Empleado)
                .FirstOrDefaultAsync(u => u.UsuarioID == us.UsuarioID);

            if (usCompleto?.Rol == null)
            {
                ModelState.AddModelError("", $"[DIAGNÓSTICO 4A]: No se pudo cargar el Rol. El RolID ({us.RolID}) no existe en la tabla Seguridad.Roles.");
                return Page();
            }

            if (usCompleto?.Empleado == null)
            {
                ModelState.AddModelError("", $"[DIAGNÓSTICO 4B]: No se pudo cargar el Empleado. El PersonalID ({us.PersonalID}) no existe en la tabla Personal.Personal.");
                return Page();
            }

            // SI PASA TODAS LAS PRUEBAS: Iniciar Sesión
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usCompleto.UsuarioID.ToString()),
                new Claim(ClaimTypes.Name, usCompleto.Username),
                new Claim(ClaimTypes.Role, usCompleto.Rol.NombreRol),
                new Claim("PersonalID", usCompleto.PersonalID.ToString()),
                new Claim("NombreEmpleado", usCompleto.Empleado.NombreCompleto)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity));


            return LocalRedirect(returnUrl);
        }
    }
}