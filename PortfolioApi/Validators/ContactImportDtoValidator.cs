using FluentValidation;
using PortfolioApi.Models.Dtos;

namespace PortfolioApi.Validators
{
    public class ContactImportDtoValidator : AbstractValidator<ContactImportDto>
    {
        public ContactImportDtoValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Message).NotEmpty();
        }
    }
}
