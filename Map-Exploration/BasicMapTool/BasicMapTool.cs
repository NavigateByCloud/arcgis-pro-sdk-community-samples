/*

   Copyright 2019 Esri

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.

   See the License for the specific language governing permissions and
   limitations under the License.

*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;

namespace BasicMapTool {
    internal class BasicMapTool : MapTool {

        private bool _zMode = false;
        Camera mainCamera;
        //double x, y, z;

        public BasicMapTool() : base() {
            x = y = z = 0;
            this.OverlayControlID = "BasicMapTool_BasicEmbeddableControl";
            //Embeddable control can be resized
            OverlayControlCanResize = true;
            //Specify ratio of 0 to 1 to place the control
            OverlayControlPositionRatio = new System.Windows.Point(0, 0); //top left
            _zoomToCmd = new RelayCommand(() => MapView.Active.ZoomToAsync(Camera, TimeSpan.FromSeconds(1.5)), () => { return MapView.Active != null; });
            _panToCmd = new RelayCommand(() => MapView.Active.PanToAsync(Camera, TimeSpan.FromSeconds(1.5)), () => { return MapView.Active != null; });

            MapViewCameraChangedEvent.Subscribe(OnCameraChanged);
            ActiveMapViewChangedEvent.Subscribe(OnActiveMapViewChanged);

            if (MapView.Active != null)
                Camera = MapView.Active.Camera; x = Camera.X; y = Camera.Y; z = Camera.Z;
        }

        ~BasicMapTool()
        {
            _zoomToCmd.Disconnect();
            _panToCmd.Disconnect();

            ActiveMapViewChangedEvent.Unsubscribe(OnActiveMapViewChanged);
            MapViewCameraChangedEvent.Unsubscribe(OnCameraChanged);
        }

        private Camera _camera;
        private double x, y, z;
        public Camera Camera {
            get { return _camera; }
            set {
                SetProperty(ref _camera, value, () => Camera);
            }
        }

        private RelayCommand _panToCmd;
        public ICommand PanToCmd {
            get { return _panToCmd; }
        }

        private RelayCommand _zoomToCmd;
        public ICommand ZoomToCmd {
            get { return _zoomToCmd; }
        }

        protected override Task OnToolActivateAsync(bool active) {
            return base.OnToolActivateAsync(active);
        }

        private void OnCameraChanged(MapViewCameraChangedEventArgs obj)
        {
            if (obj.MapView == MapView.Active)
                Camera = obj.CurrentCamera;
            updateText(Camera);
        }

        private void OnActiveMapViewChanged(ActiveMapViewChangedEventArgs obj)
        {
            if (obj.IncomingView == null)
            {
                Camera = null;
                return;
            }

            Camera = obj.IncomingView.Camera;
            updateText(Camera);
        }


        protected override void OnToolKeyDown(MapViewKeyEventArgs k) {
            //We will do some basic key handling to allow panning
            //from the arrow keys
            if (k.Key == Key.Left || // -x 
                k.Key == Key.Right || // x
                k.Key == Key.Up || // y
                k.Key == Key.Down || // -y
                k.Key == Key.W || // z
                k.Key == Key.S || // -z
                k.Key == Key.A || // heading
                k.Key == Key.D || // -heading
                k.Key == Key.Q || // scale
                k.Key == Key.E || // -scale
                k.Key == Key.Z || // roll
                k.Key == Key.X || // -roll
                k.Key == Key.C || // pitch
                k.Key == Key.V) // -pitch
                k.Handled = true;
            base.OnToolKeyDown(k);
        }

        void updateText(Camera camera)
        {
            var vm = OverlayEmbeddableControl as BasicEmbeddableControlViewModel;
            if (vm == null)
                return;

            var sb = new StringBuilder();
            sb.AppendLine(string.Format("X: {0}", camera.X));
            sb.Append(string.Format("Y: {0}", camera.Y));
            sb.AppendLine();
            sb.Append(string.Format("Z: {0}", camera.Z));
            sb.AppendLine();
            sb.Append(string.Format("heading: {0}", camera.Heading));
            sb.AppendLine();
            sb.Append(string.Format("pitch: {0}", camera.Pitch));
            sb.AppendLine();
            sb.Append(string.Format("roll: {0}", camera.Roll));
            sb.AppendLine();
            sb.Append(string.Format("scale: {0}", camera.Scale));
            sb.AppendLine();
            sb.Append(camera.Viewpoint.ToString());
            vm.Text = sb.ToString();
        }

        protected override Task HandleKeyDownAsync(MapViewKeyEventArgs k) {
            var camera = MapView.Active.Camera;

            double dx = MapView.Active.Extent.Width / 20;
            double dy = MapView.Active.Extent.Height / 20;
            double dz = 20.0;//20 meters vertical change per key stroke

            bool shiftKey = (Keyboard.Modifiers & ModifierKeys.Shift)
                                     == ModifierKeys.Shift;

            float degree = 10f;
            //When in 3D mode use the Shift key to switch from "Y" position change
            //which is 'Up','Down' in 2D but is 'forward','back' in 3D to
            //"Z" position change which is 'Up','Down' in 3D
            if (!_zMode && ActiveMapView.ViewingMode != MapViewingMode.Map)
            {
                _zMode = shiftKey;
            }

            switch (k.Key) {
                case Key.Left:
                    camera.X -= dx;
                    break;
                case Key.Right:
                    camera.X += dx;
                    break;
                case Key.Up:
                    if (_zMode) {
                        camera.Z += dz;
                    }
                    else {
                       camera.Y += dy;
                    }
                    break;
                case Key.Down:
                    if (_zMode) {
                        camera.Z -= dz;
                    }
                    else {
                        camera.Y -= dy;
                    }
                    break;
                case Key.Q: 
                    if (_zMode) {
                        //camera.Viewpoint = CameraViewpoint.LookFrom;
                        
                        camera.Heading += 10;
                    }
                    else {
                        camera.Viewpoint = CameraViewpoint.LookAt;
                        camera.X = x;
                        camera.Y = y;
                        camera.Z = z;
                        camera.Heading += 10;
                    }
                    break;
                case Key.E:
                    if (_zMode)
                    {
                        //camera.Viewpoint = CameraViewpoint.LookFrom;

                        camera.Heading -= 10;
                    }
                    else
                    {
                        camera.Viewpoint = CameraViewpoint.LookAt;
                        camera.X = x;
                        camera.Y = y;
                        camera.Z = z;

                        camera.Heading -= 10;
                    }
                    break;
                case Key.W:
                    if (_zMode)
                    {
                        camera.Scale -= 100;
                    }
                    else
                    {
                        camera.Scale -= 100;
                    }
                    break;
                case Key.S:
                    if (_zMode)
                    {
                        camera.Scale += 100;
                    }
                    else
                    {
                        camera.Scale += 100;
                    }
                    break;
                case Key.A: break;
                case Key.D: break;

            }
            
            return MapView.Active.ZoomToAsync(camera, new TimeSpan(0, 0, 0, 0, 250));
        }

        protected override void OnToolKeyUp(MapViewKeyEventArgs k) {
            _zMode = false;
            base.OnToolKeyUp(k);
        }
        protected override void OnToolMouseDown(MapViewMouseButtonEventArgs e) {
            //On mouse down check if the mouse button pressed is the left mouse button. If it is handle the event.
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
                e.Handled = true;
        }
        Camera CopyCamera(Camera copyFrom)
        {

            return new Camera(copyFrom.X, copyFrom.Y, copyFrom.Z, copyFrom.Scale, copyFrom.Heading, copyFrom.SpatialReference, copyFrom.Viewpoint);
        }

        protected override Task HandleMouseDownAsync(MapViewMouseButtonEventArgs e) {
            //Get the instance of the ViewModel
            var vm = OverlayEmbeddableControl as BasicEmbeddableControlViewModel;
            if (vm == null)
                return Task.FromResult(0);

            //Get the map coordinates from the click point and set the property on the ViewModel.
            return QueuedTask.Run(() =>
            {
                var mapPoint = MapView.Active.ClientToMap(e.ClientPoint);
                var sb = new StringBuilder();
                sb.AppendLine(string.Format("X: {0}", mapPoint.X));
                sb.Append(string.Format("Y: {0}", mapPoint.Y));
                if (mapPoint.HasZ) {
                    sb.AppendLine();
                    sb.Append(string.Format("Z: {0}", mapPoint.Z));
                }
                vm.Text = sb.ToString();
            });
        }
    }
}
