using System;
using System.IO;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        var text = File.ReadAllText("src/E_POS.Infrastructure/Modules/Tenant/Reports/Repositories/TenantAdminReportsRepository.cs");
        var matches = Regex.Matches(text, @"Row\(\((.*?)\)\)");
        foreach(Match match in matches)
        {
            // not very reliable
        }
    }
}
