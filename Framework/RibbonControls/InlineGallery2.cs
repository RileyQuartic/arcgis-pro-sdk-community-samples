using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Extensions;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Layouts;
using ArcGIS.Desktop.Mapping;
using System.Windows.Controls;

namespace RibbonControls
{
  internal class InlineGallery2 : Gallery
  {
    private bool _isInitialized;

    public InlineGallery2()
    {
      Initialize();
    }

    private void Initialize()
    {
        if (_isInitialized)
            return;
        _isInitialized = true;

          //Add 6 items to the gallery
             }

        protected override void OnClick(GalleryItem item)
    {
        //TODO - insert your code to manipulate the clicked gallery item here
        System.Diagnostics.Debug.WriteLine("Remove this line after adding your custom behavior.");
        base.OnClick(item);
    }
  }
}
