using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

class ConvertPngToIco
{
    static void Main(string[] args)
    {
        string pngPath = @"c:\_Qsync\PrimaKurzy\aplikacni-portal\migration_test\ikona\logo_cmi.png";
        string icoPath = @"c:\_Qsync\PrimaKurzy\aplikacni-portal\migration_test\CMILauncher\Resources\icon.ico";
        
        using (var img = Image.FromFile(pngPath))
        using (var bmp = new Bitmap(img, 256, 256))
        {
            using (var fs = new FileStream(icoPath, FileMode.Create))
            using (var icon = Icon.FromHandle(bmp.GetHicon()))
            {
                icon.Save(fs);
            }
        }
        
        Console.WriteLine("✓ ICO created: " + icoPath);
        Console.WriteLine("Size: " + new FileInfo(icoPath).Length + " bytes");
    }
}
