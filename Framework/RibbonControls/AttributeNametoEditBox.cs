using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RibbonControls
{
    class AttributeNameToUseEditBox : ArcGIS.Desktop.Framework.Contracts.EditBox
        {
            public AttributeNameToUseEditBox()
            {
                Module2.Current.AttributeNameToEditBox1 = this;
                Text = "";
            }
        }
    }
