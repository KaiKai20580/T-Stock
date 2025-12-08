using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace T_Stock.Models
{
    public class SupplierViewModel
    {
        public string? SupplierId { get; set; }

        [Required(ErrorMessage = "Company name is required")]
        [RegularExpression(@"^[a-zA-Z0-9 \-',&().]+$", ErrorMessage = "Company name can only contain letters, numbers, spaces, hyphens (-), commas (,), apostrophes ('), ampersands (&), and parentheses ().")]
        public string Company { get; set; }

        [Required(ErrorMessage = "Contact person is required")]
        [RegularExpression(@"^[a-zA-Z ]+$", ErrorMessage = "Name can only contain letters and spaces.")]
        public string ContactPerson { get; set; }

        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(@"^0\d{9,10}$", ErrorMessage = "Phone number must only consist of 10-11 numbers")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Company address is required")]
        [RegularExpression(@"^[a-zA-Z0-9\-', ]+$", ErrorMessage = "Address can only contain letters, numbers, hyphen(-), apostrophes('), commas(,) and spaces.")]
        public string Address { get; set; }

        public List<SupplierProductItem>? ProductItems { get; set; }
    }

    //Used for validation only
    public class SupplierProductItem
    {
        public string ProductID { get; set; }

        public double SupplierPrice { get; set; }
    }
}
