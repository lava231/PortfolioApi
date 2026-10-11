using FluentValidation;
using PortfolioApi.Models.Dtos;

namespace PortfolioApi.Validators
{
    public class ContactImportDtoValidator : AbstractValidator<ContactImportDto>
    {
        public ContactImportDtoValidator()
        {
            RuleFor(x => x.Name).NotEmpty().Length(2,50);
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Message).NotEmpty().Length(10,1000);
            RuleFor(x => x.Status).Must(s => s == null || s == Models.Entities.ContactStatus.Unread || s == Models.Entities.ContactStatus.Read || s == Models.Entities.ContactStatus.Replied);
        }
    }
}
