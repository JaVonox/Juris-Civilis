using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; //objects

//enum biome
//{
//    Temperate_Forest,
//    Tropical_Forest,
//    Taiga,
//    Grasslands,
//    Savannah,
//    Tundra,
//    Desert,
//    Mountain_Range,
//    Forested_Plateau,
//    Shrubland_Plateau,
//}

namespace BiomeData
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
        static System.Random rnd = new System.Random();
        public static List<Biome> activeBiomes = new List<Biome>(){
        new Biome("Ocean",Property.Low,Property.NA,Property.NA,Property.NA,new Color(0.04f,0.08f,0.58f)),
        new Biome("Temperate Forest",Property.Medium,Property.Medium,Property.NA,Property.High,new Color(0.35f,0.82f,0.06f)),
        new Biome("Tropical Forest", Property.Medium, Property.High, Property.High, Property.High, new Color(0.09f,0.55f,0.02f)),
        new Biome("Taiga", Property.Medium, Property.Low, Property.NA, Property.High, new Color(0.27f,0.54f,0.47f)),
        new Biome("Grasslands", Property.Medium, Property.Medium, Property.NA, Property.Low, new Color(0.58f,0.95f,0.41f)),
        new Biome("Savannah", Property.Medium, Property.High, Property.High, Property.Low, new Color(0.73f,0.91f,0.28f)),
        new Biome("Tundra", Property.Medium, Property.Low, Property.NA, Property.Low, new Color(0.36f,0.6f,0.77f)),
        new Biome("Desert", Property.Medium, Property.High, Property.Low, Property.Low, new Color(0.87f,0.86f,0.15f)),
        new Biome("Mountain", Property.High, Property.Low, Property.NA, Property.Low, new Color(0.43f,0.53f,0.49f)),
        new Biome("Forested Plateau", Property.High, Property.NA, Property.NA, Property.High, new Color(0.13f,0.22f,0.17f)),
        new Biome("Shrubland Plateau", Property.High, Property.High, Property.NA, Property.Low, new Color(0.16f,0.28f,0.13f)),
        };

        public static Biome SortTile(TileData target, ref int[,] deciles)
        {
            float[] indexScores = new float[activeBiomes.Count]; //make temporary array the size of the activeBiomes list

            foreach (Biome tBiome in activeBiomes) //find the score of each biome against the target data
            {
                indexScores[activeBiomes.IndexOf(tBiome)] = tBiome.Score(target, ref deciles);
            }

            float maxVal = -1;
            int maxIndex = -1;

            for (int i = 0; i < activeBiomes.Count; i++) //iterate through all active biomes except ocean
            {
                if (indexScores[i] > maxVal || maxIndex == -1)
                {
                    maxVal = indexScores[i];
                    maxIndex = i;
                }
            }

            return activeBiomes[maxIndex]; //returns the appropriate biome for this tile
        }

    }

    public class TileData //Stored in X,Y array. Stores the properties for each pixel.
    {
        //Set by external
        public int _height;
        public int _temperature;
        public int _rainfall;
        public int _flora;

        public Property _heightProp;
        public Property _tempProp;
        public Property _rainfallProp;
        public Property _floraProp;

        //Set by internal
        //TODO maybe switch to private?
        public Biome _biomeType;

        public TileData() //Empty constructor - for new TileData() calls
        {

        }

        private void DefineProperties(ref int[,] deciles) //Select the property value for each property
        {
            if (_height < deciles[0, 5]) { _heightProp = Property.Low; }
            else if(_height >= deciles[0,5] && _height < deciles[0,8]) { _heightProp = Property.Medium; }
            else { _heightProp = Property.High; }

            if (_temperature < deciles[1, 2]) { _tempProp = Property.Low; }
            else if (_temperature >= deciles[1, 2] && _temperature < deciles[1, 7]) { _tempProp = Property.Medium; }
            else { _tempProp = Property.High; }

            //rainfall and flora are only high/low
            if (_rainfall < deciles[2, 1]) { _rainfallProp = Property.Low; }
            else { _rainfallProp = Property.High; }

            if (_flora < deciles[3, 3]) { _floraProp = Property.Low; }
            else { _floraProp = Property.High; }

        }

        public void InitializeBiome(ref int[,] deciles) //Set the biome for each tile, using the decile set as comparison
        {
            this.DefineProperties(ref deciles);
            _biomeType = BiomesObject.SortTile(this, ref deciles); //attempts a self sort
        }

    }

    public class Biome
    {
        public string _name;
        Property _desireElevation;
        Property _desireTemperature;
        Property _desireRainfall;
        Property _desireFlora;
        Color _colour;

        public Biome(string name, Property elT, Property tempT, Property rnT, Property flrT, Color colour)
        {
            _name = name;
            _desireElevation = elT;
            _desireTemperature = tempT;
            _desireRainfall = rnT;
            _desireFlora = flrT;
            _colour = colour;
        }

        public float Score(TileData targetTile, ref int[,] deciles) //returns the deviance of a tile from this biome. the set of these scores is then polled to find the biome with the most appropriate typing
        {
            float score=0;

            if(targetTile._heightProp == _desireElevation)
            {
                score += 1;
            }
            else if(_desireElevation == Property.NA)
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