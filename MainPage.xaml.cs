using System;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace RobotCommunicationSubmission
{
    public sealed partial class MainPage : Page
    {
        // Variables for serial communication and media playback
        private DataReader dataReader;
        private DataWriter dataWriter;
        private SerialDevice serialPort;
        private MediaElement mediaPlayerElement; // MediaElement for MP3 playback
        private MediaPlayer mediaPlayer; // MediaPlayer for controlling playback

        public MainPage()
        {
            this.InitializeComponent();
            InitializeSerial();
            InitializeMP3Player();
        }

        // Initialize serial communication
        private async void InitializeSerial()
        {
            try
            {
                string aqs = SerialDevice.GetDeviceSelector();
                var devices = await DeviceInformation.FindAllAsync(aqs);

                if (devices.Count > 0)
                {
                    var deviceInfo = devices[0];
                    serialPort = await SerialDevice.FromIdAsync(deviceInfo.Id);

                    if (serialPort != null)
                    {
                        serialPort.BaudRate = 9600;
                        serialPort.DataBits = 8;
                        serialPort.StopBits = SerialStopBitCount.One;
                        serialPort.Parity = SerialParity.None;

                        // Initialize data reader and writer
                        dataReader = new DataReader(serialPort.InputStream);
                        dataWriter = new DataWriter(serialPort.OutputStream);

                        // Send initial message to the board
                        SendMessage("UWP is connected to board");

                        // Start reading data from the serial port
                        ReadSerial();
                    }
                    else
                    {
                        AddMessage("Error: Unable to initialize serial port.");
                    }
                }
                else
                {
                    AddMessage("Error: No serial devices found.");
                }
            }
            catch (Exception ex)
            {
                AddMessage($"Error initializing serial port: {ex.Message}");
            }
        }

        // Initialize MP3 player
        private async void InitializeMP3Player()
        {
            try
            {
                mediaPlayer = new MediaPlayer();
                mediaPlayerElement = new MediaElement();
                mediaPlayerElement.AutoPlay = false;
                // Optionally set other MediaElement properties here

                // Add MediaElement to the defined UI element (myStackPanel)
                myStackPanel.Children.Add(mediaPlayerElement);
            }
            catch (Exception ex)
            {
                AddMessage($"Error initializing MP3 player: {ex.Message}");
            }
        }

        // Read data from the serial port
        private async void ReadSerial()
        {
            StringBuilder receivedData = new StringBuilder();

            try
            {
                while (true)
                {
                    uint size = await dataReader.LoadAsync(1); // Read one byte at a time
                    if (size > 0)
                    {
                        byte[] buffer = new byte[size];
                        dataReader.ReadBytes(buffer);
                        char receivedChar = (char)buffer[0];

                        if (receivedChar == '\n')
                        {
                            string command = receivedData.ToString().Trim();
                            receivedData.Clear();

                            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                            {
                                ProcessCommand(command);
                                AddMessage($"Received: {command}");
                            });
                        }
                        else
                        {
                            receivedData.Append(receivedChar);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    AddMessage($"Error: {ex.Message}");
                });
            }
        }

        // Process received command from serial port
        private void ProcessCommand(string command)
        {

        }

        // Add message to the TextBox
        private void AddMessage(string message)
        {
            messageTextBlock.Text += $"{message}\n"; // Append message with newline
        }

        // Event handler for LED-On button click
        private void BtnLedOn_Click(object sender, RoutedEventArgs e)
        {
            // Send 'Y' command to turn LED on
            SendCommand('Y');
        }

        // Event handler for LED-Off button click
        private void BtnLedOff_Click(object sender, RoutedEventArgs e)
        {
            // Send 'N' command to turn LED off
            SendCommand('N');
        }

        // Event handler for Blinky button click
        private void BtnBlinky_Click(object sender, RoutedEventArgs e)
        {
            // Send 'Q' command to start blinking
            SendCommand('Q');
        }

        // Send command over serial port
        private void SendCommand(char command)
        {
            try
            {
                dataWriter.WriteByte((byte)command);
                // Send the command asynchronously
                _ = dataWriter.StoreAsync();
            }
            catch (Exception ex)
            {
                AddMessage($"Error sending command: {ex.Message}");
            }
        }

        // Send message over serial port
        private async void SendMessage(string message)
        {
            try
            {
                dataWriter.WriteString(message);
                // Send the message asynchronously
                await dataWriter.StoreAsync();
                AddMessage($"Sent: {message}");
            }
            catch (Exception ex)
            {
                AddMessage($"Error sending message: {ex.Message}");
            }
        }

        // Play MP3 file
        private async void PlayMP3()
        {
            try
            {
                // Access the MP3 file from your project's Assets folder
                StorageFolder folder = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFolderAsync("Assets");
                StorageFile mp3File = await folder.GetFileAsync("SoundTrack.mp3");

                // Set the MediaElement's source to the MP3 file
                var stream = await mp3File.OpenAsync(FileAccessMode.Read);
                mediaPlayerElement.SetSource(stream, mp3File.ContentType);

                // Start playback
                mediaPlayerElement.Play();
            }
            catch (Exception ex)
            {
                AddMessage($"Error playing MP3: {ex.Message}");
            }
        }

        // Event handler for MP3 player button click
        private void BtnPlayMP3_Click(object sender, RoutedEventArgs e)
        {
            PlayMP3();
        }
    }
}
