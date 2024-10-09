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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RibbonControls
{
    internal class Module2 : Module
    {
        private static Module2 _this = null;

        /// <summary>
        /// Retrieve the singleton instance to this module here
        /// </summary>
        public static Module2 Current => _this ??= (Module2)FrameworkApplication.FindModule("EditInspector_Module");

        #region Overrides
        /// <summary>
        /// Called by Framework when ArcGIS Pro is closing
        /// </summary>
        /// <returns>False to prevent Pro from closing, otherwise True</returns>
        protected override bool CanUnload()
        {
            //TODO - add your business logic
            //return false to ~cancel~ Application close
            return true;
        }

        #endregion Overrides
        #region Business Logic

        // Create instances of the edit boxes for use in the UpdateValues method
        public AttributeNameToUseEditBox AttributeNameToEditBox1 { get; set; }
        public ModValueToSetEditBox ModValuetoSetEditBox1 { get; set; }

        public void UpdateValues()
        {
            //  This sample is intended for use with a featureclass with a default text field named "Description".
            //  You can replace "Description" with any field name that works for your dataset

            // Check for an active mapview
            if (MapView.Active == null)
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("No MapView currently active. Exiting...", "Info");
                return;
            }

            QueuedTask.Run(() =>
            {

                // Get the layer selected in the Contents pane, and prompt if there is none:
                if (MapView.Active.GetSelectedLayers().Count == 0)
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("No feature layer selected in Contents pane. Exiting...", "Info");
                    return;
                }
                // Check to see if there is a selected feature layer
                var featLayer = MapView.Active.GetSelectedLayers().First() as FeatureLayer;
                if (featLayer == null)
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("A feature layer must be selected. Exiting...", "Info");
                    return;
                }
                // Get the selected records, and check/exit if there are none:
                var featSelectionOIDs = featLayer.GetSelection().GetObjectIDs();
                if (featSelectionOIDs.Count == 0)
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("No records selected for layer, " + featLayer.Name + ". Exiting...", "Info");
                    return;
                }
                // Ensure there are values in the two edit boxes
                else if (ModValuetoSetEditBox1.Text == "" || AttributeNameToEditBox1.Text == "")
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Editbox for attribute name or value to set is empty. Type in value. Exiting...", "Value Needed");
                    return;
                }

                // Get the name of the attribute to update, and the value to set:
                string attributename = AttributeNameToEditBox1.Text;
                attributename = attributename.ToUpper();
                string setvalue = ModValuetoSetEditBox1.Text;

                // Display all the parameters for the update:
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Here are your update parameters:  " +
                    "\r\n Layer: " + featLayer.Name +
                    "\r\n Attribute name: " + attributename +
                    "\r\n Number of records: " + Convert.ToString(featSelectionOIDs.Count) +
                    "\r\n Value to update: " + Convert.ToString(setvalue), "Update");

                try
                {
                    // Now ready to do the actual editing:
                    // 1. Create a new edit operation and a new inspector for working with the attributes
                    // 2. Check to see if a valid field name was chosen for the feature layer
                    // 3. If so, apply the edit

                    //
                    var inspector = new ArcGIS.Desktop.Editing.Attributes.Inspector();
                    inspector.Load(featLayer, featSelectionOIDs);
                    if (inspector.HasAttributes && inspector.Count(a => a.FieldName.ToUpper() == attributename.ToUpper()) > 0)
                    {
                        inspector[attributename] = setvalue;
                        var editOp = new EditOperation();
                        editOp.Name = "Edit " + featLayer.Name + ", " + Convert.ToString(featSelectionOIDs.Count) + " records.";
                        editOp.Modify(inspector);
                        editOp.ExecuteAsync();

                        ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Update operation completed.", "Editing with Inspector");
                    }
                    else
                    {
                        ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("The Attribute provided is not valid. " +
                            "\r\n Ensure your attribute name is correct.", "Invalid attribute");
                        // return;
                    }
                }
                catch (Exception exc)
                {
                    // Catch any exception found and display a message box.
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Exception caught while trying to perform update: " + exc.Message);
                    return;
                }
            });
        }
        #endregion
    }
}
