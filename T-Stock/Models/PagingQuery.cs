using System;
using System.Collections.Generic;

namespace T_Stock.Models
{
    public class PagingQuery
    {
        public string Search { get; set; } = "";
        public string Sort { get; set; } = "";
        public bool Desc { get; set; } = false;

        public string Product { get; set; } = "none";

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public string DateType { get; set; } = "";
        public DateTime? DateFrom { get; set; } = null;
        public DateTime? DateTo { get; set; } = null;
    }

}
