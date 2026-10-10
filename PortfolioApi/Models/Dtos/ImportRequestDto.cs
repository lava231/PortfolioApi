using System.Collections.Generic;
using System.Text.Json;

namespace PortfolioApi.Models.Dtos
{
    public class ImportRequestDto
    {
        // Portfolio payload will be accepted as raw JSON and handled later when Portfolio entity exists
        public JsonElement? Portfolio { get; set; }

        public IEnumerable<ContactImportDto>? Contacts { get; set; }

        // If true, replace existing contacts with provided array. Default false (append).
        public bool ReplaceContacts { get; set; } = false;
    }
}
