using RPLidar4Net.Api.Data;
using RPLidar4Net.Core;
using RPLidar4Net.IO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

namespace Rahmanda.RpLidar4Net
{
    public class RPLidar : MonoBehaviour
    {
        [Header("Connection Settings")]
        [Tooltip("Name of the serial port connected to the RPLidar, for example COM4.")]
        public string portName = "COM4";
        [Tooltip("Serial baud rate. Use the value required by the specific RPLidar model. Supported values are 115200, 256000 460800, and 1000000.")]
        public int baudRate = 460800;
        [Tooltip("Read and write timeout in milliseconds for serial commands.")]
        public int timeout = 3000;
        [Tooltip("Connect to the RPLidar automatically during Awake().")]
        public bool connectOnAwake = true;

        [Header("Device Settings")]
        [Tooltip("Requested motor speed. The value is interpreted as RPM when motor control is RPM, or as PWM when motor control is PWM.")]
        public ushort desiredRotationFrequency = 660;

        [Header("Device Info")]
        [Tooltip("RPLidar model identifier reported by the device.")]
        [SerializeField] private string m_modelID;
        [Tooltip("RPLidar serial number reported by the device.")]
        [SerializeField] private string m_serialNumber;
        [Tooltip("RPLidar firmware version reported by the device.")]
        [SerializeField] private string m_firmwareVersion;
        [Tooltip("RPLidar hardware version reported by the device.")]
        [SerializeField] private string m_hardwareVersion;
        [Tooltip("Motor control mode supported by the connected RPLidar: RPM, PWM, or None.")]
        [SerializeField] private MotorControlSupport m_motorControlSupport;
        [Tooltip("Minimum scan frequency supported by the connected RPLidar.")]
        [SerializeField] private ushort m_minScanFrequency;
        [Tooltip("Maximum scan frequency supported by the connected RPLidar.")]
        [SerializeField] private ushort m_maxScanFrequency;

        [Header("Events")]
        public UnityEvent<bool> onIsConnectedChanged = new UnityEvent<bool>();
        public UnityEvent<Point[]> onScanFrame = new UnityEvent<Point[]>();

        private static readonly HashSet<int> SupportedBaudRates = new HashSet<int>()
        {
            115200,
            256000,
            460800,
            1000000
        };

        private readonly ConcurrentQueue<bool> m_pendingConnectionStates = new ConcurrentQueue<bool>();
        private readonly ConcurrentQueue<Point[]> m_pendingPoints = new ConcurrentQueue<Point[]>();
        private readonly object m_pointsLock = new object();

        private Point[] m_latestPoints = Array.Empty<Point>();
        private Thread m_worker;
        private volatile bool m_stopRequested;
        private bool m_isConnected;

        public string ModelID => m_modelID;
        public string SerialNumber => m_serialNumber;
        public string FirmwareVersion => m_firmwareVersion;
        public string HardwareVersion => m_hardwareVersion;
        public MotorControlSupport MotorControlSupport => m_motorControlSupport;
        public ushort MinScanFrequency => m_minScanFrequency;
        public ushort MaxScanFrequency => m_maxScanFrequency;

        private void Awake()
        {
            if (connectOnAwake) Connect();
        }

        private void Update()
        {
            while (m_pendingConnectionStates.TryDequeue(out bool connected))
                SetConnectionState(connected);

            while (m_pendingPoints.TryDequeue(out Point[] points))
            {
                lock (m_pointsLock)
                    m_latestPoints = points;
                onScanFrame.Invoke(m_latestPoints);
            }
        }

        private void OnDestroy()
        {
            Disconnect();
        }

        public void Connect(string newPortName = null, int newBaudRate = -1, int newTimeout = -1, int newDesiredRotationFrequency = -1)
        {
            string requestedPortName = !string.IsNullOrEmpty(newPortName) ? newPortName : portName;
            int requestedBaudRate = newBaudRate >= 0 ? newBaudRate : baudRate;
            int requestedTimeout = newTimeout >= 0 ? newTimeout : timeout;
            ushort requestedDesiredRotationFrequency = newDesiredRotationFrequency >= 0 ? (ushort)newDesiredRotationFrequency : desiredRotationFrequency;

            if (!IsValidBaudRate(requestedBaudRate))
                throw new ArgumentException($"[RPLidar] Invalid baud rate: {requestedBaudRate}. Supported baud rates are: {string.Join(", ", SupportedBaudRates)}");

            portName = requestedPortName;
            baudRate = requestedBaudRate;
            timeout = requestedTimeout;
            desiredRotationFrequency = requestedDesiredRotationFrequency;

            Disconnect();
            m_stopRequested = false;
            m_worker = new Thread(ReadLidar) { IsBackground = true, Name = "RPLidar" };
            m_worker.Start();
        }

        public void Disconnect()
        {
            Thread worker = m_worker;
            if (worker == null)
            {
                SetConnectionState(false);
                return;
            }

            m_stopRequested = true;
            if (worker != Thread.CurrentThread && worker.IsAlive)
            {
                worker.Join(2000);
                if (worker.IsAlive)
                    Debug.Log("[RPLidar] Worker did not stop before the timeout.");
            }

            if (!worker.IsAlive)
                m_worker = null;
            SetConnectionState(false);
        }

        private void ReadLidar()
        {
            RPLidarSerialDevice m_device = null;
            try
            {
                Debug.Log($"[RPLidar] Connecting to port {portName} at baud rate {baudRate}.");

                m_device = new RPLidarSerialDevice(portName, baudRate, timeout);
                try
                {
                    m_device.Connect();
                }
                catch (Exception e)
                {
                    Debug.LogError("[RPLidar] Connection failed. Please make sure the port is available and baud rate is matched with the RPLidar device series.");
                    throw e;
                }

                m_device.StopMotor();
                m_device.SetMotorSpeed(desiredRotationFrequency);

                SaveDeviceInfo(m_device);

                m_device.NewScan += OnNewScan;
                m_device.StartScan();

                m_pendingConnectionStates.Enqueue(true);

                Debug.Log($"[RPLidar] Connected on port {portName} at baud rate {baudRate}.");
                Debug.Log($"[RPLidar] Device Information:\n" +
                    $"Firmware Version: {m_firmwareVersion}\n" +
                    $"Hardware Version: {m_hardwareVersion}\n" +
                    $"Model ID: {m_modelID}\n" +
                    $"Serial Number: {m_serialNumber}\n" +
                    $"Motor Control Support: {m_motorControlSupport}\n" +
                    $"Desired Rotation Freq: {desiredRotationFrequency}\n" +
                    $"Min Rotation Frequency: {m_minScanFrequency}\n" +
                    $"Max Rotation Frequency: {m_maxScanFrequency}");

                while (!m_stopRequested) { }
            }
            catch (Exception e)
            {
                if (e.Message == "RESULT_INVALID_ANS_TYPE")
                    Debug.LogError("[RPLidar] Error: RESULT_INVALID_ANS_TYPE. This may indicate a communication issue or an unsupported command.\n" + e);
                else
                    Debug.LogError("[RPLidar] Error: " + e);
                m_pendingConnectionStates.Enqueue(false);
            }
            finally
            {
                if (m_device != null)
                {
                    try
                    {
                        m_device.NewScan -= OnNewScan;
                        m_device.StopScan();
                        m_device.Disconnect();
                        m_device.Dispose();
                        Debug.Log("[RPLidar] Device disposed.");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("[RPLidar] Failed to dispose device. Error: \n" + e);
                    }

                    m_pendingConnectionStates.Enqueue(false);
                }

                Debug.Log("[RPLidar] Disconnected.");
            }
        }

        private void OnNewScan(object sender, NewScanEventArgs e)
        {
            Point[] points = e.Points.ToArray();
            m_pendingPoints.Enqueue(points);
        }

        private void SetConnectionState(bool connected)
        {
            if (m_isConnected == connected)
                return;
            m_isConnected = connected;
            onIsConnectedChanged.Invoke(connected);
        }

        private void SaveDeviceInfo(RPLidarSerialDevice m_device)
        {
            m_motorControlSupport = m_device.MotorControlSupport;

            InfoDataResponse infoDataResponse = m_device.GetInfo();
            m_modelID = infoDataResponse.ModelID;
            m_firmwareVersion = infoDataResponse.FirmwareVersion;
            m_serialNumber = infoDataResponse.SerialNumber;
            m_hardwareVersion = infoDataResponse.HardwareVersion;

            LidarConfigDataResponse desiredRotationFrequencyResponse = m_device.GetDesiredRotationFrequency();
            desiredRotationFrequency = m_motorControlSupport == MotorControlSupport.PWM ? desiredRotationFrequencyResponse.DesiredRotationFrequencyPWM : desiredRotationFrequencyResponse.DesiredRotationFrequencyRPM;

            LidarConfigDataResponse minRotationFrequencyResponse = m_device.GetMinRotationFrequency();
            m_minScanFrequency = minRotationFrequencyResponse.MinRotationFrequency;

            LidarConfigDataResponse maxRotationFrequencyResponse = m_device.GetMaxRotationFrequency();
            m_maxScanFrequency = maxRotationFrequencyResponse.MaxRotationFrequency;
        }

        private static bool IsValidBaudRate(int baudRate)
        {
            return SupportedBaudRates.Contains(baudRate);
        }
    }
}