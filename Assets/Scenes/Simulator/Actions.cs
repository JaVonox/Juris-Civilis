using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Empires;
using WorldProperties;
using BiomeData;
using System;
using System.Linq;
using Calendar;

namespace Act
{
    public static class Actions //Contains all the actions that can be simulated
    {
        private static System.Random actRand = new System.Random();
        public static void ToggleDebug(GameObject debugRef)
        {
            debugRef.SetActive(!debugRef.activeSelf);
        }

        public static bool SpawnEmpire(ref List<ProvinceObject> provs, int provID, ref List<Empire> empires, ref List<Culture> cultures, ref System.Random rnd)
        {
            if(provs[provID]._biome == 0) { return false; } //Prevent ocean takeover
            if (provs[provID]._ownerEmpire == null)
            {
                empires.Add(new Empire(empires.Count, provs[provID]._cityName, provs[provID], ref cultures, ref empires, ref provs, ref rnd));
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
        public static bool ColonizeLand(ProvinceObject targetProv, Empire aggressorEmpire, List<ProvinceObject> provs, ref List<Empire> empires, ref Date curDate)
        {
            (bool canColonize, int colonyCost) ColonyAbility = CanColonize(targetProv, aggressorEmpire, ref provs);
            if (ColonyAbility.canColonize == true) //Double check. Colonize land call should be proceeded by a CanColonize already.
            {
                aggressorEmpire.curMil -= ColonyAbility.colonyCost;
                if(aggressorEmpire.curMil < 0) { aggressorEmpire.curMil = 0; }
                targetProv._ownerEmpire = aggressorEmpire;
                aggressorEmpire._componentProvinceIDs.Add(targetProv._id);
                if(aggressorEmpire.stateReligion != null && targetProv._localReligion == null) { targetProv._localReligion = aggressorEmpire.stateReligion; }

                Dictionary<Empire, (int value, int time, string type)> colonyOPmods = ColonyOpinionMods(targetProv, aggressorEmpire, provs, empires);

                foreach(KeyValuePair<Empire, (int value, int time, string type)> set in colonyOPmods) //Apply opinion modifiers
                {
                    AddNewModifier(set.Key, aggressorEmpire, set.Value.time, set.Value.value, (curDate.day, curDate.month, curDate.year), set.Value.type);
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        public static Dictionary<Empire, (int value, int time, string type)> ColonyOpinionMods(ProvinceObject targetProv, Empire aggressorEmpire, List<ProvinceObject> provs, List<Empire> empires)
        {
            //Opinion changes due to colony action
            List<Empire> impactedEmpires = empires.Where(x => x.opinions.Any(y => y.Value.targetEmpireID == aggressorEmpire._id) && x.ReturnAdjacentIDs(ref provs, true).Contains(targetProv._id)).ToList();
            Dictionary<Empire, (int value, int time, string type)> empireMods = new Dictionary<Empire, (int value, int time, string type)>() { };

            foreach (Empire x in impactedEmpires)
            {
                int valueMod = -5;
                if(x.opinions[aggressorEmpire._id]._rival || x.ReturnProvPersonalVal(targetProv,provs,true) >= aggressorEmpire.ReturnProvPersonalVal(targetProv,provs,true))
                {
                    valueMod -= 10; //Increased negative modifier if rival or the impacted empire has a greater personal value on the province than the aggressor
                }
                empireMods.Add(x, (valueMod, 1825,"COLONY")); //Making new colonies gives a (-5 to -15) 5 year penalty. This can stack per colony.
            }

            return empireMods;
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
            int cost = Math.Max(Convert.ToInt32(Math.Min(Math.Ceiling((aggressorEmpire.ExpectedMilIncrease(ref provs) * 12.0f) * modifier), Math.Max(1, aggressorEmpire.maxMil))), Convert.ToInt32(Math.Floor(BaseCost * (modifier))));

            {
                List<int> adjacentProvIds = provs.First(x => x._id == targetProv._id)._adjacentProvIDs;
                List<ProvinceObject> adjacentProvs = provs.Where(x=>adjacentProvIds.Contains(x._id) && x._biome != 0).ToList();
                //Reduced cost if all provinces adjacent are owned by the aggressor
                if(adjacentProvs.Count > 0) 
                {
                    if(adjacentProvs.All(x=>x._ownerEmpire == aggressorEmpire || x._biome == 0))
                    {
                        int reducedCost = Math.Max(1,Convert.ToInt32(Math.Floor(aggressorEmpire.ExpectedMilIncrease(ref provs))));
                        return (reducedCost < cost ? reducedCost : cost);
                    }
                }
            }
            return cost;
        }

        public static Dictionary<Empire, (int value, int time, string type)> WarOpinionMods(ProvinceObject targetProv, Empire aggressorEmpire, List<ProvinceObject> provs, List<Empire> empires) //TargetProv is used to get the enemy
        {
            Empire defenderEmpire = targetProv._ownerEmpire;
            //Opinion changes due to colony action
            List<Empire> impactedEmpires = empires.Where(x => x.opinions.Any(y => y.Value.targetEmpireID == aggressorEmpire._id)).ToList();
            Dictionary<Empire, (int value, int time, string type)> empireMods = new Dictionary<Empire, (int value, int time, string type)>() { };

            foreach (Empire x in impactedEmpires)
            {
                if (x.opinions.ContainsKey(aggressorEmpire._id))
                {
                    int valueMod = -5; //For the most part, other nations dont care about wars that dont impact them

                    if (x.opinions.ContainsKey(defenderEmpire._id))
                    {
                        if (x.opinions[defenderEmpire._id]._ally)
                        {
                            valueMod -= 15; //If allied, bonus negative impact
                        }
                        else if (x.opinions[defenderEmpire._id]._fear && !x.opinions[aggressorEmpire._id]._rival)
                        {
                            valueMod += 15; //If they fear the defender empire, reduce negative impact.
                        }

                    }

                    empireMods.Add(x, (valueMod, 3650, "WARSTARTER")); //War declaration gives a large negative value 
                }
            }

            return empireMods;
        }

        public static (int attackerMaxCost, int defenderMaxCost, float attackerVicChance) BattleStats(ProvinceObject targetProv, Empire aggressorEmpire, List<ProvinceObject> provs)
        {
            //TODO add time concerns - i.e time of year
            Empire defenderEmpire = targetProv._ownerEmpire;

            if (aggressorEmpire.curMil == 0 || aggressorEmpire.maxMil == 0)
            {
                return (0, 0, 0);
            }
            else if (defenderEmpire.curMil == 0 || defenderEmpire.maxMil == 0)
            {
                return (0, 0, 1);
            }

            int fieldedAttacker = Convert.ToInt32(Math.Floor(10.0f + aggressorEmpire.curMil / (float)Math.Min(4, Math.Max(2, aggressorEmpire._componentProvinceIDs.Count())) < aggressorEmpire.curMil ? aggressorEmpire.curMil : 10.0f + aggressorEmpire.curMil / (float)Math.Min(4,Math.Max(2,aggressorEmpire._componentProvinceIDs.Count())))); //Attacker army size 
            int fieldedDefender = Convert.ToInt32(Math.Floor(10.0f + defenderEmpire.curMil / (float)Math.Min(4, Math.Max(2, defenderEmpire._componentProvinceIDs.Count())) < defenderEmpire.curMil ? defenderEmpire.curMil : 10.0f + defenderEmpire.curMil / (float)Math.Min(4, Math.Max(2, defenderEmpire._componentProvinceIDs.Count())))); //Defender army size

            float defenderModifier = 1.2f;
            {
                if(targetProv._elProp == Property.High || targetProv._elProp == Property.Medium) { defenderModifier+=1.5f; } //Height advantage
                if(targetProv._isCoastal == true) { defenderModifier += 0.5f; } //Seige immunity
                if(targetProv._tmpProp == Property.Low) { defenderModifier+=0.5f; }
                else if(targetProv._tmpProp == Property.High && ((float)(aggressorEmpire._componentProvinceIDs.Count(x=>provs[x]._tmpProp == Property.High)) / 2.0f) < aggressorEmpire._componentProvinceIDs.Count())
                {
                    defenderModifier+=0.5f; //If the attacker is not prepared for high temperature wars
                }
            }
            //Max mod = 4

            float attackerModifier = 0.8f; //Attackers are always at a disadvantage and must choose their battles carefully
            {
                if(targetProv._floraProp == Property.High) { attackerModifier++; } //Food for soldiers
                if (targetProv._elProp == Property.Low)
                {
                    attackerModifier += 0.2f;  //Flat terrain grants a small bonus to attackers
                    if (aggressorEmpire._componentProvinceIDs.Where(x => provs[x]._adjacentProvIDs.Contains(targetProv._id)).Any(x => provs[x]._elProp == Property.Medium))
                    {
                        attackerModifier += 0.8f; //Attacking from hills grants another bonus
                    }
                }
                if(targetProv._localReligion == null || aggressorEmpire.stateReligion == null || targetProv._localReligion != aggressorEmpire.stateReligion) { attackerModifier += 0.5f; } //Religious war bonus
            }
            //Max mod = 3.3 

            //Reinforcement bonuses
            {
                float aggReinforcements = aggressorEmpire.ReturnAdjacentIDs(ref provs, false).Count(x => x == targetProv._id) - 1;
                float defReinforcements = defenderEmpire._componentProvinceIDs.Where(x => provs[x]._adjacentProvIDs.Contains(targetProv._id) && x != targetProv._id).Count();

                if (aggReinforcements > defReinforcements)
                {
                    attackerModifier += Math.Min(2.0f, (aggReinforcements / defReinforcements)/2.0f);
                }
                else if (defReinforcements > aggReinforcements)
                {
                    defenderModifier += Math.Min(2.0f, (defReinforcements / aggReinforcements)/2.0f);
                }
            }

            float attackerPower = ((float)(fieldedAttacker)) * attackerModifier;
            float defenderPower = ((float)(fieldedDefender)) * defenderModifier;

            if (attackerPower + defenderPower == 0) { return (0, 0, 0); }
            float attackerVicChance = Math.Min(0.95f,Math.Max(0.05f,attackerPower / (attackerPower + defenderPower)));
            return (fieldedAttacker, fieldedDefender, attackerVicChance);
        }
        
        public static bool RevolutionaryRebelsAttack(Empire defenderEmpire, Rebellion rebelGroup, ref List<ProvinceObject> provs, ref System.Random rnd)
        {
            int fieldedAttacker = Convert.ToInt32(Math.Floor(rebelGroup.rebelStrength));
            int fieldedDefender = Convert.ToInt32(Math.Floor(defenderEmpire.curMil));
            ProvinceObject targetProv = provs[defenderEmpire._componentProvinceIDs[0]];


            float defenderModifier = 1.2f;
            {
                if (targetProv._elProp == Property.High || targetProv._elProp == Property.Medium) { defenderModifier += 1.5f; } //Height advantage
                if (targetProv._isCoastal == true) { defenderModifier += 0.5f; } //Seige immunity
                if (targetProv._tmpProp == Property.Low) { defenderModifier += 0.5f; }
            }

            float attackerModifier = 0.8f; //Attackers are always at a disadvantage and must choose their battles carefully
            {
                if (targetProv._floraProp == Property.High) { attackerModifier++; } //Food for soldiers
                if (targetProv._elProp == Property.Low)
                {
                    attackerModifier += 0.2f;  //Flat terrain grants a small bonus to attackers
                }
            }

            float attackerPower = ((float)(fieldedAttacker)) * attackerModifier;
            float defenderPower = ((float)(fieldedDefender)) * defenderModifier;

            float rebelVicChance = Math.Min(0.95f, Math.Max(0.05f, attackerPower / (attackerPower + defenderPower)));

            bool won = rnd.NextDouble() < rebelVicChance;

            float defRed = 0.0f;

            float attOffset = 0;
            float defOffset = 0;

            if (won)
            {
                defOffset = Math.Max(0, 0 + (float)Math.Round((((float)(actRand.NextDouble()) - (float)(actRand.NextDouble())) / 10.0f), 3));
            }
            else
            {
                attOffset = Math.Max(0, (0 + (float)Math.Round((((float)(actRand.NextDouble()) - (float)(actRand.NextDouble())) / 10.0f), 3)));
                defOffset = Math.Max(0.1f, (0.1f + (float)Math.Round((((float)(actRand.NextDouble()) - (float)(actRand.NextDouble())) / 10.0f), 3)) - Math.Min(0.4f, Math.Max(0, rebelVicChance - 0.5f)));
                defOffset = Math.Min(defOffset, attOffset * 1.5f);//Winner cannot have more than 1.5x the loss of the loser
            }

            defRed = 1.0f - Math.Min(0.85f, Math.Max(0.3f, 0.3f + (Math.Min(85.0f, ((float)(defenderEmpire.logTech)) / 50.0f) - 1.0f)) + (3 * defOffset)); //defender loss reduction multiplie

            float defExactLosses = Math.Min((float)Math.Max(1.0f, defRed * defenderPower), defenderPower) * 0.5f;

            defenderEmpire.TakeLoss(defExactLosses, !won, false, null, targetProv, ref provs);

            return won;
        }
        public static void CalculateLosses(ProvinceObject targetProv, Empire aggressorEmpire, ref List<ProvinceObject> provs, (int attField, int defField, float attChance) stats, bool attackerWon)
        {
            Empire defenderEmpire = targetProv._ownerEmpire;

            float attRed = 0.0f;
            float defRed = 0.0f;

            float attOffset = 0;
            float defOffset = 0;
            if(attackerWon)
            {
                attOffset = Math.Max(0.1f,(0.1f + (float)Math.Round((((float)(actRand.NextDouble()) - (float)(actRand.NextDouble()))/10.0f),3)) - Math.Min(0.4f,Math.Max(0,0.5f-stats.attChance)));
                defOffset = Math.Max(0,0 + (float)Math.Round((((float)(actRand.NextDouble()) - (float)(actRand.NextDouble())) / 10.0f), 3));
                attOffset = Math.Min(attOffset, defOffset * 1.5f); //Winner cannot have more than 1.5x the loss of the loser
            }
            else
            {
                attOffset = Math.Max(0,(0 + (float)Math.Round((((float)(actRand.NextDouble()) - (float)(actRand.NextDouble())) / 10.0f), 3)));
                defOffset = Math.Max(0.1f,(0.1f + (float)Math.Round((((float)(actRand.NextDouble()) - (float)(actRand.NextDouble())) / 10.0f), 3)) - Math.Min(0.4f, Math.Max(0, stats.attChance-0.5f)));
                defOffset = Math.Min(defOffset, attOffset * 1.5f);//Winner cannot have more than 1.5x the loss of the loser
            }

            attRed = 1.0f-Math.Min(0.85f, Math.Max(0.3f, 0.3f + (Math.Min(85.0f, ((float)(aggressorEmpire.logTech)) / 50.0f) - 1.0f))+(3*attOffset)); //attacker loss reduction multiplier
            defRed = 1.0f-Math.Min(0.85f, Math.Max(0.3f, 0.3f + (Math.Min(85.0f, ((float)(defenderEmpire.logTech)) / 50.0f) - 1.0f))+(3*defOffset)); //defender loss reduction multiplie

            float attackExactLosses = Math.Min((float)Math.Max(1.0f,attRed * stats.attField),stats.attField);
            float defExactLosses = Math.Min((float)Math.Max(1.0f,defRed * stats.defField),stats.defField);

            Debug.Log("ATK LOSS: " + attackExactLosses + " DEF LOSS: " + defExactLosses);

            aggressorEmpire.TakeLoss(attackExactLosses,attackerWon,true, defenderEmpire,targetProv, ref provs);
            defenderEmpire.TakeLoss(defExactLosses,!attackerWon,false, aggressorEmpire, targetProv, ref provs);
        }
        public static bool CanConquer(ProvinceObject targetProv, Empire aggressorEmpire, ref List<ProvinceObject> provs)
        {
            if (!IsAdjacent(targetProv, aggressorEmpire, ref provs) || targetProv._ownerEmpire == null || targetProv._biome == 0) { return false; }
            else
            {
                if (aggressorEmpire.opinions.ContainsKey(targetProv._ownerEmpire._id))
                {
                    if(aggressorEmpire.opinions[targetProv._ownerEmpire._id]._isWar == true) { return true; }
                }
            }

            return false;
        }
        public static bool ConquerLand(ProvinceObject targetProv, Empire aggressorEmpire, ref List<ProvinceObject> provs)
        {
            if (CanConquer(targetProv, aggressorEmpire, ref provs)) //Double check. Colonize land call should be proceeded by a CanConquer already.
            {
                (int attField, int defField, float attChance) stats = BattleStats(targetProv, aggressorEmpire, provs);
                float battleScore = (float)(actRand.NextDouble()) + 0.001f;

                if (battleScore < stats.attChance) //If won battle
                {
                    CalculateLosses(targetProv, aggressorEmpire, ref provs, stats, true);
                    if (targetProv._ownerEmpire != null)
                    {
                        targetProv._ownerEmpire._componentProvinceIDs.Remove(targetProv._id); //Remove province from set of owned provinces in previous owner
                    }

                    Actions.IncreaseUnrest("occupied", aggressorEmpire, provs, new List<int>() { targetProv._id }); //Increase unrest for changing nation

                    targetProv._ownerEmpire = aggressorEmpire;
                    aggressorEmpire._componentProvinceIDs.Add(targetProv._id);
                    return true;
                }
                else
                {
                    CalculateLosses(targetProv, aggressorEmpire, ref provs, stats, false);
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        public static bool IsAdjacent(ProvinceObject targetProv, Empire tEmpire, ref List<ProvinceObject> provs) //Checks if the selected province is adjacent to the empire
        {
            if(tEmpire._exists != true) { return false; }
            List<int> adjProvs = tEmpire.ReturnAdjacentIDs(ref provs, true);
            return provs.Any(tP => adjProvs.Contains(targetProv._id));
        }
        public static bool UpdateCultures(ref List<Culture> cultures, ref List<ProvinceObject> provs, ref List<Empire> empires)
        {
            foreach(Culture x in cultures)
            {
                x.CalculateEconomy(ref empires,provs);
            }

            return true;
        }

        public static bool UpdateMilitary(ref List<Culture> cultures, ref List<Empire> empires, ref List<ProvinceObject> provs, ref System.Random rnd)
        {
            List<Empire> appEmpires = empires.Where(t => t._exists == true).ToList();
            foreach (Empire x in appEmpires)
            {
                x.RecruitMil(ref cultures, ref provs);

                if(x.rebels.Count > 0)
                {
                    foreach(Rebellion r in x.rebels)
                    {
                        r.rebelStrength += x.RebelMilIncrease(r,provs) * (float)(rnd.NextDouble()); //Add military strength to rebels
                        if(r.rebelStrength >= x.maxMil) { r.rebelStrength = x.maxMil; } //TODO add random element to max mil
                        Debug.Log(x._id + " REBEL = " + r.rebelStrength);
                    }
                }
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

        public static bool SetStateReligion(ref List<ProvinceObject> provs, List<Empire> empires, List<Religion> religions, int targetEmpire, int targetReligion, ref Date curDate)
        {
            //this is conducted *before* state religion is changed
            if (provs.Select(t => t._localReligion).Distinct().ToList().Contains(religions[targetReligion]) && empires[targetEmpire]._exists == true) //Check if religion exists on map
            {
                Empire tEmpire = empires[targetEmpire];
                if (tEmpire.stateReligion != null)
                {
                    List<Empire> impactedEmpires = empires.Where(y => y.opinions.Any(z => z.Value.targetEmpireID == tEmpire._id) && (y.stateReligion == tEmpire.stateReligion || y.stateReligion == religions[targetReligion])).ToList();
                    if (impactedEmpires.Count > 0)
                    {
                        foreach(Empire x in impactedEmpires)
                        {
                            int mod = 0;
                            if(x.stateReligion == tEmpire.stateReligion) { mod = -20; }
                            else if(x.stateReligion == religions[targetReligion]) { mod = 10; }
                            AddNewModifier(x, tEmpire, 10000, mod, (curDate.day, curDate.month, curDate.year), "RELIGIONSWITCH"); //Opinion penalty for switching religion
                        }
                    }
                }
                tEmpire.stateReligion = religions[targetReligion]; //set religion
                return SetReligion(ref provs, ref religions, tEmpire._componentProvinceIDs[0], targetReligion);
            }
            else
            {
                return false;
            }
        }

        public static bool AddNewModifier(Empire recvEmpire, Empire sendEmpire, int days, int modifier, (int day, int month, int year) curDate, string type)
        {
            if(!recvEmpire._exists || !sendEmpire._exists) { return false; }

            if(recvEmpire.opinions.ContainsKey(sendEmpire._id))
            {
                Date tmpDate = new Date();
                tmpDate.day = curDate.day;
                tmpDate.month = curDate.month;
                tmpDate.year = curDate.year;

                if (type == "LEARNED" || type == "DIPTECH" || type == "CULTECH" || type == "RELIGIONSWITCH") //Non-duplicate types
                {
                    if (recvEmpire.opinions[sendEmpire._id].modifiers.Any(x => x.typestring == type))
                    {
                        Modifier x = recvEmpire.opinions[sendEmpire._id].modifiers.Where(x => x.typestring == type).First();
                        if(x.opinionModifier < modifier) { x.opinionModifier = modifier; }
                        Date tDate2 = Calendar.Calendar.ReturnDate(days, ref tmpDate);
                        x.timeOutDate = (tDate2.day, tDate2.month, tDate2.year);
                        return true;
                    }
                }

                Modifier nMod = new Modifier(days, modifier, ref tmpDate, type);
                recvEmpire.opinions[sendEmpire._id].modifiers.Add(nMod);

                return true;
            }
            return false;
        }

        public static bool DiplomaticEnvoy(Empire recvEmpire, Empire sendEmpire, (int day, int month, int year) curDate, ref System.Random rnd, ref List<Empire> empires)
        {
            if (!recvEmpire.opinions.ContainsKey(sendEmpire._id)) { return false; }
            Opinion tOp = recvEmpire.opinions[sendEmpire._id];

            Date tmpDate = new Date();
            tmpDate.day = curDate.day;
            tmpDate.month = curDate.month;
            tmpDate.year = curDate.year;

            int optionChange = rnd.Next(20, Math.Min(40,25 + Convert.ToInt32(Math.Round((float)(sendEmpire.dipTech)/2.0f)))); //Strength of opinion modifier
            int dateLasting = 3650; //How long it will last

            if(AddNewModifier(recvEmpire, sendEmpire, dateLasting, optionChange, curDate, "ENVOY"))
            {
                AddNewModifier(sendEmpire, recvEmpire, dateLasting, optionChange, curDate, "ENVOY"); //Bonuses apply both ways

                Dictionary<Empire, (int value, int time, string type)> colonyOPmods = EnvoyMod(recvEmpire,empires); //Calculate diplomatic impacts of this action

                foreach (KeyValuePair<Empire, (int value, int time, string type)> set in colonyOPmods) //Apply opinion modifiers
                {
                    AddNewModifier(set.Key, sendEmpire, set.Value.time, set.Value.value, (curDate.day, curDate.month, curDate.year), set.Value.type);
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        public static Dictionary<Empire, (int value, int time, string type)> EnvoyMod(Empire targetEmpire, List<Empire> empires)
        {
            //Opinion changes due to envoy action
            List<Empire> impactedEmpires = empires.Where(x => x.opinions.ContainsKey(targetEmpire._id)).ToList(); //All empires with opinions of this nation
            Dictionary<Empire, (int value, int time, string type)> empireMods = new Dictionary<Empire, (int value, int time, string type)>() { };

            foreach (Empire x in impactedEmpires) //For all empires that have opinions on the target
            {
                int opMod = 0; 

                if (x.opinions.ContainsKey(targetEmpire._id))
                {
                    Opinion tOp = x.opinions[targetEmpire._id];
                    if (tOp._rival || tOp._fear) { opMod -= 10; } //Decrease in opinion if the empire fears or rivals the target 
                    else if (tOp._ally) { opMod += 15; } //Increase in opinion if the empire is allied

                    empireMods.Add(x, (opMod, 3650, "ADJACENTENVOY")); //Opinion change for sending envoys to others
                }

            }

            return empireMods;
        }

        public static void IncreaseUnrest(string conditionType, Empire myEmpire, List<ProvinceObject> provs, List<int>? impactedIDs) //increase unrest in provinces
        {
            float multiplier = 1.0f;

            //Multipliers for various negative impacts
            if (myEmpire._componentProvinceIDs.Count > Math.Max(5, myEmpire.culTech)) { multiplier += .25f; }
            if(myEmpire._componentProvinceIDs.Any(x=>provs[x]._unrest > myEmpire.GetUnrestCap() && (myEmpire.rebels.Count == 0 || !myEmpire.rebels.Any(y => y.IsContained(x))))){ multiplier += .25f; }
            if(myEmpire.rebels.Count > 0) { multiplier += .1f; }
            //TODO add for increasing unrest when declaring war on empire of non-primary culture

            if (!myEmpire._exists) { return; }
            switch (conditionType) //Increment 
            {
                case "religionSwitch": //If the ruler switches religion, increment unrest for each religious non-primary item.
                    {
                        List<ProvinceObject> impactedProvinces = myEmpire._componentProvinceIDs.Select(x => provs[x]).Where(y => y._localReligion != null && y._localReligion != myEmpire.stateReligion).ToList(); //All provinces with non-primary religion
                        foreach (ProvinceObject t in impactedProvinces)
                        {
                            t._unrest += 1.0f * multiplier;
                        }
                        return;
                    }
                case "forcedConvert": //If a province is forcibly converted by a ruler. uses impactedIDs
                    {
                        if(impactedIDs == null) { IncreaseUnrest("default", myEmpire, provs, impactedIDs); return; } //If no listed impactedIDs, then just increase all unrest. This is a bug
                        List<ProvinceObject> impactedProvinces = impactedIDs.Select(x => provs[x]).ToList(); //All provinces with non-primary religion
                        foreach (ProvinceObject t in impactedProvinces)
                        {
                            t._unrest += 1.0f * multiplier;
                        }
                        return;
                    }
                case "cultureSwitch": //When primary culture changes
                    {
                        List<ProvinceObject> impactedProvinces = myEmpire._componentProvinceIDs.Select(x => provs[x]).Where(y => y._cultureID != myEmpire._cultureID).ToList(); //All provinces with non-primary culture
                        foreach (ProvinceObject t in impactedProvinces)
                        {
                            t._unrest += 0.5f * multiplier;
                        }
                        return;
                    }
                case "newRuler": //When new ruler takes power
                    {
                        foreach (int provID in myEmpire._componentProvinceIDs)
                        {
                            provs[provID]._unrest += 0.5f * multiplier;
                        }
                        return;
                    }
                case "newDynasty": //When new dynasty takes power
                    {
                        foreach (int provID in myEmpire._componentProvinceIDs)
                        {
                            provs[provID]._unrest += 1.0f * multiplier;
                        }
                        return;
                    }
                case "occupied": //When occupied by a foreign power (non-colony)
                    {
                        provs[impactedIDs[0]]._unrest += 0.5f * multiplier;
                        return;
                    }
                case "lostWar": //When lost more provinces in a war than taken
                    {
                        foreach (int provID in myEmpire._componentProvinceIDs)
                        {
                            provs[provID]._unrest += 0.5f * multiplier;
                        }
                        return;
                    }
                case "capitalChanged": //When the capital is changed
                    {
                        foreach (int provID in myEmpire._componentProvinceIDs)
                        {
                            provs[provID]._unrest += 2.0f * multiplier;
                        }
                        return;
                    }
                case "localPolitics": //When the ruler aggrivates provinces or unrest is stirred
                    {
                        foreach (int provID in impactedIDs)
                        {
                            provs[provID]._unrest += (1 + (myEmpire.curRuler.rulerPersona["per_SpawnRebellion"] - myEmpire.curRuler.rulerPersona["per_Calm"])) * multiplier;
                        }
                        return;
                    }
                case "colony": //When a colony is made 
                    {
                        foreach (int provID in impactedIDs)
                        {
                            float modifier = myEmpire.ReturnPopScore(provs[provID], provs) - 0.25f;
                            modifier = myEmpire._cultureID != provs[provID]._cultureID ? modifier + 1 : modifier - 0.5f;

                            modifier = Math.Max(0.25f, modifier);
                            provs[provID]._unrest += modifier * multiplier;
                        }
                        return;
                    }
                case "cultureTech": //When a new culture tech is created
                    {
                        foreach (int t in myEmpire._componentProvinceIDs)
                        {
                            if(provs[t]._cultureID == myEmpire._cultureID) { provs[t]._unrest -= 0.2f; }
                            else { provs[t]._unrest -= 0.05f; }
                        }
                        return;
                    }
                case "warWeary": //Increases over time when a nation is over their war exhaustion cap
                    {
                        foreach (int t in myEmpire._componentProvinceIDs)
                        {
                            provs[t]._unrest += 0.1f * multiplier;
                        }
                        return;
                    }
                case "spreading": //Added unrest from active rebels
                    {
                        foreach (int t in impactedIDs)
                        {
                            provs[t]._unrest += 0.1f;
                        }
                        return;
                    }
                default:
                    {
                        Debug.Log("UNKNOWN UNREST " + conditionType);
                        foreach (int provID in myEmpire._componentProvinceIDs)
                        {
                            provs[provID]._unrest += 0.1f * multiplier;
                        }
                        return;
                    }
            }
        }
    }
}
