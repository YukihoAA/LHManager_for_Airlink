using System;
using System.Diagnostics;
using System.IO;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace OculusKiller
{
    public class Program
    {
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
                string lhManagerPath = @"C:\\Program Files\\LHManager\\lighthouse-v2-manager.exe";
                string[] macList = { @"E3:E7:D2:B5:9B:EB", @"CB:95:59:69:43:62", @"FD:F4:68:BA:D3:7A", @"F0:DD:40:1F:01:C4" };
                bool lhAvailable = File.Exists(lhManagerPath) && macList.Length > 0;


                Process[] ps = new Process[macList.Length];
                if (lhAvailable)
                {
                    for (int i = 0; i < macList.Length; i++)
                    {
                        ps[i] = Process.Start(lhManagerPath, @"on " + macList[i]);

                    }
                }

                Process.Start(startupPath).WaitForExit();

                foreach (Process p in ps)
                {
                    p?.WaitForExit();
                }

                Stopwatch sw = Stopwatch.StartNew();
                while (true)
                {
                    if (sw.ElapsedMilliseconds >= 10000)
                    {
                        MessageBox.Show("SteamVR vrserver not found... (Did SteamVR crash?)");
                        return;
                    }

                    // Don't give the user an error if the process isn't found, it happens often...
                    Process vrServerProcess = Array.Find(Process.GetProcessesByName("vrserver"), process => process.MainModule.FileName == vrServerPath);
                    if (vrServerProcess == null)
                        continue;
                    vrServerProcess.WaitForExit();

                    if (lhAvailable)
                    {
                        for (int i = 0; i < macList.Length; i++)
                        {
                            ps[i] = Process.Start(lhManagerPath, @"off " + macList[i]);

                        }
                    }

                    // No-one would ever use the name "OVRServer_x64" but let's just be safe...
                    Process ovrServerProcess = Array.Find(Process.GetProcessesByName("OVRServer_x64"), process => process.MainModule.FileName == oculusPath);
                    if (ovrServerProcess == null)
                    {
                        MessageBox.Show("Oculus runtime not found...");
                        return;
                    }

                    ovrServerProcess.Kill();
                    ovrServerProcess.WaitForExit();

                    foreach (Process p in ps)
                    {
                        p?.WaitForExit();
                    }
                    break;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"An exception occured while attempting to find/start SteamVR...\n\nMessage: {e}");
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
                string serverPath = Path.Combine(location, @"bin\win64\vrserver.exe");

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
