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

        public static bool SpawnEmpire(ref List<ProvinceObject> provs, int provID, ref List<Empire> empires, ref List<Culture> cultures)
        {
            if(provs[provID]._biome == 0) { return false; } //Prevent ocean takeover
            if (provs[provID]._ownerEmpire == null)
            {
                empires.Add(new Empire(empires.Count, provs[provID]._cityName, provs[provID], ref cultures, ref empires));
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool ForceConquerLand(ProvinceObject targetProv, Empire aggressorEmpire, ref List<ProvinceObject> provs) //Used to and conquer land without cost/restrictions
        {

            if(aggressorEmpire != null && aggressorEmpire._exists == true && targetProv._ownerEmpire != aggressorEmpire && targetProv._biome != 0)
            {
                if (IsAdjacent(targetProv,aggressorEmpire,ref provs)) 
                {
                    if (targetProv._ownerEmpire != null)
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
            else
            {
                return false;
            }
        }

        public static (bool,int) CanColonize(ProvinceObject targetProv, Empire aggressorEmpire, ref List<ProvinceObject> provs)
        {
            if (!IsAdjacent(targetProv, aggressorEmpire, ref provs) || targetProv._ownerEmpire != null || aggressorEmpire._exists != true || targetProv._biome == 0) { return (false,0); }
            else
            {
                int cCost = ColonyCost(targetProv, aggressorEmpire, ref provs);
                if (cCost <= aggressorEmpire.curMil)
                {
                    return (true,cCost);
                }
                else
                {
                    return (false,cCost);
                }
            }
        }
        public static bool ColonizeLand(ProvinceObject targetProv, Empire aggressorEmpire, ref List<ProvinceObject> provs)
        {
            (bool canColonize, int colonyCost) ColonyAbility = CanColonize(targetProv, aggressorEmpire, ref provs);
            if (ColonyAbility.canColonize == true) //Double check. Colonize land call should be proceeded by a CanColonize already.
            {
                aggressorEmpire.curMil -= ColonyAbility.colonyCost;
                if(aggressorEmpire.curMil < 0) { aggressorEmpire.curMil = 0; }
                targetProv._ownerEmpire = aggressorEmpire;
                aggressorEmpire._componentProvinceIDs.Add(targetProv._id);
                return true;
            }
            else
            {
                return false;
            }
        }
        public static int ColonyCost(ProvinceObject targetProv, Empire aggressorEmpire, ref List<ProvinceObject> provs) //Calculate cost for the colony to check if target empire can afford it
        {
            float BaseCost = 100;
            {
                float cTech = (float)Math.Max(1, aggressorEmpire.culTech);
                float Ac = (float)Math.Abs(Math.Log(cTech / 50.0f));
                float Bc = (float)Math.Log(cTech / 50.0f);
                float Cc = (float)Math.Min(0.9f, Math.Max(Ac, Bc));
                BaseCost = (1000.0f * (1 - Cc)) / 10.0f;
            }
            
            float modifier = 0;
            if(targetProv._population == Property.High || targetProv._population == Property.Low) { modifier += 2; }
            if(targetProv._elProp == Property.High) { modifier += 2; }
            if(targetProv._rainProp == Property.Low) { modifier+=2; }
            if(!provs.Where(x=>x._ownerEmpire == aggressorEmpire).Select(y=>y._cultureID).Contains(targetProv._cultureID)) { modifier += 3; }
            if(targetProv._floraProp == Property.High) { modifier--; }
            if(targetProv._isCoastal) { modifier--; }
            modifier -= (float)Math.Min(3.0f,Math.Floor(0.5f * (float)(provs.Count(x => x._ownerEmpire == aggressorEmpire && x._adjacentProvIDs.Contains(targetProv._id)))));
            if((float)(aggressorEmpire._componentProvinceIDs.Count()) /2 <= (float)(provs.Where(x=>x._ownerEmpire == aggressorEmpire).Count(y=>y._biome == targetProv._biome))) { modifier--; }

            if(modifier < 1) { modifier = Math.Max(0.1f,Math.Min(1,1-(Math.Abs(modifier) / 10.0f))); } //If modifier is less than 1, set it to become reduction modifiers.
            int cost = Math.Max(Convert.ToInt32(Math.Min(Math.Ceiling(aggressorEmpire.ExpectedMilIncrease(ref provs) * 12.0f), Math.Max(1, aggressorEmpire.maxMil))), Convert.ToInt32(Math.Floor(BaseCost * (modifier))));

            {
                List<int> adjacentProvIds = provs.First(x => x._id == targetProv._id)._adjacentProvIDs;
                List<ProvinceObject> adjacentProvs = provs.Where(x=>adjacentProvIds.Contains(x._id) && x._biome != 0).ToList();
                //Reduced cost if all provinces adjacent are owned by the aggressor
                if(adjacentProvs.Count > 0) 
                {
                    if(adjacentProvs.All(x=>x._ownerEmpire == aggressorEmpire))
                    {
                        int reducedCost = Math.Max(5,Convert.ToInt32(Math.Min(Math.Floor((aggressorEmpire.ExpectedMilIncrease(ref provs) * 6.0f)), Math.Max(1, aggressorEmpire.maxMil / 2.0f))));
                        return (reducedCost < cost ? reducedCost : cost);
                    }
                }
            }
            return cost;
        }
        public static bool CanConquer(ProvinceObject targetProv, Empire aggressorEmpire, ref List<ProvinceObject> provs)
        {
            //TODO add war requirement
            if (!IsAdjacent(targetProv, aggressorEmpire, ref provs) || targetProv._ownerEmpire == null || targetProv._biome == 0) { return false; }
            else
            {
                return true;
            }
        }
        public static bool ConquerLand(ProvinceObject targetProv, Empire aggressorEmpire, ref List<ProvinceObject> provs)
        {
            if (CanConquer(targetProv, aggressorEmpire, ref provs)) //Double check. Colonize land call should be proceeded by a CanColonize already.
            {
                if (targetProv._ownerEmpire != null)
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

        public static bool IsAdjacent(ProvinceObject targetProv, Empire tEmpire, ref List<ProvinceObject> provs) //Checks if the selected province is adjacent to the empire
        {
            if(tEmpire._exists != true) { return false; }
            return provs.Where(tP => tEmpire._componentProvinceIDs.Contains(tP._id)).SelectMany(p => p._adjacentProvIDs).Distinct().ToList().Contains(targetProv._id);
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
            List<Empire> appEmpires = empires.Where(t => t._exists == true).ToList();
            foreach (Empire x in appEmpires)
            {
                x.RecruitMil(ref cultures, ref provs);
            }

            return true;
        }

        public static bool UpdateTech(ref List<Empire> empires, int id, string type)
        { 
            if(empires[id]._exists == false)
            {
                return false;
            }

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
            if (provs.Select(t => t._localReligion).Distinct().ToList().Contains(religions[targetReligion]) && empires[targetEmpire]._exists == true) //Check if religion exists on map
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
