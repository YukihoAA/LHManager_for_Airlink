using System;
using System.Diagnostics;
using System.IO;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace OculusKiller
{
    public class Program
    {
        static readonly string[] macList = { @"E3:E7:D2:B5:9B:EB", @"CB:95:59:69:43:62", @"FD:F4:68:BA:D3:7A", @"F0:DD:40:1F:01:C4" };
        static readonly string lhManagerPath = @"C:\\Program Files\\LHManager\\lighthouse-v2-manager.exe";
        static readonly bool lhAvailable = File.Exists(lhManagerPath) && macList.Length > 0;
        public static void Main()
        {
            try
            {
                string oculusPath = GetOculusPath();
                var result = GetSteamPaths();
                if (result == null || String.IsNullOrEmpty(oculusPath))
                {
                    return;
                }
                string startupPath = result.Item1;
                string vrServerPath = result.Item2;
                string oculusDashPath = oculusPath.Replace(@"oculus-runtime\OVRServer_x64", @"oculus-dash\dash\bin\OculusDash1");

                if (!File.Exists(oculusDashPath))
                {
                    MessageBox.Show("Oculus dash executable not found...");
                    return;
                }

                // prevent from duplicate excute
                if (Process.GetProcessesByName("OculusDash").Length > 1 || Process.GetProcessesByName("OculusDash1").Length > 1 || Process.GetProcessesByName("lighthouse-v2-manager").Length >= 1)
                {
                    System.Threading.Thread.Sleep(2000);
                    return;
                }

                Process dashPS = Process.Start(oculusDashPath);

                LightHouseToggle("on");

                Stopwatch sw = Stopwatch.StartNew();
                while (true)
                {
                    if (sw.ElapsedMilliseconds >= 100000)
                    {
                        MessageBox.Show("SteamVR vrserver not found... (Did you lunched SteamVR?)");
                        return;
                    }

                    // Don't give the user an error if the process isn't found, it happens often...
                    Process vrServerProcess = Array.Find(Process.GetProcessesByName("vrmonitor"), process => process.MainModule.FileName == vrServerPath);
                    if (vrServerProcess == null)
                    {
                        System.Threading.Thread.Sleep(200);
                        continue;
                    }
                    else
                        sw.Stop();
                    vrServerProcess.WaitForExit();

                    Process[] vrcftProcess = Process.GetProcessesByName("VRCFaceTracking");
                    if (vrcftProcess != null && vrcftProcess.Length > 0)
                    {
                        vrcftProcess[0].CloseMainWindow();
                        vrcftProcess[0].WaitForExit();
                    }
                    LightHouseToggle("off");
                    dashPS.WaitForExit();
                    System.Threading.Thread.Sleep(15000);
                    return;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"An exception occured while attempting to find/start SteamVR...\n\nMessage: {e}");
            }
        }

        static void LightHouseToggle(String command)
        {
            Process[] ps = new Process[macList.Length];

            if (!(command == "on" || command == "off"))
                return;

            if (lhAvailable)
            {
                for (int i = 0; i < macList.Length; i++)
                {
                    ps[i] = Process.Start(lhManagerPath, command + @" " + macList[i]);

                }
            }

            foreach (Process p in ps)
            {
                p?.WaitForExit();
            }
        }

        static string GetOculusPath()
        {
            string oculusPath = Environment.GetEnvironmentVariable("OculusBase");
            if (string.IsNullOrEmpty(oculusPath))
            {
                MessageBox.Show("Oculus installation environment not found...");
                return null;
            }

            oculusPath = Path.Combine(oculusPath, @"Support\oculus-runtime\OVRServer_x64.exe");
            if (!File.Exists(oculusPath))
            {
                MessageBox.Show("Oculus server executable not found...");
                return null;
            }

            return oculusPath;
        }

        public static Tuple<string, string> GetSteamPaths()
        {
            string openVrPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"openvr\openvrpaths.vrpath");
            if (!File.Exists(openVrPath))
            {
                MessageBox.Show("OpenVR Paths file not found... (Has SteamVR been run once?)");
                return null;
            }

            try
            {
                JavaScriptSerializer jss = new JavaScriptSerializer();
                string openvrJsonString = File.ReadAllText(openVrPath);
                dynamic openvrPaths = jss.DeserializeObject(openvrJsonString);

                string location = openvrPaths["runtime"][0].ToString();
                string startupPath = Path.Combine(location, @"bin\win64\vrstartup.exe");
                string serverPath = Path.Combine(location, @"bin\win64\vrmonitor.exe");

                if (!File.Exists(startupPath))
                {
                    MessageBox.Show("SteamVR startup executable does not exist... (Has SteamVR been run once?)");
                    return null;
                }
                if (!File.Exists(serverPath))
                {
                    MessageBox.Show("SteamVR server executable does not exist... (Has SteamVR been run once?)");
                    return null;
                }

                return new Tuple<string, string>(startupPath, serverPath);
            }
            catch (Exception e)
            {
                MessageBox.Show($"Corrupt OpenVR Paths file found... (Has SteamVR been run once?)\n\nMessage: {e}");
            }
            return null;
        }
    }
}
