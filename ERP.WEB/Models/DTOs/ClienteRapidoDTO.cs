public class ClienteRapidoDTO
{
    public string TipoDocumento { get; set; } = "RUC"; // RUC o DNI
    public string NumeroDocumento { get; set; } = null!;
    public string RazonSocial { get; set; } = null!;
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
}