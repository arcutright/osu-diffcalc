using Osu_DiffCalc.FileFinder;
using Osu_DiffCalc.FileProcessor;
using Osu_DiffCalc.UserInterface;
using System;
using System.Runtime.InteropServices;
using System.Timers;
using System.Windows.Forms;

namespace Osu_DiffCalc
{
    class Program
    {
        //the STAThread is needed to call Ux.getFileFromDialog()->.openFileDialog()
        [STAThread]
        static void Main(string[] args)
        {
            if (System.Threading.Thread.CurrentThread.Name == null)
                System.Threading.Thread.CurrentThread.Name = "Main.Thread";
            //use argument as initialization point for map analysis
            //check if argument is a valid path to .osu file or directory containing osu files
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new GUI());


            //Finder.debugAllProcesses();


            /*
            Beatmap test = new Beatmap(Ux.getFilenameFromDialog());
            MapsetManager.analyzeMap(test, false);
            test.printDebug();
            */
            //MapsetManager.analyzeCurrentMapset();
            try
            {
                Console.WriteLine("-----------program finished-----------");
                Console.ReadKey();
            }
            catch { }
        }

        
    }
}
