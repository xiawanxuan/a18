using UnityEngine;
using System.Collections.Generic;
using SoftFluidPuzzle.Core;

namespace SoftFluidPuzzle.PressureMechanisms
{
    public class MechanismManager : Singleton<MechanismManager>
    {
        [Header("Settings")]
        public bool enableMechanisms = true;

        private List<FluidPressureSensor> _sensors = new List<FluidPressureSensor>();
        private List<MechanismBase> _mechanisms = new List<MechanismBase>();
        private List<MechanismConnection> _connections = new List<MechanismConnection>();

        public List<FluidPressureSensor> AllSensors => _sensors;
        public List<MechanismBase> AllMechanisms => _mechanisms;

        protected override void Awake()
        {
            base.Awake();
        }

        public void RegisterSensor(FluidPressureSensor sensor)
        {
            if (!_sensors.Contains(sensor))
            {
                _sensors.Add(sensor);
            }
        }

        public void UnregisterSensor(FluidPressureSensor sensor)
        {
            _sensors.Remove(sensor);
        }

        public void RegisterMechanism(MechanismBase mechanism)
        {
            if (!_mechanisms.Contains(mechanism))
            {
                _mechanisms.Add(mechanism);
            }
        }

        public void UnregisterMechanism(MechanismBase mechanism)
        {
            _mechanisms.Remove(mechanism);
        }

        public void AddConnection(FluidPressureSensor sensor, MechanismBase mechanism, bool invert = false)
        {
            MechanismConnection connection = new MechanismConnection
            {
                sensor = sensor,
                mechanism = mechanism,
                invertSignal = invert
            };
            _connections.Add(connection);

            sensor.onActivated.AddListener(() => OnSensorActivated(sensor));
            sensor.onDeactivated.AddListener(() => OnSensorDeactivated(sensor));
        }

        private void OnSensorActivated(FluidPressureSensor sensor)
        {
            if (!enableMechanisms) return;

            foreach (MechanismConnection connection in _connections)
            {
                if (connection.sensor == sensor)
                {
                    if (connection.invertSignal)
                    {
                        connection.mechanism.Deactivate();
                    }
                    else
                    {
                        connection.mechanism.Activate();
                    }
                }
            }
        }

        private void OnSensorDeactivated(FluidPressureSensor sensor)
        {
            if (!enableMechanisms) return;

            foreach (MechanismConnection connection in _connections)
            {
                if (connection.sensor == sensor)
                {
                    if (connection.invertSignal)
                    {
                        connection.mechanism.Activate();
                    }
                    else
                    {
                        connection.mechanism.Deactivate();
                    }
                }
            }
        }

        public void ActivateAllMechanisms()
        {
            foreach (MechanismBase mechanism in _mechanisms)
            {
                if (mechanism != null)
                {
                    mechanism.Activate();
                }
            }
        }

        public void DeactivateAllMechanisms()
        {
            foreach (MechanismBase mechanism in _mechanisms)
            {
                if (mechanism != null)
                {
                    mechanism.Deactivate();
                }
            }
        }

        public void ResetAllMechanisms()
        {
            foreach (MechanismBase mechanism in _mechanisms)
            {
                if (mechanism != null)
                {
                    mechanism.ResetMechanism();
                }
            }

            foreach (FluidPressureSensor sensor in _sensors)
            {
                if (sensor != null)
                {
                    sensor.ResetSensor();
                }
            }
        }

        public FluidPressureSensor GetSensorById(string id)
        {
            foreach (FluidPressureSensor sensor in _sensors)
            {
                if (sensor != null && sensor.sensorId == id)
                {
                    return sensor;
                }
            }
            return null;
        }

        public MechanismBase GetMechanismById(string id)
        {
            foreach (MechanismBase mechanism in _mechanisms)
            {
                if (mechanism != null && mechanism.mechanismId == id)
                {
                    return mechanism;
                }
            }
            return null;
        }

        public int GetActiveMechanismCount()
        {
            int count = 0;
            foreach (MechanismBase mechanism in _mechanisms)
            {
                if (mechanism != null && mechanism.isActive)
                {
                    count++;
                }
            }
            return count;
        }
    }

    [System.Serializable]
    public class MechanismConnection
    {
        public FluidPressureSensor sensor;
        public MechanismBase mechanism;
        public bool invertSignal = false;
    }
}
