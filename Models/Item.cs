using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvoiceApp.Models
{
    public class Item
    {
        public int Id { get; set; }

        [Required]
        [DisplayName("Item Number")]
        public int ItemNo { get; set; }

        [Required]
        [DisplayName("Description")]
        public string Description { get; set; }

        [Required]
        public int Quantity { get; set; }
        [Required]
        public double Price { get; set; }
        [Required]
        public double TotalPrice { get; set; }
        public bool Status { get; set; }

        [DisplayName("Unit")]
        public int UnitId { get; set; }
        [ForeignKey(nameof(UnitId))]
        public Unit Unit { get; set; }

        [DisplayName("Contractor")]
        public int ContractorId { get; set; }
        [ForeignKey(nameof(ContractorId))]
        public Contractor Contractor { get; set; }

        public IEnumerable<CallOffItems> CallOffItems { get; set; }
    }
}
