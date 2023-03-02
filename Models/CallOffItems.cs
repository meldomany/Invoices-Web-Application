using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvoiceApp.Models
{
    public class CallOffItems
    {
        public int Id { get; set; }
        public int CallOffId { get; set; }
        public int ItemId { get; set; }
        public int Quantity { get; set; }
        public int Remains { get; set; }
        public int Allowed { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey(nameof(CallOffId))]
        public CallOff CallOff { get; set; }

        [ForeignKey(nameof(ItemId))]
        public Item Item { get; set; }

        public IEnumerable<InvoiceCallOffItems> InvoiceCallOffItems { get; set; }
    }
}
