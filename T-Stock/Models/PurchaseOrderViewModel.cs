using System.ComponentModel.DataAnnotations;

namespace T_Stock.Models
{
    public class PurchaseOrderViewModel
    {
       public string PO_ID { get; set; }
        public string Status { get; set; }
        public string? Remarks { get; set; }
        public List<POItemViewModel> POProductItems { get; set; } = new List<POItemViewModel>();
    }

    public class POItemViewModel
    {
        public string ProductId { get; set; }
        public string SupplierID { get; set; }
        public string? ProductName { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
    }

}
