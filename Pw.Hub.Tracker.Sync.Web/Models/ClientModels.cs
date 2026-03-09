namespace Pw.Hub.Tracker.Sync.Web.Models;

/// <summary>
/// Настройки для Client Credentials Flow
/// </summary>
public class ClientCredentialsOptions
{
    /// <summary>
    /// Адрес сервера авторизации (Identity Provider)
    /// </summary>
    public string Authority { get; set; } = string.Empty;
    
    /// <summary>
    /// Идентификатор клиента
    /// </summary>
    public string ClientId { get; set; } = string.Empty;
    
    /// <summary>
    /// Секретный ключ клиента
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;
    
    /// <summary>
    /// Запрашиваемые области доступа (scopes), разделённые пробелами
    /// </summary>
    public string Scope { get; set; } = string.Empty;
}