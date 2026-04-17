using System;
using Microsoft.EntityFrameworkCore;
using RCS.Data;
using RCS.Data.Entities;
using RCS.Cogo.Wpf.ViewModels;

namespace TestExport
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                var vm = new InstalledAssetsViewModel();
                // We need to inject a test project ID or it won't load anything.
                // Wait, ExportAllToSingleFile doesn't require project ID if it just iterates empty arrays! 
                vm.ExportAllToSingleFile("test_assets.xlsx", "xlsx");
                Console.WriteLine("SUCCESS!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR:");
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
