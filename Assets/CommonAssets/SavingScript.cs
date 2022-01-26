using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using BiomeData;
using WorldProperties;
using UnityEngine;

namespace SaveLoad
{
    public static class SavingScript
    {
        public static XmlWriterSettings settings = new XmlWriterSettings();
        static SavingScript() //Set up basic XML data
        {
            settings.Indent = true;
        }

        private static byte[] FormatData(Dictionary<string, string> data)
        {
            string collatedData = "";

            foreach (KeyValuePair<string, string> subject in data)
            {
                collatedData += subject.Key + ":" + subject.Value + ",\n";
            }

            return Encoding.UTF8.GetBytes(collatedData);
        }
        public static Dictionary<string, string> FindLoadables()
        {
            Dictionary<string, string> targetFiles = new Dictionary<string, string>(); //Name/path

            if (!Directory.Exists(System.IO.Directory.GetCurrentDirectory().ToString() + "/Saves/")) { return null; } //Exit if there are no valid targets

            string[] dirs = Directory.GetDirectories(System.IO.Directory.GetCurrentDirectory().ToString() + "/Saves/");

            foreach (string p in dirs)
            {
                if (File.Exists(p + "/World.sav"))
                {
                    string worldName = new DirectoryInfo(p).Name.ToString();
                    if (!targetFiles.ContainsKey(worldName)) //There should only be one world of each type
                    {
                        targetFiles.Add(worldName, p);
                    }
                }
            }

            return targetFiles;
        }
        public static string CreateFile(int worldWidth, int worldHeight) //Creates a new save file structure
        {

            string path;
            int iterate = 0;

            {
                if (!Directory.Exists(System.IO.Directory.GetCurrentDirectory().ToString() + "/Saves/")) //Create a save folder if one does not exist
                {
                    Directory.CreateDirectory(System.IO.Directory.GetCurrentDirectory().ToString() + "/Saves/");
                }


                while (true) //Find the next available world save folder
                {
                    if (!Directory.Exists(System.IO.Directory.GetCurrentDirectory().ToString() + "/Saves/World" + iterate))
                    {
                        path = System.IO.Directory.GetCurrentDirectory().ToString() + "/Saves/World" + iterate + "/";
                        break;
                    }

                    iterate++;
                }

            }

            Directory.CreateDirectory(path + "WorldData/");

            //Write to the xml config file
            XmlWriter xmlWriter = XmlWriter.Create(path + "World.sav", settings);
            xmlWriter.WriteStartDocument();

            xmlWriter.WriteStartElement("World");

            xmlWriter.WriteStartElement("Name");
            xmlWriter.WriteString("World" + iterate);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("Width");
            xmlWriter.WriteString(worldWidth.ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("Height");
            xmlWriter.WriteString(worldHeight.ToString());
            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndElement();

            xmlWriter.WriteEndDocument();
            xmlWriter.Close();

            return path; //Return save file name
        }
        public static Dictionary<string, string> LoadBaseData(string filepath)
        {
            Dictionary<string, string> properties = new Dictionary<string, string>();
            XmlDocument xmlReader = new XmlDocument(); //Open xmlfile
            xmlReader.Load(filepath + "/World.sav");

            XmlNode propNode = xmlReader.SelectSingleNode("World");

            properties.Add("Width", propNode["Width"].InnerText);
            properties.Add("Height", propNode["Height"].InnerText);
            properties.Add("Name", propNode["Name"].InnerText);

            return properties;
        }
        public static void CreateMap(string filePath, ref byte[] imageBytes) //draws a map to a file
        {
            //Get bytes of map and convert to image
            FileStream mapFile = File.Create(filePath + "WorldData/Map.png");
            mapFile.Write(imageBytes, 0, imageBytes.Length);

            mapFile.Close();
        }
        public static byte[] LoadMap(string filepath, int width, int height)
        {
            return File.ReadAllBytes(filepath + "/WorldData/Map.png"); ;
        }
        public static void CreateProvinceMapping(string filePath, ref List<ProvinceObject> provinces) //draws just the mapping elements of a province
        {
            //Write all province properties to an xml file
            XmlWriter xmlWriter = XmlWriter.Create(filePath + "WorldData/Provinces.xml", settings);
            xmlWriter.WriteStartDocument();

            xmlWriter.WriteStartElement("Provinces");
            foreach (ProvinceObject tProv in provinces)
            {
                xmlWriter.WriteStartElement("Province");
                xmlWriter.WriteAttributeString("ID", tProv._id.ToString());
                xmlWriter.WriteAttributeString("City", tProv._cityName);
                xmlWriter.WriteAttributeString("CultureID", tProv._cultureID.ToString());

                xmlWriter.WriteStartElement("Colour");
                xmlWriter.WriteString(ColorUtility.ToHtmlStringRGB(tProv._provCol));
                xmlWriter.WriteEndElement();

                xmlWriter.WriteStartElement("Coastal");
                xmlWriter.WriteString(tProv._isCoastal.ToString());
                xmlWriter.WriteEndElement();

                xmlWriter.WriteStartElement("Population");
                xmlWriter.WriteString(tProv._population.ToString());
                xmlWriter.WriteEndElement();

                xmlWriter.WriteStartElement("Elevation");
                xmlWriter.WriteString(tProv._elProp.ToString());
                xmlWriter.WriteEndElement();

                xmlWriter.WriteStartElement("Temperature");
                xmlWriter.WriteString(tProv._tmpProp.ToString());
                xmlWriter.WriteEndElement();

                xmlWriter.WriteStartElement("Rainfall");
                xmlWriter.WriteString(tProv._rainProp.ToString());
                xmlWriter.WriteEndElement();

                xmlWriter.WriteStartElement("Flora");
                xmlWriter.WriteString(tProv._floraProp.ToString());
                xmlWriter.WriteEndElement();

                xmlWriter.WriteStartElement("Vertices"); //Province vertices
                foreach (Vector3 vec in tProv._vertices)
                {
                    xmlWriter.WriteStartElement("Vertex");
                    xmlWriter.WriteString(vec.x + "," + vec.y);
                    xmlWriter.WriteEndElement();
                }
                xmlWriter.WriteEndElement();

                xmlWriter.WriteStartElement("Adjacents"); //Adjacent prov IDs
                foreach (int adj in tProv._adjacentProvIDs)
                {
                    xmlWriter.WriteStartElement("ID");
                    xmlWriter.WriteString(adj.ToString());
                    xmlWriter.WriteEndElement();
                }
                xmlWriter.WriteEndElement();

                xmlWriter.WriteEndElement();
            }
            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndDocument();
            xmlWriter.Close();
        }

        public static void CreateCultures(string filePath, ref List<Culture> cultures) //draws just the mapping elements of a province
        {
            //Write all province properties to an xml file
            XmlWriter xmlWriter = XmlWriter.Create(filePath + "WorldData/Cultures.xml", settings);
            xmlWriter.WriteStartDocument();

            xmlWriter.WriteStartElement("Cultures");
            foreach (Culture tCult in cultures)
            {
                xmlWriter.WriteStartElement("Culture");
                xmlWriter.WriteAttributeString("ID", tCult._id.ToString());
                xmlWriter.WriteAttributeString("Name", tCult._name.ToString());

                xmlWriter.WriteStartElement("Colour");
                xmlWriter.WriteString(ColorUtility.ToHtmlStringRGB(tCult._cultureCol));
                xmlWriter.WriteEndElement();

                xmlWriter.WriteEndElement();
            }
            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndDocument();
            xmlWriter.Close();
        }
    }
}