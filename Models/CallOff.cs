using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace InvoiceApp.Models
{
    public class CallOff
    {
        public int Id { get; set; }

        [Required]
        public long CallOffNumber { get; set; }

        [Required]
        public string Description { get; set; }

        public IEnumerable<CallOffItems> CallOffItems { get; set; }
    }
}
