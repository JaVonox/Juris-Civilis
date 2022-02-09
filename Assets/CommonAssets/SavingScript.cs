using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Linq;
using BiomeData;
using WorldProperties;
using UnityEngine;
using Empires;

namespace SaveLoad
{
    public static class SavingScript
    {
        public static XmlWriterSettings settings = new XmlWriterSettings();
        static SavingScript() //Set up basic XML data
        {
            settings.Indent = true;
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
        public static string CreateFile(int worldWidth, int worldHeight, bool overwrite, string prePath, int day, int month, int year) //Creates a new save file structure
        {

            string path;
            int iterate = 0;

            {
                if (!Directory.Exists(System.IO.Directory.GetCurrentDirectory().ToString() + "/Saves/")) //Create a save folder if one does not exist
                {
                    Directory.CreateDirectory(System.IO.Directory.GetCurrentDirectory().ToString() + "/Saves/");
                }

                if (!overwrite)
                {
                    while (true) //Find the next available world save folder
                    {
                        if (!Directory.Exists(System.IO.Directory.GetCurrentDirectory().ToString() + "/Saves/World" + iterate))
                        {
                            path = System.IO.Directory.GetCurrentDirectory().ToString() + "/Saves/World" + iterate + "/";
                            Directory.CreateDirectory(path + "WorldData/");
                            break;
                        }

                        iterate++;
                    }
                }
                else
                {
                    XmlDocument empFile = new XmlDocument();
                    path = prePath + "/";
                    empFile.Load(path + "/World.sav");
                    empFile.DocumentElement.RemoveAll(); //Clears the file
                    empFile.Save(path + "/World.sav");
                }

            }

            //Write to the xml config file
            XmlWriter xmlWriter = XmlWriter.Create(path + "/World.sav", settings);
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

            xmlWriter.WriteStartElement("Date");
            xmlWriter.WriteAttributeString("Year", year.ToString());
            xmlWriter.WriteAttributeString("Month", month.ToString());
            xmlWriter.WriteAttributeString("Day", day.ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteEndElement();

            xmlWriter.WriteEndDocument();
            xmlWriter.Close();

            if (!overwrite)
            {
                Directory.CreateDirectory(path + "Simulation/");
                XmlWriter simData = XmlWriter.Create(path + "Simulation/Empires.xml", settings); //This will store the simulated empires data, but is left blank at this stage.

                simData.WriteStartDocument();
                simData.WriteStartElement("Empires");
                simData.WriteEndDocument();
                simData.Close();

                XmlWriter culData = XmlWriter.Create(path + "WorldData/Cultures.xml", settings); //Makes blank culture doc
                culData.WriteStartDocument();
                culData.WriteStartElement("Cultures");
                culData.WriteEndDocument();
                culData.Close();
            }
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
            properties.Add("Year", propNode["Date"].Attributes["Year"].Value);
            properties.Add("Month", propNode["Date"].Attributes["Month"].Value);
            properties.Add("Day", propNode["Date"].Attributes["Day"].Value);

            return properties;
        }
        public static void CreateMap(string filePath, ref byte[] imageBytes, ref byte[] maskBytes) //draws a map to a file
        {
            //Get bytes of map and convert to image
            FileStream mapFile = File.Create(filePath + "WorldData/Map.png");
            mapFile.Write(imageBytes, 0, imageBytes.Length);

            mapFile.Close();

            FileStream maskFile = File.Create(filePath + "WorldData/Mask.png");
            maskFile.Write(maskBytes, 0, maskBytes.Length);

            maskFile.Close();
        }
        public static (byte[],byte[]) LoadMap(string filepath, int width, int height)
        {
            byte[] map = File.ReadAllBytes(filepath + "/WorldData/Map.png");
            byte[] mask = File.ReadAllBytes(filepath + "/WorldData/Mask.png");
            return (map, mask);

        }
        public static void CreateProvinceMapping(string filePath, ref List<ProvinceObject> provinces) //draws province elements to save file
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

                xmlWriter.WriteStartElement("BiomeID");
                xmlWriter.WriteString(tProv._biome.ToString());
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

                xmlWriter.WriteStartElement("Owner");
                if (tProv._ownerEmpire != null)
                {
                    xmlWriter.WriteString(tProv._ownerEmpire._id.ToString());
                }
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

        public static void LoadProvinces(string filepath, ref List<ProvinceObject> outputProvs, ref List<Empire> empires)
        {
            outputProvs.Clear();

            XmlDocument xmlReader = new XmlDocument(); //Open xmlfile
            xmlReader.Load(filepath + "/WorldData/Provinces.xml");

            XmlNode provinceNodes = xmlReader.SelectSingleNode("Provinces");

            foreach(XmlNode provNode in provinceNodes.ChildNodes)
            {
                //Xml file is by ID so add should order correctly
                ProvinceObject loadedProv = new ProvinceObject();
                loadedProv._id = Convert.ToInt32(provNode.Attributes["ID"].Value);
                loadedProv._cityName = provNode.Attributes["City"].Value;
                loadedProv._cultureID = Convert.ToInt32(provNode.Attributes["CultureID"].Value);

                ColorUtility.TryParseHtmlString("#" + provNode["Colour"].InnerText, out loadedProv._provCol); //Sets colour via hex code
                loadedProv._isCoastal = provNode["Coastal"].InnerText == "True" ? true : false;

                loadedProv._population = (Property)Enum.Parse(typeof(Property), provNode["Population"].InnerText);
                loadedProv._elProp = (Property)Enum.Parse(typeof(Property), provNode["Elevation"].InnerText);
                loadedProv._tmpProp = (Property)Enum.Parse(typeof(Property), provNode["Temperature"].InnerText);
                loadedProv._rainProp = (Property)Enum.Parse(typeof(Property), provNode["Rainfall"].InnerText);
                loadedProv._floraProp = (Property)Enum.Parse(typeof(Property), provNode["Flora"].InnerText);

                if(provNode["Owner"].InnerText != "")
                {
                    loadedProv._ownerEmpire = empires[Convert.ToInt32(provNode["Owner"].InnerText)];
                }
                else
                {
                    loadedProv._ownerEmpire = null;
                }
                loadedProv._biome = Convert.ToInt32(provNode["BiomeID"].InnerText);

                foreach (XmlNode verts in provNode["Vertices"].ChildNodes)
                {
                    string[] vertSet = verts.InnerText.Split(',');
                    loadedProv._vertices.Add(new Vector3(Convert.ToInt32(vertSet[0]), Convert.ToInt32(vertSet[1]), -2)); //add vert
                }

                foreach (XmlNode adjs in provNode["Adjacents"].ChildNodes)
                {
                    loadedProv._adjacentProvIDs.Add(Convert.ToInt32(adjs.InnerText));
                }

                outputProvs.Add(loadedProv);
            }

        }
        public static void CreateCultures(string filePath, ref List<Culture> cultures, bool isUpdate) //draws just the mapping elements of a province
        {
            if(isUpdate)
            {
                XmlDocument empFile = new XmlDocument();
                empFile.Load(filePath + "/WorldData/Cultures.xml");
                empFile.DocumentElement.RemoveAll();
                empFile.Save(filePath + "/WorldData/Cultures.xml");
            }

            //Write all province properties to an xml file
            XmlWriter xmlWriter = XmlWriter.Create(filePath + "/WorldData/Cultures.xml", settings);
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

                xmlWriter.WriteStartElement("Economy");
                xmlWriter.WriteString(tCult._economyScore.ToString());
                xmlWriter.WriteEndElement();

                xmlWriter.WriteEndElement();
            }
            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndDocument();
            xmlWriter.Close();
        }

        public static void LoadCultures(string filepath, ref List<Culture> outCulture)
        {
            outCulture.Clear();

            XmlDocument xmlReader = new XmlDocument(); //Open xmlfile
            xmlReader.Load(filepath + "/WorldData/Cultures.xml");

            XmlNode cultNodes = xmlReader.SelectSingleNode("Cultures");

            foreach (XmlNode cultNode in cultNodes.ChildNodes)
            {
                //Xml file is by ID so add should order correctly
                Culture loadedCult = new Culture();
                loadedCult._id = cultNode.Attributes["ID"].Value;
                loadedCult._name = cultNode.Attributes["Name"].Value;
                loadedCult._economyScore = (float)(Convert.ToDouble(cultNode["Economy"].InnerText));
                ColorUtility.TryParseHtmlString("#" + cultNode["Colour"].InnerText, out loadedCult._cultureCol); //Sets colour via hex code

                outCulture.Add(loadedCult);
            }
        }

        public static void SaveEmpires(string filepath, ref List<Empire> emp, ref List<ProvinceObject> provs)
        {
            //Writing to the empire file requires clearing of empire file and recreation
            //Provs also need to be written to, in order to store prov data

            {
                XmlDocument empFile = new XmlDocument();
                empFile.Load(filepath + "/Simulation/Empires.xml");
                empFile.DocumentElement.RemoveAll(); //Clears the file
                empFile.Save(filepath + "/Simulation/Empires.xml");
            }

            XmlWriter empData = XmlWriter.Create(filepath + "/Simulation/Empires.xml", settings); //Begin writing onto empires doc

            empData.WriteStartElement("Empires");
            foreach (Empire tEmpire in emp) //Write in all the empire data
            {
                empData.WriteStartElement("Empire");
                empData.WriteAttributeString("ID", tEmpire._id.ToString());
                empData.WriteAttributeString("Name", tEmpire._empireName.ToString());

                empData.WriteStartElement("Colour");
                empData.WriteString(ColorUtility.ToHtmlStringRGB(tEmpire._empireCol));
                empData.WriteEndElement();

                empData.WriteStartElement("CultureID");
                empData.WriteString(tEmpire._cultureID.ToString());
                empData.WriteEndElement();

                empData.WriteStartElement("MilitarySize");
                empData.WriteString(tEmpire.curMil.ToString());
                empData.WriteEndElement();

                empData.WriteStartElement("MaxMil");
                empData.WriteString(tEmpire.maxMil.ToString());
                empData.WriteEndElement();

                empData.WriteStartElement("ReligionID");
                empData.WriteString(tEmpire.religionID.ToString());
                empData.WriteEndElement();

                empData.WriteStartElement("PercentageEco");
                empData.WriteString(tEmpire.percentageEco.ToString());
                empData.WriteEndElement();

                empData.WriteStartElement("Components"); //Component provs
                foreach (int compProv in tEmpire._componentProvinceIDs)
                {
                    empData.WriteStartElement("ProvinceID");
                    empData.WriteString(compProv.ToString());
                    empData.WriteEndElement();
                }
                empData.WriteEndElement();

                empData.WriteStartElement("Technology"); //Tech scores

                empData.WriteStartElement("Military");
                empData.WriteString(tEmpire.milTech.ToString());
                empData.WriteEndElement();

                empData.WriteStartElement("Economic");
                empData.WriteString(tEmpire.ecoTech.ToString());
                empData.WriteEndElement();

                empData.WriteStartElement("Diplomacy");
                empData.WriteString(tEmpire.dipTech.ToString());
                empData.WriteEndElement();

                empData.WriteStartElement("Logistics");
                empData.WriteString(tEmpire.logTech.ToString());
                empData.WriteEndElement();

                empData.WriteStartElement("Culture");
                empData.WriteString(tEmpire.culTech.ToString());
                empData.WriteEndElement();

                empData.WriteEndElement();

                //TODO add ruler data
                empData.WriteEndElement();
            }
            empData.WriteEndElement();
            empData.Close();


            //Loading of provinces to change empire data where applicable
            XmlDocument provFile = new XmlDocument();
            provFile.Load(filepath + "/WorldData/Provinces.xml");

            List<ProvinceObject> provsToAmmend = provs.Where(p => p._ownerEmpire != null).ToList(); //Get all provs that are owned - these are the only data points that need ammending
            //TODO this assumes land cannot become unpopulated. If this changes change this

            foreach(ProvinceObject tProv in provsToAmmend)
            {
                XmlNode provNode = provFile.SelectSingleNode("/Provinces/Province[@ID='" + tProv._id.ToString() + "']"); //Get the node with the appropriate ID
                provNode["Owner"].InnerText = tProv._ownerEmpire._id.ToString();
            }

            provFile.Save(filepath + "/WorldData/Provinces.xml");

        }

        public static void LoadEmpires(string filepath, ref List<Empire> outEmpires)
        {
            outEmpires.Clear();

            XmlDocument xmlReader = new XmlDocument(); //Open xmlfile
            xmlReader.Load(filepath + "/Simulation/Empires.xml");

            XmlNode empireNodes = xmlReader.SelectSingleNode("Empires");

            foreach (XmlNode empNode in empireNodes.ChildNodes)
            {
                //Xml file is by ID so add should order correctly
                Empire loadedEmp = new Empire();
                loadedEmp._id = Convert.ToInt32(empNode.Attributes["ID"].Value);
                loadedEmp._empireName = empNode.Attributes["Name"].Value;

                loadedEmp._cultureID = Convert.ToInt32(empNode["CultureID"].InnerText);
                loadedEmp.curMil = (float)(Convert.ToDouble(empNode["MilitarySize"].InnerText));
                loadedEmp.maxMil = (float)(Convert.ToDouble(empNode["MaxMil"].InnerText));
                loadedEmp.religionID = Convert.ToInt32(empNode["ReligionID"].InnerText);
                loadedEmp.percentageEco = (float)Convert.ToDouble(empNode["PercentageEco"].InnerText);

                ColorUtility.TryParseHtmlString("#" + empNode["Colour"].InnerText, out loadedEmp._empireCol); //Sets colour via hex code

                foreach (XmlNode comps in empNode["Components"].ChildNodes)
                {
                    loadedEmp._componentProvinceIDs.Add(Convert.ToInt32(comps.InnerText));
                }

                XmlNodeList techNodes = empNode["Technology"].ChildNodes;
                loadedEmp.milTech = Convert.ToInt32(techNodes[0].InnerText);
                loadedEmp.ecoTech = Convert.ToInt32(techNodes[1].InnerText);
                loadedEmp.dipTech = Convert.ToInt32(techNodes[2].InnerText);
                loadedEmp.logTech = Convert.ToInt32(techNodes[3].InnerText);
                loadedEmp.culTech = Convert.ToInt32(techNodes[4].InnerText);

                outEmpires.Add(loadedEmp);
            }
        }
    }
}