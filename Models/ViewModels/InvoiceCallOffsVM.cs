using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace InvoiceApp.Models.ViewModels
{
    public class InvoiceCallOffsVM
    {
        public Invoice Invoice { get; set; }
        public IEnumerable<SelectListItem> CallOffsSelectList { get; set; }
        public List<int> CallOffsIDS { get; set; }
    }
}
