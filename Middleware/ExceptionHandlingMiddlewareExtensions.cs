namespace EventTrackerApi.Middleware;

/// <summary>
/// Расширения для регистрации mw обработки исключений
/// </summary>
public static class ExceptionHandlingMiddlewareExtensions
{
    /// <summary>
    /// Добавляет глобальную обработку исключений в пайплайн
    /// </summary>
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
