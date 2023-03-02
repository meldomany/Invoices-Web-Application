using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace InvoiceApp.Models.ViewModels
{
    public class ItemVM
    {
        public Item Item { get; set; }
        public IEnumerable<SelectListItem> ContractorSelectList { get; set; }
        public IEnumerable<SelectListItem> UnitSelectList { get; set; }
    }
}
