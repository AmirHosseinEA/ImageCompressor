using System.Diagnostics;

public class PdfCompressor
{
    public static void CompressPdf(string inputFilePath, string outputFilePath)
    {
        string gsPath = @"C:\Program Files\gs\gs10.05.0\bin\gswin64c.exe";

        string args = $"-sDEVICE=pdfwrite -dCompatibilityLevel=1.4 -dPDFSETTINGS=/ebook " +
                      $"-dNOPAUSE -dQUIET -dBATCH -sOutputFile=\"{outputFilePath}\" \"{inputFilePath}\"";

        Process process = new Process();
        process.StartInfo.FileName = gsPath;
        process.StartInfo.Arguments = args;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.Start();
        process.WaitForExit();
    }
}
