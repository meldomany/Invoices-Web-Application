using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvoiceApp.Models
{
    public class InvoiceCallOffs
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; }
        public int CallOffId { get; set; }
        public DateTime CreatedAt { get; set; }

        [ForeignKey(nameof(InvoiceId))]
        public Invoice Invoice { get; set; }
        [ForeignKey(nameof(CallOffId))]
        public CallOff CallOff { get; set; }
    }
}
