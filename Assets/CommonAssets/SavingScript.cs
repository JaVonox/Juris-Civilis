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

        public static void CreateMap(string filePath, ref List<Province> provSet) //draws a map to a file
        {
            //Province save syntax = ID : {x,y,biomeID} etc. \n

            FileStream mapFile = File.Create(filePath + "WorldData/Map.sav");

            int provsDone = 0;
            //Iterate through all provinces
            foreach(Province prov in provSet) //Iterate through all provinces to draw a map data list
            {
                List<byte> mapBytes = new List<byte>();
                string tileString = "";

                foreach (Chunk ch in prov._componentChunks.Values)
                {

                    foreach((int x, int y, TileData tile) component in ch.chunkTiles)
                    {
                        tileString += "{" + component.x.ToString() + "," + component.y.ToString() + "," + component.tile._biomeType.ToString() + "}";
                    }

                }

                mapBytes.AddRange(FormatData(new Dictionary<string, string> { { provSet.IndexOf(prov).ToString(), tileString } })); //Appends the tiles data to the set of bytes - with the province ID included
                provsDone++;
                byte[] tmpMap = mapBytes.ToArray();
                mapFile.Write(tmpMap, 0, tmpMap.Length);

                if(provsDone % 100 == 0)
                {
                    Debug.Log("Saved : " + provsDone + " Of " + provSet.Count);
                }
            }

            mapFile.Close();
            Debug.Log("Got all " + provSet.Count);
        }
    }
}