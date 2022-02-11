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
            if (provs[provID]._ownerEmpire == null)
            {
                empires.Add(new Empire(empires.Count, provs[provID]._cityName, provs[provID]));
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool ForceConquerLand(ProvinceObject targetProv, Empire aggressorEmpire) //Used to and conquer land without cost/restrictions
        {

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
        public static bool UpdateCultures(ref List<Culture> cultures, ref List<ProvinceObject> provs, ref List<Empire> empires)
        {
            foreach(Culture x in cultures)
            {
                x.CalculateEconomy(ref empires,provs);
            }

            return true;
        }

        public static bool UpdateMilitary(ref List<Culture> cultures, ref List<Empire> empires, ref List<ProvinceObject> provs)
        {
            foreach (Empire x in empires)
            {
                x.RecruitMil(ref cultures, ref provs);
            }

            return true;
        }

        public static bool UpdateTech(ref List<Empire> empires, int id, string type)
        { 
            switch(type)
            {
                case "Military":
                    empires[id].milTech += 1;
                    return true;
                case "Economic":
                    empires[id].ecoTech += 1;
                    return true;
                case "Diplomacy":
                    empires[id].dipTech += 1;
                    return true;
                case "Logistics":
                    empires[id].logTech += 1;
                    return true;
                case "Culture":
                    empires[id].culTech += 1;
                    return true;
                default:
                    return false;
            }

        }

        public static bool NewReligion(ref List<ProvinceObject> provs, ref List<Religion> religions, int targetProv)
        {
            List<Religion> availableReligions = religions.Except(provs.Select(t => t._localReligion).Distinct().ToList()).ToList(); //Find unknown religion
            if (availableReligions.Count > 0)
            {
                provs[targetProv]._localReligion = availableReligions[0]; //Select next available religion in set
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool SetReligion(ref List<ProvinceObject> provs, ref List<Religion> religions, int targetProv, int targetReligion)
        {
            if (provs.Select(t => t._localReligion).Distinct().ToList().Contains(religions[targetReligion]) && provs[targetProv]._biome != 0) //Check if religion exists on map
            {
                provs[targetProv]._localReligion = religions[targetReligion]; //set religion
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool SetStateReligion(ref List<ProvinceObject> provs, ref List<Empire> empires, ref List<Religion> religions, int targetEmpire, int targetReligion)
        {
            if (provs.Select(t => t._localReligion).Distinct().ToList().Contains(religions[targetReligion])) //Check if religion exists on map
            {
                empires[targetEmpire].stateReligion = religions[targetReligion]; //set religion
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
