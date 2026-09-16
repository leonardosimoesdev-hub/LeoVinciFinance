namespace Auth.Domain.Enums;

/// <summary>
/// Perfis de acesso do sistema (Especificação Mestre, seções 11 e 16).
/// Os valores numéricos são estáveis pois são persistidos e usados como claim de Role no JWT.
/// </summary>
public enum Perfil
{
    Admin = 1,
    Comerciante = 2
}
