using FluentValidation;

namespace EventService.Application.Events.CreateEvent;

public class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Date).GreaterThan(DateTime.UtcNow).WithMessage("La fecha debe ser futura.");
        RuleFor(x => x.Venue).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Zones).NotEmpty().WithMessage("Debe incluir al menos una zona.");
        RuleForEach(x => x.Zones).ChildRules(zone =>
        {
            zone.RuleFor(z => z.Name).NotEmpty();
            zone.RuleFor(z => z.Price).GreaterThanOrEqualTo(0);
            zone.RuleFor(z => z.Capacity).GreaterThan(0);
        });
    }
}
