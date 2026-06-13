namespace Pedidos.Application.Auth.Interfaces;

/// <summary>
/// Porta para hash/verificação de senhas. A implementação (PBKDF2) vive na
/// Infrastructure — o AuthService não conhece o algoritmo.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Gera o hash de uma senha em texto puro.</summary>
    string Hash(string password);

    /// <summary>Verifica se a senha corresponde ao hash armazenado.</summary>
    bool Verify(string password, string hash);
}
