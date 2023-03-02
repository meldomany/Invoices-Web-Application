using System.ComponentModel.DataAnnotations;

namespace InvoiceApp.Models
{
    public class Contractor
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public int Number { get; set; }
    }
}
