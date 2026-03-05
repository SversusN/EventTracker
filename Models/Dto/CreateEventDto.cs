using System.ComponentModel.DataAnnotations;

namespace EventTracker.Models.Dto;

public record CreateEventDto(
    [Required(ErrorMessage = "Title is required.")]
    string Title,

    string? Description,

    [Required]
    DateTime StartAt,

    [Required]
    DateTime EndAt
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
