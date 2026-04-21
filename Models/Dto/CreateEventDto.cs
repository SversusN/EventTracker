using System.ComponentModel.DataAnnotations;

namespace EventTrackerApi.Models.Dto;

public record CreateEventDto(
    [Required(ErrorMessage = "Title is required.")]
    string Title,

    string? Description,

    [Required]
    DateTime StartAt,

    [Required]
    DateTime EndAt,

    [Required(ErrorMessage = "TotalSeats is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "TotalSeats must be greater than 0.")]
    int TotalSeats
) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (EndAt <= StartAt)
        {
            yield return new ValidationResult("EndAt must be later than StartAt.");
        }
    }
}
