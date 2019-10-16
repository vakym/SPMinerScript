using System;
using System.Collections.Generic;
using VRageMath;
using VRage.Game;
using VRage.Library;
using System.Text;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Ingame;
using Sandbox.Common;
using Sandbox.Game;
using VRage.Collections;
using VRage.Game.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Threading.Tasks;

namespace SpaceEngineers
{
    public class Program : MyGridProgram
    {
        private GridControl gridControl;
        IMyShipController shipController;
        Vector3D planetG;
        Vector3D planetGNorm;
        Vector3D speedVector;
        public Program()
        {
            var gyros = new List<IMyGyro>();
            var thrusters = new List<IMyThrust>();
            GridTerminalSystem.GetBlocksOfType(gyros);
            GridTerminalSystem.GetBlocksOfType(thrusters);
            shipController = GridTerminalSystem.GetBlockWithName("Controller") as IMyShipController;
            gridControl = new GridControl(gyros, thrusters, shipController);
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        public void Main(string argument)
        {
            planetG = shipController.GetNaturalGravity();
            planetGNorm = Vector3D.Normalize(planetG);
            speedVector = shipController.GetShipVelocities().LinearVelocity;
            gridControl.MoveGrid(planetGNorm, speedVector);
        }

        private class GridControl
        {
            public bool StabilizeGrid { get; set; }
            public List<IMyGyro> Gyroscopes { get; }
            public List<IMyThrust> Thrusters { get; }
            public IMyShipController ShipController { get; }

            public GridControl (List<IMyGyro> gyroscopes, List<IMyThrust> thrusters, IMyShipController shipController)
            {
                if (gyroscopes == null) throw new ArgumentNullException("gyroscopes are null");
                if (thrusters == null ) throw new ArgumentNullException("thrusters are null");
                if (shipController == null) throw new ArgumentNullException("shipController is null");

                Gyroscopes = gyroscopes;
                Thrusters = thrusters;
                ShipController = shipController;
                StabilizeGrid = true;
                ShipController.CustomData = "";
                foreach (var gyro in Gyroscopes)
                {
                    gyro.GyroOverride = true;
                }
            }

            public void MoveGrid(Vector3D planetGravity, Vector3D speedVector)
            {
               
                var forward = Vector3D.Normalize(Vector3D.Reject(ShipController.WorldMatrix.Forward, planetGravity)) * ShipController.MoveIndicator.Z;
                var rotation = Vector3D.Normalize(
                                                    Vector3D.Reject(
                                                                    ShipController.WorldMatrix.Left,
                                                                    planetGravity)) 
                                                    * ShipController.RollIndicator;
                var orientationVector = Vector3D.Normalize(
                                                            planetGravity +
                                                            SpecialNormalizeSpeedVector(speedVector) +
                                                            forward +
                                                            rotation);
                SendSingnalToGyro((float)orientationVector.Dot(ShipController.WorldMatrix.Forward),
                                 (float)orientationVector.Dot(ShipController.WorldMatrix.Left),
                                 ShipController.MoveIndicator.X);
            }

            private Vector3D SpecialNormalizeSpeedVector(Vector3D speedVector)
            {
                return speedVector / 3*speedVector.Length() + 2;
            }
            private void SendSingnalToGyro(float pitch, float roll, float yaw)
            {
                foreach (var gyro in Gyroscopes)
                {
                    gyro.Pitch = -pitch;
                    gyro.Roll = roll;
                    gyro.Yaw = yaw;
                }
            }
        }
    }
}
