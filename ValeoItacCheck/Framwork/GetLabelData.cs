using System;
using System.IO;

namespace ValeoItacCheck
{
    public static class GetLabelData
    {
        public static BinData GetDataForLabel(string snr)
        {
            BinData data = null;

            using (StreamReader file = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + "\\data\\bin.txt"))
            {
                string line;

                while ((line = file.ReadLine()) != null)
                {
                    string[] col = line.Split('|');
                    if (col.Length > 3)
                    {
                        if (col[0] == snr)
                        {
                            data = new BinData()
                            {
                                PCB_SN = col[0],
                                SAP_PN = col[1].TrimStart('0'),
                                VARIANT = col[2],
                                PCB_COEFFICIENT = col[3]
                            };

                            break;
                        }
                    }
                }
            }

            return data;
        }


        public static bool isBinFileAvailable()
        {
            return (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\data\\bin.txt")) ? true : false;
        }
    
    }
}
