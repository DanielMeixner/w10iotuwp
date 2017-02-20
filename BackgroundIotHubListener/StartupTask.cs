using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.ApplicationModel.Background;
using Microsoft.Azure.Devices;
using System.Threading.Tasks;
using Windows.ApplicationModel;

using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.Networking;
using Windows.Networking.BackgroundTransfer;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.System.Profile;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.Foundation;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace BackgroundIotHubListener
{
    public sealed class StartupTask : IBackgroundTask
    {
        public void Run(IBackgroundTaskInstance taskInstance)
        {

            deferral = taskInstance.GetDeferral();
            InitConfiguration();

            RegisterDeviceOnIoTHub();

            // will never be called, runs forever
            //deferral.Complete();
            
            
            // 
            // TODO: Insert code to perform background work
            //
            // If you start any asynchronous methods here, prevent the task
            // from closing prematurely by using BackgroundTaskDeferral as
            // described in http://aka.ms/backgroundtaskdeferral
            //
        }


        static RegistryManager _registryManager;

        // best way to provide your connection string: duplicate resourcesFileSample to a file called resourcesFile and enter your connections String. 
        // Don't add it to source control!
        static string _connectionString = String.Empty;

        string _deviceId = "myFirstDevice";
        Device _device = null;
        Task _receivingTask;
        DeviceClient _deviceClient;
        Uri _url = new Uri("http://danielmeixner.de/666/W10IoTapp.zip");
        private BackgroundTaskDeferral deferral;

        //public MainPage()
        //{
        //    this.InitializeComponent();
        //    InitConfiguration();

        //    RegisterDeviceOnIoTHub();

        //    ShowAppVersion();
        //}



        private static void InitConfiguration()
        {
            var resources = new Windows.ApplicationModel.Resources.ResourceLoader("resourcesFile");
            _connectionString = resources.GetString("IotHubConnectionString");

            Package package = Package.Current;
            PackageId packageId = package.Id;
            var familyName = packageId.FamilyName;

            var msg = "To get things going run the following command on a remote powershell on your device:";
            string cmd = $@"REG ADD ""HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\EmbeddedMode\ProcessLauncher"" /v AllowedExecutableFilesList /t REG_MULTI_SZ /d ""c:\windows\system32\applyupdate.exe\0c:\windows\system32\deployappx.exe\0c:\installer\appinstall.cmd\0c:\Data\Users\DefaultAccount\AppData\Local\Packages\{familyName}\LocalState\installer\AppInstall\appinstall.cmd\0""";

            Debug.WriteLine(msg);
            Debug.WriteLine(cmd);
        }



        private async void RegisterDeviceOnIoTHub()
        {
            _deviceId = GetUniqueDeviceId();

            // add to Iot Hub or get Device Info
            _device = await SignUpDeviceOnIotHub(_deviceId);

            //start Listening for Update Triggers
            ListenForUpdates(_device);

        }

        private void ListenForUpdates(Device _device)
        {
            ConnectToIoTSuite(_device);
        }
        private async void ConnectToIoTSuite(Device dev)
        {

            var deviceConnectionString = $"{ _connectionString};DeviceId={_device.Id}";

            try
            {
                _deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, Microsoft.Azure.Devices.Client.TransportType.Amqp);
                await _deviceClient.OpenAsync();
                //WriteToDebugBoxAsync("Device Client Opened");

                _receivingTask = Task.Run(() => ReceiveDataFromAzure());
            }
            catch
            {
                //WriteToDebugBoxAsync("EXCEPTION ConnectToIoTSITE");
                Debug.Write("Error while trying to connect to IoT Hub");
                //deviceClient = null;
            }
        }

        private async void ReceiveDataFromAzure()
        {
            while (true)
            {
                var message = await _deviceClient.ReceiveAsync();
                if (message != null)
                {
                    //WriteToDebugBoxAsync("Received Data from Azure ");
                    try
                    {

                        //dynamic command = DeSerialize(message.GetBytes());
                        //if (command.Name == "TriggerAlarm")
                        //{
                        //    // Received a new message, display it
                        //await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                        //async () =>
                        //{
                        //    await TriggerUpdateAsync();
                        //});


                        TriggerUpdateAsync();


                        // We received the message, indicate IoTHub we treated it
                        await _deviceClient.CompleteAsync(message);
                        // WriteToDebugBoxAsync("Sent Azure completed status");
                        //}
                    }
                    catch
                    {
                        await _deviceClient.RejectAsync(message);
                    }
                }
            }
        }

        private async Task TriggerUpdateAsync()
        {
            await RunAppUpdateAsync(_url);

        }

        // get unique Device name
        private static string GetUniqueDeviceId()
        {
            var hostNames = NetworkInformation.GetHostNames();
            var hostName = hostNames.FirstOrDefault(name => name.Type == HostNameType.DomainName)?.DisplayName ?? "???";

            var hwToken = HardwareIdentification.GetPackageSpecificToken(null);
            var hwTokenId = hwToken.Id;
            var dataReader = Windows.Storage.Streams.DataReader.FromBuffer(hwTokenId);
            byte[] bytes = new byte[hwTokenId.Length];
            dataReader.ReadBytes(bytes);
            var id = BitConverter.ToString(bytes).Replace("-", "").Substring(25);

            var deviceId = $"BG_{hostName}_{id}";
            return deviceId;
        }

        private static async Task<Device> SignUpDeviceOnIotHub(string deviceId)
        {
            Device device;
            _registryManager = RegistryManager.CreateFromConnectionString(_connectionString);

            try
            {
                device = await _registryManager.AddDeviceAsync(new Device(deviceId));
            }
            catch (DeviceAlreadyExistsException e)
            {
                device = await _registryManager.GetDeviceAsync(deviceId);

            }


            Debug.WriteLine("Generated device key: {0}", device.Authentication.SymmetricKey.PrimaryKey);
            return device;
        }





        public IAsyncAction RunAppUpdateAsync(Uri url)
        {
            return Task.Run(async () =>
            {
                try
                {
                    Uri source = url;
                    StorageFile destinationFile = await ApplicationData.Current.LocalFolder.CreateFileAsync("download.zip", CreationCollisionOption.GenerateUniqueName);
                    BackgroundDownloader downloader = new BackgroundDownloader();
                    DownloadOperation download = downloader.CreateDownload(source, destinationFile);
                    Debug.WriteLine("Downloading " + url);
                    await download.StartAsync();
                    Debug.WriteLine("Download completed");
                    await UnzipFile(download.ResultFile.Path);
                    Debug.WriteLine("Unzip completed");


                    //// REG ADD "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\EmbeddedMode\ProcessLauncher" /v AllowedExecutableFilesList /t REG_MULTI_SZ /d "c:\windows\system32\applyupdate.exe\0c:\windows\system32\deployappx.exe\0c:\installer\appinstall.cmd\0c:\Data\Users\DefaultAccount\AppData\Local\Packages\15c8ba7d-b8cc-46ee-84f1-ef0f27753fbe_0wy2ejr5nfw9j\LocalState\installer\AppInstall\appinstall.cmd\0"

                    StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                    StorageFolder t = null;
                    try
                    {
                        t = await localFolder.GetFolderAsync("installer");
                    }
                    catch
                    {
                        t = null;
                    }
                    if (t != null)
                    {
                        await t.DeleteAsync();
                    }
                    StorageFolder f = await localFolder.GetFolderAsync("update.main");
                    await f.RenameAsync("installer");
                    string path = localFolder.Path + "\\installer\\AppInstall\\appinstall.cmd";
                    string s = "";
                    await RunMyProcess(path, s);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Exception in STartDownload()");
                }


            }).AsAsyncAction();


        }

        private async Task UnzipFile(string path)
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            ZipFile.ExtractToDirectory(path, localFolder.Path);
        }

        public IAsyncAction RunMyProcess(string cmd, string args)
        {
            return Task.Run(async () =>
            {
                var options = new ProcessLauncherOptions();
                var standardOutput = new InMemoryRandomAccessStream();
                var standardError = new InMemoryRandomAccessStream();
                options.StandardOutput = standardOutput;
                options.StandardError = standardError;
                Debug.WriteLine("Run command " + cmd);
                //await CoreWindow.GetForCurrentThread().Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                //{
                    try
                    {
                        var result = await ProcessLauncher.RunToCompletionAsync(cmd, args == null ? string.Empty : args, options);

                        //ProcessExitCode.Text += "Process Exit Code: " + result.ExitCode;

                        using (var outStreamRedirect = standardOutput.GetInputStreamAt(0))
                        {
                            var size = standardOutput.Size;
                            using (var dataReader = new DataReader(outStreamRedirect))
                            {
                                var bytesLoaded = await dataReader.LoadAsync((uint)size);
                                var stringRead = dataReader.ReadString(bytesLoaded);
                                //StdOutputText.Text += stringRead;
                            }
                        }

                        using (var errStreamRedirect = standardError.GetInputStreamAt(0))
                        {
                            using (var dataReader = new DataReader(errStreamRedirect))
                            {
                                var size = standardError.Size;
                                var bytesLoaded = await dataReader.LoadAsync((uint)size);
                                var stringRead = dataReader.ReadString(bytesLoaded);
                                //StdErrorText.Text += stringRead;
                            }
                        }
                    }
                    catch (UnauthorizedAccessException uex)
                    {
                    }
                    catch (Exception ex)
                    {
                    }
                //});

            }).AsAsyncAction();

        }
    }

}