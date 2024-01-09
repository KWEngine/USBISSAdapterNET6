using System.IO.Ports;

namespace USBISSAdapter
{
    public class UsbIssAdapter
    {
        private readonly SerialPort _port;
        private bool _usbFound = false;

        private UsbIssAdapter()
        {
            _port = new SerialPort
            {
                PortName = "unknown port name",
                ReadTimeout = 32,
                WriteTimeout = 32
            };
        }

        /// <summary>
        /// Initialisiert ein I2C-Gerät am vorgegebenen Port mit der vorgegebenen Hardware-Adresse (8bit)
        /// </summary>
        /// <param name="portName">COM-Portname (in der Regel "COM3")</param>
        public UsbIssAdapter(string portName)
        {
            _port = new SerialPort
            {
                PortName = portName,
                ReadTimeout = 32,
                WriteTimeout = 32
            };

            if (_port.IsOpen)
                _port.Close();
        }

        /// <summary>
        /// Öffnet die Verbindung zum I2C-Gerät
        /// </summary>
        public void Open()
        {
            try
            {
                _port.Open();
                _usbFound = true;

                byte[] buffer = new byte[3];
                // Setzte USB-Adapter auf I2C-Modus:
                buffer[0] = 0x5A;       // USB-ISS Kommando-Byte
                buffer[1] = 0x02;       // USB-ISS Modus
                buffer[2] = 0x60;       // 100KHz hardware I2C-Verbindung
                _port.Write(buffer, 0, 3);

                buffer = Receive(2);    // Antwortbytes lesen
                if (buffer[0] == 0x00)     // Wenn 0 zurückkommt, ist etwas falsch:
                {
                    _usbFound = false;
                    _port.Close();
                    throw new Exception("I2C adapter could not be initialized. Please check your USB connection.");
                }
                else
                {
                    _usbFound = true;
                }
            }
            catch (Exception ex)
            {
                _usbFound = false;
                throw new Exception("Port could not be initialized. Maybe you entered the wrong port number?", ex);
            }
        }

        /// <summary>
        /// Schließt einen geöffneten I2C Port
        /// </summary>
        public void Close()
        {
            if (_port.IsOpen)
            {
                _port.Close();
            }
        }

        /// <summary>
        /// Schreibt in das angegebene Register eine folge von Bytes
        /// </summary>
        /// <param name="register">Zielregister</param>
        /// <param name="bytes">zu schreibende Bytes</param>
        /// <returns>Return code (0 = Fehler, >0 = OK)</returns>
        public byte Write(byte i2cAdress, byte register, params byte[] bytes)
        {
            if (!_usbFound)
                throw new Exception("Device not found.");

            bytes ??= Array.Empty<byte>();
            List<byte> transmitSequence = new()
            {
                0x55,
                (byte)((i2cAdress << 1) + 0), // write operation
                register,
                (byte)bytes.Length
            };
            foreach (byte b in bytes)
            {
                transmitSequence.Add(b);
            }
            _port.Write(transmitSequence.ToArray(), 0, transmitSequence.Count);

            byte[] receivedBytes = new byte[1];
            _port.Read(receivedBytes, 0, 1);

            return receivedBytes[0];
        }

        /// <summary>
        /// Liest eine vorgegebene Anzahl an Bytes aus dem angegebenen Register des I2C-Geräts
        /// </summary>
        /// <param name="register">Adresse des Registers</param>
        /// <param name="count">Anzahl der zu lesenden Bytes</param>
        /// <returns>gelesene Bytes</returns>
        public byte[] Read(byte i2cAddress, byte register, byte count)
        {
            if (!_usbFound)
                throw new Exception("Device not found.");

            byte[] receivedBytes = new byte[count];

            List<byte> transmitSequence = new()
            {
                0x55,
                (byte)((i2cAddress << 1) + 1), // read operation
                register,
                count
            };
            _port.Write(transmitSequence.ToArray(), 0, transmitSequence.Count);
            _port.Read(receivedBytes, 0, count);

            return receivedBytes;
        }

        /// <summary>
        /// Versucht, eine bestimmte Anzahl an Bytes auf dem I2C-Bus zu lesen.
        /// </summary>
        /// <param name="readBytes">Anzahl zu lesender Bytes</param>
        /// <returns>Gelesene Bytes in einem Array</returns>
        private byte[] Receive(byte readBytes)
        {
            byte[] returnArray = new byte[readBytes];
            for (byte x = 0; x < readBytes; x++)
            {
                try
                {
                    if (_usbFound)
                    {
                        _port.Read(returnArray, x, 1);
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Could not read data from I2C device.", ex);
                }
            }
            return returnArray;
        }

        public bool ValidateAddress(byte address)
        {
            byte[] command = new byte[2];

            command[0] = 0x58;
            command[1] = address;

            _port.Write(command, 0, 2);

            byte[] receiveBuffer = new byte[1];
            _port.Read(receiveBuffer, 0, 1);
            return receiveBuffer[0] != 0;
        }
    }
}
