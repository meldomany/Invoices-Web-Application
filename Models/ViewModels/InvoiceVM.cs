using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace InvoiceApp.Models.ViewModels
{
    public class InvoiceVM
    {
        public InvoiceVM()
        {
            InvoiceCallOffs = new List<InvoiceCallOffs>();
        }

        public Invoice Invoice { get; set; }
        public List<InvoiceCallOffs> InvoiceCallOffs { get; set; }
        public IEnumerable<SelectListItem> CallOffsSelectList { get; set; }
        public List<int> CallOffsIDs { get; set; }
    }
}
