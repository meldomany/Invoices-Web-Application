using System.ComponentModel.DataAnnotations.Schema;

namespace InvoiceApp.Models
{
    public class InvoiceCallOffItems
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; }
        public int CallOffItemId { get; set; }

        [ForeignKey(nameof(InvoiceId))]
        public Invoice Invoice { get; set; }

        [ForeignKey(nameof(CallOffItemId))]
        public CallOffItems CallOffItems { get; set; }
    }
}
