using System;
using System.ComponentModel.DataAnnotations;

namespace InvoiceApp.Models
{
    public class Invoice
    {
        public int Id { get; set; }
        [Required]
        public int InvoiceNumber { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}