using SDL3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static SDL3.SDL3;

namespace Ryujinx.Input.SDL3
{
    public unsafe class SDL3MotionDriver : IHandheld, IDisposable
    {
        private readonly Dictionary<SDL_SensorType, SDL_Sensor> sensors;
        private bool _disposed;

        public SDL3MotionDriver()
        {
            int result = SDL_Init(SDL_InitFlags.Sensor);
            if (result < 0)
            {
                throw new InvalidOperationException($"SDL sensor initialization failed: {SDL_GetError()}");
            }
            sensors = SDL_GetSensors().ToArray().ToDictionary(SDL_GetSensorTypeForID, SDL_OpenSensor);
        }

        ~SDL3MotionDriver()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing && sensors != null)
            {
                foreach (var sensor in sensors.Values)
                {
                    if (sensor != IntPtr.Zero)
                    {
                        SDL_CloseSensor(sensor);
                    }
                }
            }

            _disposed = true;
        }

        public Vector3 GetMotionData(MotionInputId inputType)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            return inputType switch
            {
                MotionInputId.Gyroscope => GetSensorVector(SDL_SensorType.Gyro) * 180 / MathF.PI,
                MotionInputId.Accelerometer => GetSensorVector(SDL_SensorType.Accel) / SDL_STANDARD_GRAVITY,
                _ => Vector3.Zero
            };
        }

        private Vector3 GetSensorVector(SDL_SensorType sensorType)
        {
            if (!sensors.TryGetValue(sensorType, out SDL_Sensor sensor))
            {
                return Vector3.Zero;
            }

            var data = stackalloc float[3];
            if (SDL_GetSensorData(sensor, data, 3) < 0)
            {
                return Vector3.Zero;
            }

            return new Vector3(data[0], data[1], data[2]);
        }
    }
}
