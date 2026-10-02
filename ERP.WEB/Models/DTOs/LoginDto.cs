using System.ComponentModel.DataAnnotations;

namespace ERP.WEB.Models.DTOs
{
    public class LoginDto
    {
        [Required(ErrorMessage = "El usuario es obligatorio.")]
        [Display(Name = "Usuario")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Recordar en este dispositivo")]
        public bool RememberMe { get; set; }
    }
}