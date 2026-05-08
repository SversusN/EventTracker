using Microsoft.AspNetCore.Mvc;

namespace EventApi.Exceptions
{
    internal sealed class ValidationException : Exception
    {
        internal IDictionary<string, ICollection<string>> Errors { get; } = new Dictionary<string, ICollection<string>>();

        internal ValidationException(IDictionary<string, ICollection<string>> errors) : base("Validation failed")
        {
            Errors = errors;
        }

        internal ValidationException(string field, string error) : base("Validation failed")
        {
            Errors = new Dictionary<string, ICollection<string>>
            {
                { field, new[] { error } }
            };
        }

        internal ValidationProblemDetails ToProblemDetails()
            => new ValidationProblemDetails(Errors.ToDictionary(
                kvp => kvp.Key, 
                kvp => kvp.Value.ToArray()))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Failed",
                Detail = "One or more validation errors occurred.",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1"
            };
    }
}
