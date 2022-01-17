using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; //objects

//For quick querying
using System.Linq;

namespace PropertiesGenerator
{
    public static class GenerateNames //Generates a list of names to be stored, most notably the province names
    {
        public static List<string> GenerateProvinceName(ref System.Random rnd, int numToGenerate)
        {
            //Loads city names to be used to generate new city names
            TextAsset citiesFile = (TextAsset)Resources.Load("Cities");
            string[] citiesSet = citiesFile.text.Split('\n');
            citiesFile = null;

            List<string> newGeneratedCities = new List<string>();


            //This algorithm works to generate a list of new city names for the world generator
            //It does this by taking a number of characters from a random city name, then searching for a city with a letter in the same position, taking a random number of items from that set and so on.
            //This often generates psuedo-realistic city names, as well as sometimes generating the names of real cities
            for(int i=0;i<numToGenerate;i++)
            {
                int nextID = rnd.Next(0, citiesSet.Length);
                List<char> newCityName = new List<char>();
                int currentLength = 0;
                int maxLength = citiesSet[nextID].Length;

                List<string> applicableSet = citiesSet.ToList();

                while (currentLength < maxLength)
                {
                    int amountToAppend = 1 + ((applicableSet[nextID].Length - currentLength) % 5); //Gets a random value between 1 and the remaining length of a string or 5

                    if(applicableSet[nextID].Length - currentLength < amountToAppend) //If there are too few possible characters left
                    {
                        break; //Instantly end the generation procedure
                    }
                    
                    newCityName.AddRange(applicableSet[nextID].Substring(currentLength, amountToAppend));
                    currentLength += amountToAppend;
                    applicableSet = FindMatchAtIndex(ref citiesSet, currentLength, newCityName[currentLength - 1]); //Search for matching character
                    nextID = rnd.Next(0, applicableSet.Count); //Find next ID
                }

                string newCityString = new string(newCityName.ToArray());

                if(newCityString.Length <= 3) //Redo attempts with too few characters
                {
                    i--;
                }
                else //Add to set of generated cities names
                {
                    newGeneratedCities.Add(newCityString);
                }
            }

            return newGeneratedCities;
        }

        private static List<string> FindMatchAtIndex(ref string[] set, int ind, char match) //Returns a list of all matches for a query
        {
            //This finds parts of the set inwhich a character matches at the same point.
            //The ternary is included to ensure no errors occur.
            return set.Where(p => p.Length >= ind && p[(p.Length < ind ? 0 : ind - 1)] == match).ToList();
        }


    }

}