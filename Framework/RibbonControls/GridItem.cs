using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Printing;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Core.Data;

namespace TransferCenterAttributes
{
    internal class GridItem
    {
        public string FieldName { get; set; }
        public bool IsActive { get; set; }
        public string DType { get; set; }

        //public FieldValue grid_field { get; set; }

        public GridItem(string name, string datatype)
        {
            FieldName = name;
            DType = datatype;
            IsActive = false;
        }
    }
}
