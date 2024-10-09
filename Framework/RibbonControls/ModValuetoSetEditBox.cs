using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RibbonControls
{
    class ModValueToSetEditBox : ArcGIS.Desktop.Framework.Contracts.EditBox
        {
            public ModValueToSetEditBox()
            {
                Module2.Current.ModValuetoSetEditBox1 = this;
                Text = "";
            }
        }
    }
