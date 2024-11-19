using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ValeoItacCheck
{
    public static class CsvHelper
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public static void WriteToCsv(List<PanelPositions> panelPositions)
        {
            string currentDate   = DateTime.Now.ToString("yyyy-MM-dd");
            string directoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "InventoryData");
            string filePath      = Path.Combine(directoryPath, $"InventoryData_{currentDate}.txt");

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            var csv = new StringBuilder();

            if (!File.Exists(filePath))
            {
                // Write the header only if the file is newly created
                csv.AppendLine($"{AppSettings.CONTAINER_NAME},Serial_Number,Part_Number,SAP_PN,PCB_Coefficient");
            }

            // Write the data
            foreach (var item in panelPositions)
            {
                // skip SN that has no SAP info
                if(!string.IsNullOrEmpty(item.SapPn) && !string.IsNullOrEmpty(item.PcbCoef))
                {
                    csv.AppendLine($"{item.Container},{item.SerialNumber},{item.PartNumber},{item.SapPn},{item.PcbCoef}");
                }
            }

            try
            {
                // Append data to the file (create if it doesn't exist)
                File.AppendAllText(filePath, csv.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to write CSV: {ex.Message}");
            }
        }

    }
}
