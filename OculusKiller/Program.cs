using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Web.Script.Serialization;

namespace OculusKiller
{
    public class Program
    {
        public static void Main()
        {
            try
            {
                string openVrPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"openvr\openvrpaths.vrpath");
                if (File.Exists(openVrPath))
                {
                    var jss = new JavaScriptSerializer();
                    string jsonString = File.ReadAllText(openVrPath);
                    dynamic steamVrPath = jss.DeserializeObject(jsonString);

                    string vrStartupPath = Path.Combine(steamVrPath["runtime"][0].ToString(), @"bin\win64\vrstartup.exe");
                    string lhManagerPath = @"C:\\Program Files\\LHManager\\lighthouse-v2-manager.exe";
                    if (File.Exists(vrStartupPath) && File.Exists(lhManagerPath))
                    {
                        Process lhON = Process.Start(lhManagerPath, @"on E3:E7:D2:B5:9B:EB CB:95:59:69:43:62 FD:F4:68:BA:D3:7A F0:DD:40:1F:01:C4"), lhOFF=null;
                        Process vrStartupProcess = Process.Start(vrStartupPath);
                        lhON.WaitForExit();
                        vrStartupProcess.WaitForExit();
                        lhOFF = Process.Start(lhManagerPath, @"off E3:E7:D2:B5:9B:EB CB:95:59:69:43:62 FD:F4:68:BA:D3:7A F0:DD:40:1F:01:C4");
                        lhOFF.WaitForExit();

                    }
                    else if(File.Exists(vrStartupPath))
                    {
                        Process vrStartupProcess = Process.Start(vrStartupPath);
                        vrStartupProcess.WaitForExit();
                    }
                    else
                        MessageBox.Show("SteamVR does not exist in installation directory.");
                }
                else
                    MessageBox.Show("Could not find openvr config file within LocalAppdata.");
            }
            catch (Exception e)
            {
                MessageBox.Show($"An exception occured while attempting to find SteamVR!\n\nMessage: {e}");
            }
        }
    }
}
