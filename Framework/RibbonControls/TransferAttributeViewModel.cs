using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Editing.Attributes;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

namespace TransferCenterAttributes
{
    internal class TransferAttributeViewModel : DockPane
    {
        private const string _dockPaneID = "TransferCenterAttributes_TransferAttribute";

        private IEnumerable<Layer> layers;
        private IEnumerable<Layer> _selectedLineLayers;
        private MapView mapView;
        private Object _layer_lock = new Object();
        private bool _initialized = false;
        private FeatureLayer _activeLayer;

        protected TransferAttributeViewModel() 
        {
            BindingOperations.EnableCollectionSynchronization(_layers, _layer_lock);
        }

        // Logic for updating polygon list when hiding and showing the dockpane.
        protected override void OnShow(bool isVisible)
        {
            if(MapView.Active == null)
            {
                this.Hide();
            }
            else
            {
                if (isVisible)
                {
                    UpdatePolyList();
                }
                base.OnShow(isVisible);
            }
            return;
        }

        /// <summary>
        /// Show the DockPane.
        /// </summary>
        internal static void Show()
        {
            DockPane pane = FrameworkApplication.DockPaneManager.Find(_dockPaneID);
            if (pane == null)
                return;

            pane.Activate();
        }

        #region Data Bindings
        /// <summary>
        /// Text shown near the top of the DockPane.
        /// </summary>
        private string _heading = "My DockPane";
        public string Heading
        {
            get => _heading;
            set => SetProperty(ref _heading, value);
        }

        private string _selectedLayer;

        public string SelectedLayer
        {
            get => _selectedLayer;
            set => SetProperty(ref _selectedLayer, value);
        }

        private ObservableCollection<string> _layers = new ObservableCollection<string>();

        public ObservableCollection<string> Layers
        {
            get => _layers;
            set => SetProperty(ref _layers, value);
        }

        private ObservableCollection<GridItem> _fieldGrid = new ObservableCollection<GridItem>();

        public ObservableCollection<GridItem> FieldGrid
        {
            get => _fieldGrid;
            set => SetProperty(ref _fieldGrid, value);
        }

        private string _selectedField;

        public string SelectedField
        {
            get => _selectedField;
            set => SetProperty(ref _selectedField, value);
        }

        //Data binding for Radio Buttons

        private bool _leftButton;
        private bool _rightButton;

        public bool LeftButton
        {
            get => _leftButton;
            set => SetProperty(ref _leftButton, value);
        }
        public bool RightButton
        {
            get => _rightButton;
            set => SetProperty(ref _rightButton, value);
        }

        //Data binding for offset input
        private string _offset;

        public string Offset
        {
            get => _offset;
            set => SetProperty(ref _offset, value);
        }

        #endregion Data Bindings

        //Updates the polygon details in the dockpane
        private void UpdatePolyList()
        {
            mapView = MapView.Active;
            layers = mapView.Map.Layers.Where(layer => layer is FeatureLayer);
            var selectedLineLayer = mapView.GetSelectedLayers();


            Layers.Clear();

            foreach(FeatureLayer layer in layers)
            {
                if(layer.ShapeType == esriGeometryType.esriGeometryPolygon)
                {
                    lock (_layer_lock) { Layers.Add(layer.Name); }
                }
            }

            SelectedLayer = Layers.FirstOrDefault();

            foreach(FeatureLayer layer in layers)
            {
                if(layer.Name==SelectedLayer)
                {
                    _activeLayer = layer;
                    UpdateFieldList();
                    return;
                }
            }
        }

        //Populates the datagrid with all fields.
        private async void UpdateFieldList()
        {
            IReadOnlyList<ArcGIS.Core.Data.Field> fields = null;
            await QueuedTask.Run(() =>
            {
                fields = _activeLayer.GetTable().GetDefinition().GetFields();
            });
            foreach (var field in fields)
            {
                FieldGrid.Add(new GridItem(field.Name, field.FieldType.ToString()));
            }
        }

        //Activates on "Transfer" button press
        public ICommand Transfer
        {
            get
            {
                return new RelayCommand(async () =>
                {
                    int offset;
                    if (Int32.TryParse(Offset, out int j))
                    {
                        offset = j;
                    }
                    else
                    {
                        MessageBox.Show($"Please provide a valid whole number for the offset.");
                        return;
                    }
                    IEnumerable<Layer> TOC_layers = mapView.GetSelectedLayers().Where(layer => layer is FeatureLayer);
                    Dictionary<(long, string), MapPoint> point_mapping = await GetCenterpointMapping(TOC_layers, offset);
                    if (point_mapping != null)
                    {
                        await AddTransferFields(TOC_layers);
                        Dictionary<(long, string), long> shape_mapping = await GetLinePolygonMapping(point_mapping, TOC_layers);
                        PopulateFields(shape_mapping, TOC_layers);
                        await Project.Current.SaveEditsAsync();
                    }
                }, true);
            }
            set
            {

            }
        }

        // Adds the transfered values to the newly created fields in the line feature layers
        private async void PopulateFields(Dictionary<(long, string), long> mapping, IEnumerable<Layer> TOC_layers)
        {
            await QueuedTask.Run(() =>
            {
                foreach (FeatureLayer layer in TOC_layers)
                {
                    if (layer.ShapeType != esriGeometryType.esriGeometryPolyline) { continue; }

                    Selection select = layer.GetSelection();
                    SelectionSet lineSelectionSet = SelectionSet.FromSelection(layer, select);

                    Table table = layer.GetTable();
                    QueryFilter queryFilter = new QueryFilter();

                    RowCursor rowCursor = null;

                    if(lineSelectionSet.IsEmpty)
                    {
                        rowCursor = table.Search(queryFilter, false);
                    }
                    else
                    {
                        rowCursor = select.Search();
                    }

                    while (rowCursor.MoveNext())
                    {
                        using (Row row = rowCursor.Current)
                        {
                            long oid = row.GetObjectID();
                            long polygon_oid = mapping[(oid, table.GetName())];
                            if (polygon_oid == -1)
                            {
                                foreach (GridItem entry in FieldGrid)
                                {
                                    if (!entry.IsActive) { continue; }
                                    var modifyFeature = new EditOperation();
                                    var modifyInspector = new Inspector();
                                    modifyInspector.Load(layer, oid);
                                    modifyInspector[entry.FieldName] = null;
                                    modifyFeature.Modify(modifyInspector);
                                    if (!modifyFeature.IsEmpty)
                                    {
                                        var result = modifyFeature.Execute();
                                    }
                                }
                                continue;
                            }
                            QueryFilter qf = new QueryFilter();
                            // The polygon feature layer selected in the Dockpane
                            Table polyTable = _activeLayer.GetTable();

                            using (RowCursor rc = polyTable.Search(qf, false))
                            {
                                while (rc.MoveNext())
                                {
                                    using (Row r = rc.Current)
                                    {
                                        if (polygon_oid == r.GetObjectID())
                                        {
                                            foreach (GridItem entry in FieldGrid)
                                            {
                                                if (!entry.IsActive) { continue; }
                                                var modifyFeature = new EditOperation();
                                                var modifyInspector = new Inspector();
                                                modifyInspector.Load(layer, oid);
                                                modifyInspector[entry.FieldName] = r[entry.FieldName];
                                                modifyFeature.Modify(modifyInspector);
                                                if (!modifyFeature.IsEmpty)
                                                {
                                                    var result = modifyFeature.Execute(); //Execute and ExecuteAsync will return true if the operation was successful and false if not
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    rowCursor.Dispose();
                }
            });
            return;
        }

        // Adds the new fields for transfer to line feature layers
        private async Task AddTransferFields(IEnumerable<Layer> TOC_layers)
        {
            foreach (GridItem entry in FieldGrid)
            {
                if (!entry.IsActive){ continue; }

                foreach (Layer layer in TOC_layers)
                {
                    string layerPath = await QueuedTask.Run(async () => { return layer.GetPath().ToString(); });
                    layerPath = layerPath.Remove(0, 8);
                    string dtype = GetFieldType(entry.DType);
                    var valArray = Geoprocessing.MakeValueArray(layerPath, entry.FieldName, dtype);
                    var georesult = await Geoprocessing.ExecuteToolAsync("management.AddField", valArray, null, null, null, GPExecuteToolFlags.AddToHistory);
                }
            }
        }

        // Obtains the field type for a geoprocessing tool.
        public string GetFieldType(string fieldtype)
        {
            string clause;
            switch (fieldtype)
            {
                case "Integer":
                    clause = "LONG";
                    break;
                case "SmallInteger":
                    clause = "SHORT";
                    break;
                case "String":
                    clause = "TEXT";
                    break;
                default:
                    clause = fieldtype;
                    break;
            }
            return clause;
        }

        // returns a dictionary mapping lines to their centerpoints
        private async Task<Dictionary<(long, string), MapPoint>> GetCenterpointMapping(IEnumerable<Layer> TOC_layers, double offset)
        {
            Dictionary<(long, string), MapPoint> mapping = new Dictionary<(long, string), MapPoint>();
            foreach (FeatureLayer layer in TOC_layers)
            {
                if (layer.ShapeType != esriGeometryType.esriGeometryPolyline) { continue; }
               
                Table table = await QueuedTask.Run(async () => { return layer.GetTable(); }); 

                QueryFilter queryFilter = new QueryFilter();
                int mssg = await QueuedTask.Run(async () =>
                {
                    using (RowCursor rowCursor = table.Search(queryFilter, false))
                    {
                        while (rowCursor.MoveNext())
                        {
                            using (Row row = rowCursor.Current)
                            {
                                long oid = row.GetObjectID();
                                Feature feature = row as Feature;
                                Polyline polyline = feature.GetShape() as Polyline;
                                MapPoint mid_pt = GeometryEngine.Instance.MovePointAlongLine(polyline, 0.5, true, 0, SegmentExtensionType.NoExtension);
                                MapPoint offset_pt = GetPointOnNormal(polyline, mid_pt, offset);
                                if (offset_pt == null)
                                {
                                    mapping[(oid, table.GetName())] = null;
                                    return -1;
                                }
                                mapping[(oid, table.GetName())] = offset_pt;
                            }
                        }
                    }
                    return 1;
                });
                if (mssg == -1)
                {
                    return null;
                }
            }
            return mapping;
        }

        // Creates a point on an offset and 90 degrees to the centerpoint of a line
        private MapPoint GetPointOnNormal(Polyline polyline, MapPoint midpoint, double offset)
        {
            double midpoint_distance = polyline.Length/2;
                      
            ICollection<Segment> collection = new List<Segment>();
            polyline.GetAllSegments(ref collection);
            double length_tracker = 0;
            double position_along_segment = -1;
            LineSegment center_segment = null;
            foreach(Segment segment in collection)
            {
                length_tracker += segment.Length;
                if(length_tracker > midpoint_distance)
                {
                    center_segment = (LineSegment)segment;
                    length_tracker -= segment.Length;
                    position_along_segment = midpoint_distance - length_tracker;
                    break;
                }
            }

            //Deciding the angle for the point offset.
            double abs_val_angle = 0.0;
            if (RightButton)
            {
                if (center_segment.Angle < 0) { abs_val_angle = Math.PI + center_segment.Angle; }
                else { abs_val_angle = center_segment.Angle; }
            }
            else if (LeftButton)
            {
                if (center_segment.Angle > 0) { abs_val_angle =  center_segment.Angle - Math.PI; }
                else { abs_val_angle = center_segment.Angle; }
            }
            else
            {
                if(offset != 0) 
                { 
                    MessageBox.Show($"Please select an orientation for the offset.");
                    return null;
                }
                else { abs_val_angle = center_segment.Angle; }
            }
            
            double angle_off_center = (abs_val_angle) - (Math.PI / 2);
            MapPoint pt = GeometryEngine.Instance.ConstructPointFromAngleDistance(midpoint, angle_off_center, offset);
            return pt;
        }

        // Creates a mapping between a polyline row and a polygon
        private async Task<Dictionary<(long, string), long>> GetLinePolygonMapping(Dictionary<(long, string), MapPoint> centerpoint, IEnumerable<Layer> TOC_layers)
        {
            Dictionary<(long, string), long> mapping = new Dictionary<(long, string), long>();
            await QueuedTask.Run(() =>
            {
                foreach (FeatureLayer layer in TOC_layers)
                {
                    if (layer.ShapeType != esriGeometryType.esriGeometryPolyline) { continue; }
                    Table table = layer.GetTable();
                    QueryFilter queryFilter = new QueryFilter();
                    // Searching through polyline
                    using (RowCursor rowCursor = table.Search(queryFilter, false))
                    {
                        while (rowCursor.MoveNext())
                        {
                            using (Row row = rowCursor.Current)
                            {
                                long oid = row.GetObjectID();
                                Feature feature = row as Feature;
                                Polyline polyline = feature.GetShape() as Polyline;
                                var temp = centerpoint[(oid, table.GetName())];
                                var sf = new SpatialQueryFilter()
                                {
                                    FilterGeometry = centerpoint[(oid, table.GetName())],
                                    SpatialRelationship = SpatialRelationship.Within,
                                    SubFields = "*"
                                };
                                //should only return at most one polygon row
                                using (RowCursor polyrows = _activeLayer.Search(sf))
                                {
                                    while (polyrows.MoveNext())
                                    {
                                        using (Row polyrow = polyrows.Current)
                                        {
                                            mapping[(oid, table.GetName())] = polyrow.GetObjectID();
                                        }
                                    }
                                }
                                // If the center is not in a polygon
                                if(!mapping.ContainsKey((oid, table.GetName())))
                                {
                                    mapping[(oid, table.GetName())] = -1;
                                }
                            }
                        }
                    }
                }
            });
            return mapping;
        }
    }

    /// <summary>
    /// Button implementation to show the DockPane.
    /// </summary>
    internal class TransferAttribute_ShowButton : Button
    {
        protected override void OnClick()
        {
            TransferAttributeViewModel.Show();
        }
    }
}
