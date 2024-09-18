using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;

namespace RobotCommunicationSubmission
{
    //AHM Serial Comms Class
    class LeanComms
    {
        public enum MbedDevice { F401RE, LPC1768 };
        public MbedDevice device;
        ushort vid;
        ushort pid;
        SerialDevice serialPort = null;
        DataReader dataReaderObject;
        DataWriter dataWriterObject;
        CancellationTokenSource ReadCancellationTokenSource = null;
        CancellationTokenSource WriteCancellationTokenSource = null;
        string DataReceived;
        List<string> dataList;

        public delegate void SerialDataHandler(NewDataArgs e);
        public event SerialDataHandler NewSerialData;

        int RunNum = 0;
        public static bool debugging = false;

        public LeanComms(MbedDevice mbed)
        {
            device = mbed;
            FindAndStreamDevice();
        }

        public async void FindAndStreamDevice()
        {
            try
            {
                DeviceInformationCollection dis = null;
                //FindAndStreamDevice the device we want. LPC1768 device (VendorID 0D28, ProductID 0204), Nucleo-F411RE device (VendorID 0483, ProductID 374B)
                if (device == MbedDevice.F401RE)
                {
                    vid = 0x0483;
                    pid = 0x374B;
                }
                else if (device == MbedDevice.LPC1768)
                {
                    vid = 0x0D28;
                    pid = 0x0204;
                }
                else
                {
                    throw new Exception("Device has not been selected properly");
                }
                dis = await DeviceInformation.FindAllAsync(SerialDevice.GetDeviceSelectorFromUsbVidPid(vid, pid));
                if (dis.Any())
                {
                    serialPort = await SerialDevice.FromIdAsync(dis.First().Id);
                    if (serialPort != null)
                    {
                        try
                        {
                            // Configure serial settings
                            serialPort.DataBits = 8;
                            serialPort.StopBits = SerialStopBitCount.One;
                            serialPort.Parity = SerialParity.None;
                            serialPort.ReadTimeout = TimeSpan.FromMilliseconds(10);
                            serialPort.BaudRate = 9600;
                            ReadCancellationTokenSource = new CancellationTokenSource();
                            Listen();
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }
        private async void Listen()
        {
            try
            {
                if (serialPort != null)
                {
                    dataReaderObject = new DataReader(serialPort.InputStream);

                    // keep reading the serial input
                    while (true)
                    {
                        await ReadAsync(ReadCancellationTokenSource.Token);
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        public async void WriteSerial(string message)
        {
            WriteCancellationTokenSource = new CancellationTokenSource();
            await WriteAsync(WriteCancellationTokenSource.Token, message);
        }

        private async Task ReadAsync(CancellationToken cancellationToken)
        {
            Task<UInt32> loadAsyncTask;

            uint ReadBufferLength = 2048;

            // If task cancellation was requested, comply
            cancellationToken.ThrowIfCancellationRequested();

            // Set InputStreamOptions to complete the asynchronous read operation when one or more bytes is available
            dataReaderObject.InputStreamOptions = InputStreamOptions.Partial;

            // Create a task object to wait for data on the serialPort.InputStream
            loadAsyncTask = dataReaderObject.LoadAsync(ReadBufferLength).AsTask(cancellationToken);

            // Launch the task and wait
            UInt32 bytesRead = await loadAsyncTask;
            if (bytesRead > 0)
            {
                try
                {
                    if (debugging)
                    {
                        byte[] test = new byte[bytesRead];
                        dataReaderObject.ReadBytes(test);
                        string s = Encoding.UTF8.GetString(test);
                        string[] sa = new string[] { s };
                        NewDataArgs nda = new NewDataArgs(sa);
                        NewSerialData(nda);
                    }
                    else
                    {
                        DataReceived = dataReaderObject.ReadString(bytesRead);
                        string[] splitData = DataReceived.Split(new char[] { '\r', '\n' });
                        dataList = new List<string>();
                        foreach (string s in splitData)
                        {
                            if (s.Contains("{\"Data\":") && s.Contains("}}"))
                            {
                                string s1 = s.Remove(0, 6);
                                string s2 = s1.Remove(s1.LastIndexOf('}'));

                                dataList.Add(s2);
                            }
                        }
                        if (dataList.Count > 0)
                        {
                            NewDataArgs nda = new NewDataArgs(dataList.ToArray());
                            NewSerialData(nda);
                        }
                    }
                }
                catch (Exception x)
                {
                    ReadCancellationTokenSource.Cancel();
                    serialPort.Dispose();
                    FindAndStreamDevice();
                }
            }
        }

        private async Task WriteAsync(CancellationToken cancellationToken, string message)
        {
            Task<UInt32> storeAsyncTask;
            cancellationToken.ThrowIfCancellationRequested();
            dataWriterObject = new DataWriter(serialPort.OutputStream);
            // Load the text from the sendText input text box to the dataWriter object
            dataWriterObject.WriteString(message);

            // Launch an async task to complete the write operation
            storeAsyncTask = dataWriterObject.StoreAsync().AsTask(cancellationToken);

            UInt32 bytesWritten = await storeAsyncTask;
            if (bytesWritten > 0)
            {
                // status += "bytes written successfully!";
            }
        }
    }

    public class NewDataArgs : EventArgs
    {
        public NewDataArgs(string[] Data)
        {
            this.NewData = Data;
        }

        public string[] NewData { get; }
    }
}
