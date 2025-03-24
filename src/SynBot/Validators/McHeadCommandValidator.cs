using FluentValidation;
using SynBot.Commands;

namespace SynBot.Validators;

public class McHeadCommandValidator : AbstractValidator<McHeadCommand>
{
    public McHeadCommandValidator()
    {
        RuleFor(cmd => cmd.Username)
            .Matches("[a-zA-Z_]+");
    }
}