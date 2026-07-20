using System;
using System.IO;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        string path = @"tests\E_POS.IntegrationTests\POSOperations\PosReturnRepositoryTests.cs";
        string content = File.ReadAllText(path);

        // Fix CreateCompletedPosSale
        content = Regex.Replace(content, @"(null,\s*""LKR"",\s*)(\d+m)", "$1false, $2");

        // Fix CreateForPosSale first part
        content = Regex.Replace(content, @"(null,\s*""[^""]+"",\s*)("".*?"",\s*.*?,\s*)(""EA"")", "$1null, $2null, null, null, null, $3");

        // Fix CreateForPosSale second part
        content = Regex.Replace(content, @"(\d+m,\s*)(Now\)\);)", "$1false, $2");
        
        File.WriteAllText(path, content);
        Console.WriteLine("Done");
    }
}
