// Copyright 2019 Esri 
//
// 
//   Licensed under the Apache License, Version 2.0 (the "License"); 
//   you may not use this file except in compliance with the License. 
//   You may obtain a copy of the License at 
//
//       https://www.apache.org/licenses/LICENSE-2.0 
//
//   Unless required by applicable law or agreed to in writing, software 
//   distributed under the License is distributed on an "AS IS" BASIS, 
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
//   See the License for the specific language governing permissions and 
//   limitations under the License. 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;

namespace RibbonControls
{
  internal class Button1 : Button
  {
    protected override void OnClick()
    {
      ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Button1 clicked.");
    }
  }

  internal class Button2 : Button
  {
    protected override void OnClick()
    {
      ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Button2 clicked.");
    }
  }

  internal class Button3 : Button
  {
    protected override void OnClick()
    {
      ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Button3 clicked.");
    }
  }
  internal class Button4 : Button
  {
    protected override void OnClick()
    {
      ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Button4 clicked.");
    }
  }
  internal class Button5 : Button
  {
    protected override void OnClick()
    {
       ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Button5 clicked.");
    }
  }
  internal class Button6 : Button
  {
    protected override void OnClick()
    {
       ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Button6 clicked.");
    }
  }
  internal class Button7 : Button
  {
    protected override void OnClick()
    {
       ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Button7 clicked.");
    }
  }
}
