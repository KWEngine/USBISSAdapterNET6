using USBISSAdapter;

namespace USBISSAdapterTest
{
    internal class Program
    {
        static void Main()
        {
            UsbIssAdapter adapter = new UsbIssAdapter("COM3");
            adapter.Open();

            byte[] result = adapter.Read(0x39, 0x92, 1);

            adapter.Close();
        }
    }
}
