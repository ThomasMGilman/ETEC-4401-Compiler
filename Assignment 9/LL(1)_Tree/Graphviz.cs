using System;
using System.IO;
using System.Drawing;

public static class FileDotEngine
{
    public static Bitmap Run(string dot)
    {
        string executable = @".\external\dot.exe";
        string output = @".\external\tempgraph";
        File.WriteAllText(output, dot);

        System.Diagnostics.Process process = new System.Diagnostics.Process();

        // Stop the process from opening a new window
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;

        // Setup executable and parameters
        process.StartInfo.FileName = executable;
        process.StartInfo.Arguments = string.Format(@"{0} -Tjpg -O", output);

        // Go
        process.Start();
        // and wait dot.exe to complete and exit
        process.WaitForExit();
        Bitmap bitmap = null; ;
        using (Stream bmpStream = System.IO.File.Open(output + ".jpg", System.IO.FileMode.Open))
        {
            Image image = Image.FromStream(bmpStream);
            bitmap = new Bitmap(image);
        }
        File.Delete(output);
        File.Delete(output + ".jpg");
        return bitmap;
    }
}