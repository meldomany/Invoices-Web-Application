using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel;

namespace InvoiceApp.Models.ViewModels
{
    public class CallOffVM
    {
        public CallOffVM()
        {
            CallOffItems = new List<CallOffItems>();
        }

        public CallOff CallOff { get; set; }
        public List<CallOffItems> CallOffItems { get; set; }
        public IEnumerable<SelectListItem> ItemsSelectList { get; set; }
    }


    //public class CallOffItemsVM
    //{
    //    [DisplayName("Item Quantity")]
    //    public int Quantity { get; set; }
    //    [DisplayName("Item Number")]
    //    public int ItemId { get; set; }
    //}
}
