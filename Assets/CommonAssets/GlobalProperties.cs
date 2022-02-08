using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using BiomeData;
using Empires;
namespace WorldProperties
{
    public enum Property
    {
        Low,
        Medium,
        High,
        NA
    }
    public static class BiomesObject
    {
        public static List<Biome> activeBiomes = new List<Biome>(){
        new Biome("Ocean",Property.Low,Property.NA,Property.NA,Property.NA,new Color(0.04f,0.08f,0.58f),400), //ocean should always be 0th
        new Biome("Temperate Forest",Property.Medium,Property.Medium,Property.NA,Property.High,new Color(0.01f,0.39f,0),16),
        new Biome("Tropical Forest", Property.Medium, Property.High, Property.High, Property.High, new Color(0.06f,0.34f,0.05f),36),
        new Biome("Taiga", Property.Medium, Property.Low, Property.NA, Property.High, new Color(0.06f,0.22f,0),36),
        new Biome("Grasslands", Property.Medium, Property.Medium, Property.NA, Property.Low, new Color(0.02f,0.54f,0),24),
        new Biome("Savannah", Property.Medium, Property.High, Property.High, Property.Low, new Color(0.78f,0.55f,0.15f),16),
        new Biome("Tundra", Property.Medium, Property.Low, Property.NA, Property.Low, new Color(1,0.78f,0.78f),48),
        new Biome("Desert", Property.Medium, Property.High, Property.Low, Property.Low, new Color(0.77f,0.61f,0.23f),36),
        new Biome("Mountain", Property.High, Property.Low, Property.NA, Property.Low, new Color(0.5f,0.5f,0.5f),8),
        new Biome("Forested Plateau", Property.High, Property.NA, Property.NA, Property.High, new Color(0.5f,0.5f,0.5f),8),
        new Biome("Shrubland Plateau", Property.High, Property.High, Property.NA, Property.Low, new Color(0.5f,0.5f,0.5f),8),
        };

        public static int SortTile(TileData target, ref int[,] deciles)
        {
            if (target._heightProp == Property.Low) //Oceans
            {
                return 0;
            }

            int acBiomesCount = activeBiomes.Count;

            float[] indexScores = new float[acBiomesCount]; //make temporary array the size of the activeBiomes list

            foreach (Biome tBiome in activeBiomes) //find the score of each biome against the target data
            {
                indexScores[activeBiomes.IndexOf(tBiome)] = tBiome.Score(target, ref deciles);
            }

            float maxVal = -1;
            int maxIndex = -1;

            for (int i = 1; i < acBiomesCount; i++) //iterate through all active biomes except ocean
            {
                if (indexScores[i] > maxVal || maxIndex == -1)
                {
                    maxVal = indexScores[i];
                    maxIndex = i;
                }
            }

            return maxIndex; //returns the appropriate biome for this tile
        }

    }
    public class ProvinceObject //Stores the data for a province after province data has been appended. This is the saved province data.
    {
        public int _id;
        public string _cityName;
        public int _biome;
        public Color _provCol;
        public Property _elProp;
        public Property _tmpProp;
        public Property _rainProp;
        public Property _floraProp;
        public Property _population;
        public bool _isCoastal;
        public List<Vector3> _vertices = new List<Vector3>();
        public List<int> _adjacentProvIDs = new List<int>();
        public int _cultureID;

        //Simulation variables
        public Empire _ownerEmpire;
        public ProvinceObject(int id, string name, Province tProv) //Constructor from province object
        {
            _id = id;
            _cityName = name;
            _biome = tProv._biome;
            _provCol = tProv._provCol;
            _adjacentProvIDs = tProv.adjacentProvIDs;
            _elProp = tProv._elProp;
            _tmpProp = tProv._tmpProp;
            _rainProp = tProv._rainProp;
            _floraProp = tProv._floraProp;
            _population = Property.NA;
            _cultureID = 0;
            _isCoastal = false;

            foreach (Chunk compChunk in tProv._componentChunks.Values)
            {
                for (int v = 0; v < 3; v++)
                {
                    _vertices.Add(new Vector3(compChunk.vertices[v].x, compChunk.vertices[v].y, 0));
                }
            }
        }
        public ProvinceObject()
        {
            //Used for loading in save data
        }
        public void NewOwner(Empire empireOwner) //Used to set the new owner empire if applicable
        {
            _ownerEmpire = empireOwner;
        }
        public void GenerateFinalValues(ref System.Random rnd, bool isCoastal)
        {
            if(_biome != 0 && _elProp != Property.High)
            {
                if(rnd.Next(0,8) == 5) //randomly make hills across the map
                {
                    _elProp = Property.Medium;
                }
                else
                {
                    _elProp = Property.Low;
                }
            }

            _isCoastal = isCoastal; //copy coastal info
        }
        public Vector3 CalculateRelativeCenterPoint() //returns the relative centerpoint for the province
        {
            float lowestX = -1;
            float highestX = -1;
            float lowestY = -1;
            float highestY = -1;

            foreach (Vector3 vert in _vertices)
            {
                if (vert.x < lowestX || lowestX == -1) { lowestX = vert.x; }
                if (vert.x > highestX || highestX == -1) { highestX = vert.x; }
                if (vert.y < lowestY || lowestY == -1) { lowestY = vert.y; }
                if (vert.y > highestY || highestY == -1) { highestY = vert.y; }
            }

            float centerX = lowestX + (((float)highestX - (float)lowestX) / 2);
            float centerY = lowestY + (((float)highestY - (float)lowestY) / 2);

            return new Vector3(centerX, centerY, 0);
        }

    }
    public class Culture
    {
        public string _id;
        public Color _cultureCol;
        public string _name;
        public float _economyScore; //total of techs of all empires inside = economy score
        public Culture(string id, ref System.Random rnd)
        {
            _id = id;
            _economyScore = 0;
            if (_id == "0") { new Color(0, 0, 0); }
            else { _cultureCol = new Color((float)rnd.Next(0, 256) / (float)255, (float)rnd.Next(0, 256) / (float)255, (float)rnd.Next(0, 256) / (float)255); }
        }

        public Culture()
        {
            //For loading in cultures
        }

        public void CalculateEconomy(ref List<Empire> empires, List<ProvinceObject> provinces) //Gets the total economic score of a region and set all components scores. done every year
        {
            int myID = Convert.ToInt32(_id);
            List<Empire> applicableEmpires = empires.Where(t => t._cultureID == myID).ToList();

            //Total of all techs
            if (applicableEmpires.Count != 0)
            {
                _economyScore = applicableEmpires.Sum(x => x.ReturnTechTotal());

                //ecoTech min/max
                float ecoTechMax = applicableEmpires.Max(x => (float)x.ecoTech);

                ecoTechMax += 0.001f; 

                //empire size min/max
                float empSizeMax = (float)applicableEmpires.Max(x => x.ReturnPopScore(provinces));
                empSizeMax += 0.001f;

                float sumScore = applicableEmpires.Sum(x => (float)(Math.PI * (Math.Sin((float)(x.ecoTech) / ecoTechMax))) + ((float)(Math.Log10(100 *(x.ReturnPopScore(provinces)/ empSizeMax) + 0.01f))));

                foreach (Empire tEmp in applicableEmpires)
                {
                    //Set the percentage power of each empire within the culture group
                    float myEmpSize = tEmp.ReturnPopScore(provinces);

                    float myTechScore = (float)(Math.PI * (Math.Sin((float)(tEmp.ecoTech) / ecoTechMax)));
                    float mySizeScore = (float)(Math.Log10(100 * ((myEmpSize / empSizeMax) + 0.01f)));
                    float myScore = myTechScore + mySizeScore;

                    tEmp.percentageEco = (float)(myScore) / (float)(sumScore);
                }
            }
        }
    }

    public class Biome
    {
        public string _name;

        //Stores what the expected properties are for a biome of this type
        Property _desireElevation;
        Property _desireTemperature;
        Property _desireRainfall;
        Property _desireFlora;
        Color _colour;
        public int _provinceSpread; //decides how many chunks a province of this type will spread

        public Biome(string name, Property elT, Property tempT, Property rnT, Property flrT, Color colour, int provSpread)
        {
            _name = name;
            _desireElevation = elT;
            _desireTemperature = tempT;
            _desireRainfall = rnT;
            _desireFlora = flrT;
            _colour = colour;
            _provinceSpread = provSpread;
        }

        public float Score(TileData targetTile, ref int[,] deciles) //returns the deviance of a tile from this biome. the set of these scores is then polled to find the biome with the most appropriate typing
        {
            float score = 0;

            if (targetTile._heightProp == _desireElevation)
            {
                score += 10;
            }
            else if (_desireElevation == Property.NA)
            {
                score += 0.5f;
            }

            if (targetTile._tempProp == _desireTemperature)
            {
                score += 1;
            }
            else if (_desireTemperature == Property.NA)
            {
                score += 0.5f;
            }

            if (targetTile._rainfallProp == _desireRainfall)
            {
                score += 1;
            }
            else if (_desireRainfall == Property.NA)
            {
                score += 0.5f;
            }

            if (targetTile._floraProp == _desireFlora)
            {
                score += 1;
            }
            else if (_desireFlora == Property.NA)
            {
                score += 0.5f;
            }

            return score;
        }

        public Color GetColour()
        {
            return _colour;
        }
    }
}
