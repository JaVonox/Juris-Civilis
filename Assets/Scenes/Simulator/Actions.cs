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
            List<Empire> impactedEmpires = empires.Where(x => x.opinions.Any(y => y.targetEmpireID == aggressorEmpire._id) && x.ReturnAdjacentIDs(ref provs, true).Contains(targetProv._id)).ToList();
            Dictionary<Empire, (int value, int time, string type)> empireMods = new Dictionary<Empire, (int value, int time, string type)>() { };

            foreach (Empire x in impactedEmpires)
            {
                empireMods.Add(x, (-5,7300,"COLONY"));
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
                    if(adjacentProvs.All(x=>x._ownerEmpire == aggressorEmpire))
                    {
                        int reducedCost = Math.Max(5,Convert.ToInt32(Math.Min(Math.Floor((aggressorEmpire.ExpectedMilIncrease(ref provs) * 6.0f) * modifier), Math.Max(1, aggressorEmpire.maxMil / 2.0f))));
                        return (reducedCost < cost ? reducedCost : cost);
                    }
                }
            }
            return cost;
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

            int fieldedAttacker = Convert.ToInt32(Math.Floor(aggressorEmpire.curMil / Math.Min(10, Math.Max(2, aggressorEmpire._componentProvinceIDs.Count())) < 1 ? aggressorEmpire.curMil : aggressorEmpire.curMil / Math.Min(10,Math.Max(2,aggressorEmpire._componentProvinceIDs.Count())))); //Attacker army size 
            int fieldedDefender = Convert.ToInt32(Math.Floor(aggressorEmpire.curMil / Math.Min(10, Math.Max(2, aggressorEmpire._componentProvinceIDs.Count())) < 1 ? defenderEmpire.curMil : defenderEmpire.curMil / Math.Min(10, Math.Max(2, defenderEmpire._componentProvinceIDs.Count())))); //Defender army size

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

            Debug.Log("ATT POWER: " + attackerPower);
            Debug.Log("DEF POWER: " + defenderPower);

            if (attackerPower + defenderPower == 0) { return (0, 0, 0); }
            float attackerVicChance = Math.Min(0.95f,Math.Max(0.05f,attackerPower / (attackerPower + defenderPower)));
            return (fieldedAttacker, fieldedDefender, attackerVicChance);
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

                Debug.Log("ATT OFFSET:" + (3 * attOffset));
                Debug.Log("DEF OFFSET:" + (3 * defOffset));
            }
            else
            {
                attOffset = Math.Max(0,(0 + (float)Math.Round((((float)(actRand.NextDouble()) - (float)(actRand.NextDouble())) / 10.0f), 3)));
                defOffset = Math.Max(0.1f,(0.1f + (float)Math.Round((((float)(actRand.NextDouble()) - (float)(actRand.NextDouble())) / 10.0f), 3)) - Math.Min(0.4f, Math.Max(0, stats.attChance-0.5f)));

                Debug.Log("ATT OFFSET:" + (3*attOffset));
                Debug.Log("DEF OFFSET:" + (3*defOffset));
            }

            attRed = 1.0f-Math.Min(0.85f, Math.Max(0.3f, 0.3f + (Math.Min(85.0f, ((float)(aggressorEmpire.logTech)) / 50.0f) - 1.0f))+(3*attOffset)); //attacker loss reduction multiplier
            defRed = 1.0f-Math.Min(0.85f, Math.Max(0.3f, 0.3f + (Math.Min(85.0f, ((float)(defenderEmpire.logTech)) / 50.0f) - 1.0f))+(3*defOffset)); //defender loss reduction multiplie

            Debug.Log("ATT WON: " + attackerWon.ToString() + " ATT MOD: " + attRed + " DEF MOD: " + defRed);


            float attackExactLosses = Math.Min((float)Math.Max(1.0f,attRed * stats.attField),stats.attField);
            float defExactLosses = Math.Min((float)Math.Max(1.0f,defRed * stats.defField),stats.defField);

            Debug.Log("ATK LOSS: " + attackExactLosses + " DEF LOSS: " + defExactLosses);

            aggressorEmpire.TakeLoss(attackExactLosses);
            defenderEmpire.TakeLoss(defExactLosses);
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
            if (CanConquer(targetProv, aggressorEmpire, ref provs)) //Double check. Colonize land call should be proceeded by a CanConquer already.
            {
                (int attField, int defField, float attChance) stats = BattleStats(targetProv, aggressorEmpire, provs);
                Debug.Log(stats.ToString());
                float battleScore = (float)(actRand.NextDouble()) + 0.001f;

                if (battleScore < stats.attChance)
                {
                    CalculateLosses(targetProv, aggressorEmpire, ref provs, stats, true);
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

        public static bool SetStateReligion(ref List<ProvinceObject> provs, List<Empire> empires, ref List<Religion> religions, int targetEmpire, int targetReligion, ref Date curDate)
        {
            if (provs.Select(t => t._localReligion).Distinct().ToList().Contains(religions[targetReligion]) && empires[targetEmpire]._exists == true) //Check if religion exists on map
            {
                Empire tEmpire = empires[targetEmpire];
                if (tEmpire.stateReligion != null)
                {
                    List<Empire> impactedEmpires = empires.Where(y => y.opinions.Any(z => z.targetEmpireID == tEmpire._id) && y.stateReligion == tEmpire.stateReligion).ToList();
                    if (impactedEmpires.Count > 0)
                    {
                        foreach(Empire x in impactedEmpires)
                        { 
                            AddNewModifier(x, tEmpire, 10000, -20, (curDate.day, curDate.month, curDate.year), "RELIGIONSWITCH"); //Opinion penalty for switching religion
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

            if(recvEmpire.opinions.Any(x=>x.targetEmpireID == sendEmpire._id))
            {
                Date tmpDate = new Date();
                tmpDate.day = curDate.day;
                tmpDate.month = curDate.month;
                tmpDate.year = curDate.year;

                if (false) //Non-duplicate types
                {
                    if (recvEmpire.opinions.First(x => x.targetEmpireID == sendEmpire._id).modifiers.Any(x => x.typestring == type))
                    {
                        Modifier x = recvEmpire.opinions.First(x => x.targetEmpireID == sendEmpire._id).modifiers.Where(x => x.typestring == type).First();
                        x.opinionModifier = modifier;
                        Date tDate2 = Calendar.Calendar.ReturnDate(days, ref tmpDate);
                        x.timeOutDate = (tDate2.day, tDate2.month, tDate2.year);
                    } //No duplicate types
                }

                Modifier nMod = new Modifier(days, modifier, ref tmpDate, type);
                recvEmpire.opinions.First(x => x.targetEmpireID == sendEmpire._id).modifiers.Add(nMod);
                return true;
            }
            return false;
        }

        public static bool DiplomaticEnvoy(Empire recvEmpire, Empire sendEmpire, (int day, int month, int year) curDate, ref System.Random rnd, ref List<Empire> empires)
        {
            if (!recvEmpire.opinions.Any(x => x.targetEmpireID == sendEmpire._id)) { return false; }
            Opinion tOp = recvEmpire.opinions.First(x => x.targetEmpireID == sendEmpire._id);

            Date tmpDate = new Date();
            tmpDate.day = curDate.day;
            tmpDate.month = curDate.month;
            tmpDate.year = curDate.year;

            int optionChange = rnd.Next(1, Math.Max(20,sendEmpire.dipTech)); //Strength of opinion modifier
            int dateLasting = rnd.Next(1825, 3650); //How long it will last

            if(AddNewModifier(recvEmpire, sendEmpire, dateLasting, optionChange, curDate, "ENVOY"))
            {
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
            List<Empire> impactedEmpires = empires.Where(x => x.opinions.Any(y => y.targetEmpireID == targetEmpire._id && y.lastOpinion < 0)).ToList(); //All empires that dislike the target empire
            Dictionary<Empire, (int value, int time, string type)> empireMods = new Dictionary<Empire, (int value, int time, string type)>() { };

            foreach (Empire x in impactedEmpires)
            {
                int opMod = -5;
                Opinion tOp = x.opinions.First(x => x.targetEmpireID == targetEmpire._id);

                if (tOp._rival || tOp._fear) { opMod -= 5; }
                
                empireMods.Add(x, (opMod, 7300, "ENEMYENVOY")); //Opinion penalty for sending envoys to enemies

            }

            return empireMods;
        }
    }
}
