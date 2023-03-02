using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace InvoiceApp.Models.ViewModels
{
    public class InvoiceCallOffItemsVM
    {
        public Invoice Invoice { get; set; }
        public IEnumerable<SelectListItem> CallOffItems { get; set; }
        public List<int> CallOffItemsIDS { get; set; }
    }
}