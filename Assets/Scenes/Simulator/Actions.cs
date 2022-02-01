using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Empires;
using WorldProperties;
using BiomeData;
using System;
using System.Linq;
namespace Act
{
    public static class Actions //Contains all the actions that can be simulated
    {
        public static void ToggleDebug(GameObject debugRef)
        {
            debugRef.SetActive(!debugRef.activeSelf);
        }

        public static bool SpawnEmpire(ref List<ProvinceObject> provs, int provID, ref List<Empire> empires)
        {
            if(provs[provID]._biome == 0) { return false; } //Prevent ocean takeover
            empires.Add(new Empire(empires.Count, provs[provID]._cityName, provs[provID]));
            return true;
        }

        public static bool ConquerLand(ProvinceObject targetProv, Empire aggressorEmpire) //Used to try and conquer land
        {
            //TODO add changing of values such as military power or units (if applicable)

            if(aggressorEmpire != null && targetProv._ownerEmpire != aggressorEmpire && targetProv._biome != 0)
            {
                if(targetProv._ownerEmpire != null)
                {
                    targetProv._ownerEmpire._componentProvinceIDs.Remove(targetProv._id); //Remove province from set of owned provinces in previous owner
                }
                targetProv._ownerEmpire = aggressorEmpire;
                aggressorEmpire._componentProvinceIDs.Add(targetProv._id);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
