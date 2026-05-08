using Microsoft.AspNetCore.Mvc;

namespace EventApi.Exceptions
{
    internal sealed class NotFoundException : Exception
    {
        internal NotFoundException(string message) : base(message) { }

        internal ProblemDetails ToProblemDetails()
            => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Resource Not Found",
                Detail = Message,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5"
            };
    }
}
