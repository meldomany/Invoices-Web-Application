using System.ComponentModel.DataAnnotations;

namespace InvoiceApp.Models
{
    public class Unit
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
    }
}
