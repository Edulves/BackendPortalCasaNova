namespace PainelFelizCasaNova.Api.Models;

/// <summary>Espelho do model Django <c>Usuario</c> (AbstractUser customizado).</summary>
public class Usuario
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string? Email { get; set; }
    public string PasswordHash { get; set; } = "";
    public int? OrganizacaoId { get; set; }
    public Organizacao? Organizacao { get; set; }
    public string Papel { get; set; } = Models.Papel.Operador;
    public bool IsActive { get; set; } = true;
    public bool IsStaff { get; set; }
    public bool IsSuperUser { get; set; }
    public DateTime? LastLogin { get; set; }
    public DateTime? AtivoAte { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public bool PodeEscrever => Papel is Models.Papel.Admin or Models.Papel.Operador;
    public bool PodeAdministrar => Papel == Models.Papel.Admin;
}