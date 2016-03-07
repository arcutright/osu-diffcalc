using System;
using System.Windows.Forms;

namespace Osu_DiffCalc.UserInterface
{
    class UX
    {
        public static string getFilenameFromDialog()
        {
            string filename = null;
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = "Open Osu Beatmap File";
            dialog.Filter = "OSU files|*.osu";
            dialog.InitialDirectory = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), @"osu file examples");
            dialog.Multiselect = false;
            try
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                    filename = dialog.FileName;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.GetBaseException());
            }
            dialog.Dispose();

            return filename;
        }

        public static string getFilenameFromDialog(GUI gui)
        {
            string filename = null;
            gui.Invoke((MethodInvoker)delegate
            {
                filename = getFilenameFromDialog();
            });
            return filename;
        }
    }
}
