using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BiomeData;
using UnityEngine;

namespace SaveLoad
{
    public static class SavingScript
    {
        private static byte[] FormatData(Dictionary<string,string> data)
        {
            string collatedData = "";
            
            foreach(KeyValuePair<string,string> subject in data)
            {
                collatedData += subject.Key + ":" + subject.Value + ",\n";
            }

            return Encoding.UTF8.GetBytes(collatedData);
        }

        public static string CreateFile(int worldWidth, int worldHeight) //Creates a new save file structure
        {
            //TODO add new world file names

            //Adds directories
            Directory.CreateDirectory(System.IO.Directory.GetCurrentDirectory().ToString() + "/Saves/World1/");
            Directory.CreateDirectory(System.IO.Directory.GetCurrentDirectory().ToString() + "/Saves/World1/WorldData");

            //Necessary files
            FileStream configFile = File.Create(System.IO.Directory.GetCurrentDirectory().ToString() + "/Saves/World1/WorldConfig.cfg");

            //Adds data to be appended to the configfile
            byte[] configData = FormatData(new Dictionary<string, string> { { "Width", worldWidth.ToString() }, { "Height", worldHeight.ToString() } });
            configFile.Write(configData, 0, configData.Length);

            configFile.Close();
            return System.IO.Directory.GetCurrentDirectory().ToString() + "/Saves/World1/"; //Return file name string
        }

        public static void CreateMap(string filePath, ref byte[] imageBytes) //draws a map to a file
        {
            //Get bytes of map and convert to image
            FileStream mapFile = File.Create(filePath + "WorldData/Map.png");
            mapFile.Write(imageBytes, 0, imageBytes.Length);

            mapFile.Close();
        }
        public static void CreateProvinceMapping(string filePath, ref List<Province> provinces) //draws just the mapping elements of a province
        {
            //Write all province properties to file
            FileStream provFile = File.Create(filePath + "WorldData/ProvMap.dat");

            //TODO
            provFile.Close();
        }
    }
}