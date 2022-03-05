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
using PropertiesGenerator;

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
        public static string CreateFile(int worldWidth, int worldHeight, bool overwrite, string prePath, int day, int month, int year, string WorldName) //Creates a new save file structure
        {

            string path = "";
            string setWorldName = "";

            {
                if (!Directory.Exists(System.IO.Directory.GetCurrentDirectory().ToString() + "/Saves/")) //Create a save folder if one does not exist
                {
                    Directory.CreateDirectory(System.IO.Directory.GetCurrentDirectory().ToString() + "/Saves/");
                }

                if (!overwrite)
                {
                    bool isWrittenTo = false;

                    if(WorldName == "(DEFAULT)")
                    {
                        WorldName = "World";
                        isWrittenTo = false;
                    }
                    else
                    {
                        if (!Directory.Exists(System.IO.Directory.GetCurrentDirectory().ToString() + "/Saves/" + WorldName))
                        {
                            path = System.IO.Directory.GetCurrentDirectory().ToString() + "/Saves/" + WorldName + "/";
                            Directory.CreateDirectory(path + "WorldData/");
                            setWorldName = WorldName;
                            isWrittenTo = true;
                        }
                        else
                        {
                            isWrittenTo = false;
                        }
                    }

                    if (!isWrittenTo)
                    {
                        int iterate = 1;
                        while (true) //Find the next available world save folder
                        {
                            if (!Directory.Exists(System.IO.Directory.GetCurrentDirectory().ToString() + "/Saves/" + WorldName + "(" + iterate + ")"))
                            {
                                path = System.IO.Directory.GetCurrentDirectory().ToString() + "/Saves/" + WorldName + "(" + iterate + ")/";
                                Directory.CreateDirectory(path + "WorldData/");
                                setWorldName = WorldName + "(" + iterate + ")";
                                break;
                            }

                            iterate++;
                        }
                    }
                }
                else
                {
                    XmlDocument empFile = new XmlDocument();
                    path = prePath + "/";
                    setWorldName = WorldName;
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
            xmlWriter.WriteString(setWorldName);
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

                CreateReligions(path);
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
                xmlWriter.WriteAttributeString("ReligionID", tProv._localReligion == null ? "NULL" : tProv._localReligion._id.ToString());

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

        public static void LoadProvinces(string filepath, ref List<ProvinceObject> outputProvs, ref List<Empire> empires, ref List<Religion> rels)
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
                string relID = provNode.Attributes["ReligionID"].Value;
                loadedProv._localReligion = relID == "NULL" ? null : rels[Convert.ToInt32(relID)];

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

                xmlWriter.WriteStartElement("NameScheme");
                xmlWriter.WriteString(tCult._nameType.ToString());
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
                loadedCult._nameType = cultNode["NameScheme"].InnerText;
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
                empData.WriteAttributeString("ReligionID", tEmpire.stateReligion == null ? "NULL" : tEmpire.stateReligion._id.ToString());
                empData.WriteAttributeString("Exists", tEmpire._exists.ToString());

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

                empData.WriteStartElement("PercentageEco");
                empData.WriteString(tEmpire.percentageEco.ToString());
                empData.WriteEndElement();

                empData.WriteStartElement("UpdateTime");
                empData.WriteString(tEmpire.timeUntilNextUpdate.ToString());
                empData.WriteEndElement();

                empData.WriteStartElement("OccupationCooldown");
                empData.WriteString(tEmpire.occupationCooldown.ToString());
                empData.WriteEndElement();

                empData.WriteStartElement("Exhaustion");
                empData.WriteString(tEmpire.warExhaustion.ToString());
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

                empData.WriteStartElement("TechPoints");
                empData.WriteString(tEmpire.techPoints.ToString());
                empData.WriteEndElement();

                empData.WriteEndElement();

                empData.WriteStartElement("Opinions");
                foreach (Opinion opin in tEmpire.opinions.Values)
                {
                    empData.WriteStartElement("Opinion");
                    empData.WriteAttributeString("Target",opin.targetEmpireID.ToString());
                    empData.WriteAttributeString("LastOpinion", opin.lastOpinion.ToString());

                    empData.WriteAttributeString("Fear", opin._fear.ToString());
                    empData.WriteAttributeString("Rival", opin._rival.ToString());
                    empData.WriteAttributeString("Ally", opin._ally.ToString());
                    empData.WriteAttributeString("War", opin._isWar.ToString());

                    empData.WriteAttributeString("ExhaustCap", opin._maxWarExhaustion.ToString());
                    empData.WriteAttributeString("Disputes", opin._capturedProvinces.ToString());

                    empData.WriteStartElement("Modifiers"); //Opinion Modifiers
                    foreach (Modifier mod in opin.modifiers)
                    {
                        empData.WriteStartElement("Modifier");
                        string modDate = mod.timeOutDate.day + "," + mod.timeOutDate.month + "," + mod.timeOutDate.year;
                        empData.WriteAttributeString("End", modDate);
                        empData.WriteAttributeString("Modifier", mod.opinionModifier.ToString());
                        empData.WriteEndElement();
                    }
                    empData.WriteEndElement();

                    empData.WriteEndElement();
                }
                empData.WriteEndElement();

                ///Ruler data
                Ruler tRuler = tEmpire.curRuler;
                empData.WriteStartElement("Ruler");
                empData.WriteAttributeString("FirstName", tRuler.fName);
                empData.WriteAttributeString("LastName", tRuler.lName);
                empData.WriteAttributeString("Age", tRuler.age.ToString());
                empData.WriteAttributeString("Converted", tRuler.hasAdoptedRel.ToString());

                empData.WriteStartElement("Birthday");
                empData.WriteString(tRuler.birthday.day.ToString() + "/" + tRuler.birthday.month.ToString());
                empData.WriteEndElement();

                empData.WriteStartElement("Deathday");
                empData.WriteString(tRuler.deathday.day.ToString() + "/" + tRuler.deathday.month.ToString() + "/" + tRuler.deathday.age);
                empData.WriteEndElement();

                empData.WriteStartElement("Personality");
                foreach(KeyValuePair<string,float> persona in tRuler.rulerPersona)
                {
                    empData.WriteStartElement(persona.Key);
                    empData.WriteAttributeString("Score", persona.Value.ToString());
                    empData.WriteEndElement();
                }

                empData.WriteStartElement("TechFocus");
                empData.WriteString(tRuler.techFocus[0].ToString() + "," + tRuler.techFocus[1].ToString());
                empData.WriteEndElement();

                empData.WriteEndElement();

                empData.WriteEndElement();

                empData.WriteEndElement();
            }
            empData.WriteEndElement();
            empData.Close();


            //Loading of provinces to change empire data where applicable
            XmlDocument provFile = new XmlDocument();
            provFile.Load(filepath + "/WorldData/Provinces.xml");

            foreach(ProvinceObject tProv in provs)
            {
                XmlNode provNode = provFile.SelectSingleNode("/Provinces/Province[@ID='" + tProv._id.ToString() + "']"); //Get the node with the appropriate ID
                provNode["Owner"].InnerText = tProv._ownerEmpire == null ? "" : tProv._ownerEmpire._id.ToString();
                provNode.Attributes["ReligionID"].Value = tProv._localReligion == null ? "NULL" : tProv._localReligion._id.ToString();
            }

            provFile.Save(filepath + "/WorldData/Provinces.xml");

        }

        public static void LoadEmpires(string filepath, ref List<Empire> outEmpires, ref List<Religion> rels)
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
                string relID = empNode.Attributes["ReligionID"].Value;
                loadedEmp.stateReligion = relID == "NULL" ? null : rels[Convert.ToInt32(relID)];
                loadedEmp._exists = Convert.ToBoolean(empNode.Attributes["Exists"].Value);

                loadedEmp._cultureID = Convert.ToInt32(empNode["CultureID"].InnerText);
                loadedEmp.curMil = (float)(Convert.ToDouble(empNode["MilitarySize"].InnerText));
                loadedEmp.maxMil = (float)(Convert.ToDouble(empNode["MaxMil"].InnerText));
                loadedEmp.percentageEco = (float)Convert.ToDouble(empNode["PercentageEco"].InnerText);
                loadedEmp.timeUntilNextUpdate = Convert.ToInt32(empNode["UpdateTime"].InnerText);
                loadedEmp.occupationCooldown = Convert.ToInt32(empNode["OccupationCooldown"].InnerText);
                loadedEmp.warExhaustion = (float)Convert.ToDouble(empNode["Exhaustion"].InnerText);

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
                loadedEmp.techPoints = Convert.ToInt32(techNodes[5].InnerText); //Tech points

                foreach (XmlNode opinions in empNode["Opinions"].ChildNodes) //Add opinion data
                {
                    Opinion newOp = new Opinion();
                    newOp.targetEmpireID = Convert.ToInt32(opinions.Attributes["Target"].Value.ToString());
                    newOp.lastOpinion = (float)(Convert.ToDouble(opinions.Attributes["LastOpinion"].Value.ToString()));
                    newOp._fear = (Convert.ToBoolean(opinions.Attributes["Fear"].Value.ToString()));
                    newOp._rival = (Convert.ToBoolean(opinions.Attributes["Rival"].Value.ToString()));
                    newOp._ally = (Convert.ToBoolean(opinions.Attributes["Ally"].Value.ToString()));
                    newOp._isWar = (Convert.ToBoolean(opinions.Attributes["War"].Value.ToString()));

                    newOp._maxWarExhaustion = (float)(Convert.ToDouble(opinions.Attributes["ExhaustCap"].Value.ToString()));
                    newOp._capturedProvinces = (float)(Convert.ToDouble(opinions.Attributes["Disputes"].Value.ToString()));

                    foreach (XmlNode modifiers in opinions["Modifiers"].ChildNodes)
                    {
                        Modifier tMod = new Modifier();
                        string[] dateSplit = modifiers.Attributes["End"].Value.Split(',');
                        tMod.timeOutDate = (Convert.ToInt32(dateSplit[0]), Convert.ToInt32(dateSplit[1]), Convert.ToInt32(dateSplit[2]));
                        tMod.opinionModifier = (float)Convert.ToDouble(modifiers.Attributes["Modifier"].Value);
                        newOp.modifiers.Add(tMod);
                    }
                    loadedEmp.opinions.Add(newOp.targetEmpireID,newOp);
                }

                //Loading ruler data
                XmlNode rulNode = empNode.SelectSingleNode("Ruler");
                Ruler tmpRuler = new Ruler();

                tmpRuler.fName = rulNode.Attributes["FirstName"].Value;
                tmpRuler.lName = rulNode.Attributes["LastName"].Value;
                tmpRuler.age = Convert.ToInt32(rulNode.Attributes["Age"].Value);
                string[] bDay = rulNode.SelectSingleNode("Birthday").InnerText.ToString().Split('/');
                tmpRuler.birthday.day = Convert.ToInt32(bDay[0]);
                tmpRuler.birthday.month = Convert.ToInt32(bDay[1]);
                string[] dDay = rulNode.SelectSingleNode("Deathday").InnerText.ToString().Split('/');
                tmpRuler.deathday.day = Convert.ToInt32(dDay[0]);
                tmpRuler.deathday.month = Convert.ToInt32(dDay[1]);
                tmpRuler.deathday.age = Convert.ToInt32(dDay[2]);
                tmpRuler.hasAdoptedRel = Convert.ToBoolean(rulNode.Attributes["Converted"].Value);

                XmlNode personalityNode = rulNode.SelectSingleNode("Personality");
                foreach (XmlNode comps in personalityNode.ChildNodes)
                {
                    if (comps.Name.StartsWith("per_"))
                    {
                        tmpRuler.rulerPersona[comps.Name] = (float)Convert.ToDouble(comps.Attributes["Score"].Value);
                    }
                    else if(comps.Name == "TechFocus")
                    {
                        string[] techs = comps.InnerText.Split(',');
                        tmpRuler.techFocus[0] = Convert.ToInt32(techs[0]);
                        tmpRuler.techFocus[1] = Convert.ToInt32(techs[1]);
                    }
                }

                loadedEmp.curRuler = tmpRuler;

                outEmpires.Add(loadedEmp);
            }
        }

        public static void CreateReligions(string filePath) //Creates a number of religions and saves them to a file. Only called on generation saving.
        {
            System.Random rnd = new System.Random();

            List<Religion> religions = PropertiesGenerator.GenerateNames.PullReligions(ref rnd);
            //Write all religion properties to an xml file
            XmlWriter xmlWriter = XmlWriter.Create(filePath + "Simulation/Religions.xml", settings);
            xmlWriter.WriteStartDocument();

            xmlWriter.WriteStartElement("Religions");
            foreach (Religion tRel in religions)
            {
                xmlWriter.WriteStartElement("Religion");
                xmlWriter.WriteAttributeString("ID", tRel._id.ToString());
                xmlWriter.WriteAttributeString("Name", tRel._name);

                xmlWriter.WriteStartElement("Colour");
                xmlWriter.WriteString(ColorUtility.ToHtmlStringRGB(tRel._col));
                xmlWriter.WriteEndElement();

                xmlWriter.WriteEndElement();
            }
            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndDocument();
            xmlWriter.Close();
        }

        public static void LoadReligions(string filepath, ref List<Religion> outRels)
        {
            outRels.Clear();

            XmlDocument xmlReader = new XmlDocument(); //Open xmlfile
            xmlReader.Load(filepath + "/Simulation/Religions.xml");

            XmlNode relNodes = xmlReader.SelectSingleNode("Religions");

            foreach (XmlNode relNode in relNodes.ChildNodes)
            {
                //Xml file is by ID so add should order correctly
                Religion loadedReligion = new Religion();
                loadedReligion._id = Convert.ToInt32(relNode.Attributes["ID"].Value);
                loadedReligion._name = relNode.Attributes["Name"].Value;

                ColorUtility.TryParseHtmlString("#" + relNode["Colour"].InnerText, out loadedReligion._col); //Sets colour via hex code
                outRels.Add(loadedReligion);
            }
        }

    }
}