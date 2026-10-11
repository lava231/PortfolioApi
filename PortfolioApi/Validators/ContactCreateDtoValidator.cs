using FluentValidation;
using PortfolioApi.Models.Dtos;

namespace PortfolioApi.Validators
{
    public class ContactCreateDtoValidator : AbstractValidator<ContactCreateDto>
    {
        public ContactCreateDtoValidator()
        {
            // Consolidated rules for create DTO
            RuleFor(x => x.Name).NotEmpty().Length(2, 50);
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Message).NotEmpty().Length(10, 1000);
        }
    }
}
