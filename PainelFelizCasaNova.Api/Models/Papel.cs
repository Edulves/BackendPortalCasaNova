namespace PainelFelizCasaNova.Api.Models;

/// <summary>Espelho de Usuario.Papel (TextChoices) do Django.</summary>
public static class Papel
{
    public const string Admin = "admin";
    public const string Operador = "operador";
    public const string Visualizador = "visualizador";

    public static readonly string[] Todos = { Admin, Operador, Visualizador };

    public static bool EhValido(string? papel) =>
        papel is not null && Todos.Contains(papel, StringComparer.Ordinal);

    public static int Nivel(string? papel) => papel switch
    {
        Admin => 3,
        Operador => 2,
        Visualizador => 1,
        _ => 0,
    };
}