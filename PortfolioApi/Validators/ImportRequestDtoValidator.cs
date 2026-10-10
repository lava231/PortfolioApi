using FluentValidation;
using PortfolioApi.Models.Dtos;

namespace PortfolioApi.Validators
{
    public class ImportRequestDtoValidator : AbstractValidator<ImportRequestDto>
    {
        public ImportRequestDtoValidator()
        {
            When(x => x.Contacts != null, () => {
                RuleForEach(x => x.Contacts).SetValidator(new ContactImportDtoValidator());
            });
        }
    }
}
