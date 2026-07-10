namespace EventTracker.EventsService.Application.Ports;

/// <summary>
/// Абстракция распределённого кеша.
/// Реализация должна быть устойчива к сбоям: при недоступности хранилища
/// методы не должны пробрасывать исключения, а возвращать null / выполнять no-op.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Получает десериализованное значение по ключу.
    /// </summary>
    /// <typeparam name="T">Тип значения.</typeparam>
    /// <param name="key">Ключ кеша.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Значение из кеша или <c>null</c>, если ключ отсутствует или хранилище недоступно.</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Сохраняет значение в кеш с указанным временем жизни.
    /// </summary>
    /// <typeparam name="T">Тип значения.</typeparam>
    /// <param name="key">Ключ кеша.</param>
    /// <param name="value">Значение.</param>
    /// <param name="expiration">Время жизни. Если <c>null</c>, используется значение по умолчанию.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Удаляет значение из кеша по ключу.
    /// </summary>
    /// <param name="key">Ключ кеша.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}
