using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using WorldProperties;
using BiomeData;
using Act;
using System;
using System.Linq;
using Calendar;

namespace Empires //Handles empires and their existance. Actions they may take are in Actions.cs
{
    public class Empire
    {
        public bool _exists;
        public int _id;
        public string _empireName;
        public BindingList<int> _componentProvinceIDs = new BindingList<int>();
        public Color _empireCol;
        public int _cultureID;
        //Empire values

        //Techs
        public int milTech;
        public int ecoTech;
        public int dipTech;
        public int logTech;
        public int culTech;

        //Ruler + Religion
        public Ruler curRuler;
        public Religion stateReligion;


        //Simulation properties
        public float percentageEco; //Percentage of the culture economy owned by this nation
        public Dictionary<int,Opinion> opinions = new Dictionary<int, Opinion>(); //Opinion of other cultures

        public float maxMil;
        public float curMil;
        public float leftoverMil;
        public int timeUntilNextUpdate;

        public int occupationCooldown; //The amount of time before this nation can attack again
        public Empire(int id, string name, ProvinceObject startingProvince, ref List<Culture> cultures, ref List<Empire> empires, ref List<ProvinceObject> provs, ref System.Random rnd) //Constructor for an empire - used when a new empire is spawned
        {
            _id = id;
            _exists = true;
            _empireName = name;
            _componentProvinceIDs.Add(startingProvince._id);
            startingProvince.NewOwner(this); //Append self to set of owner
            _componentProvinceIDs.ListChanged += CheckEmpireExists;
            _empireCol = startingProvince._provCol;

            _cultureID = startingProvince._cultureID;
            stateReligion = startingProvince._localReligion != null ? startingProvince._localReligion : null;

            {
                (int milTech, int ecoTech, int dipTech, int logTech, int culTech) baseTechs = cultures[_cultureID].CalculateMinTech(ref empires);
                milTech = baseTechs.milTech;
                ecoTech = baseTechs.ecoTech;
                dipTech = baseTechs.dipTech;
                logTech = baseTechs.logTech;
                culTech = baseTechs.culTech;
            }

            timeUntilNextUpdate = 30; //30 gives them a month before they can begin having updates occur
            occupationCooldown = 0;

            provs[_componentProvinceIDs[0]].updateText = "New Nation";

            curRuler = new Ruler(null,this, ref cultures, ref empires, ref provs, ref rnd); //Create new random ruler
            RecruitMil(ref cultures, ref provs);

        }
        public bool CheckForUpdate(ref System.Random rnd)
        {
            timeUntilNextUpdate--;

            if(timeUntilNextUpdate <= 0)
            {
                timeUntilNextUpdate = rnd.Next(3, 20);
                return true;
            }
            else
            {
                return false;
            }

        }
        public Empire() //For loading
        {

        }
        private void CheckEmpireExists(object sender, ListChangedEventArgs e)
        {
            if (_componentProvinceIDs.Count == 0) //If no more component provs
            {
                _exists = false;
                _cultureID = -1;
            }
        }
        public float ReturnProvsVal(List<ProvinceObject> provinces)
        {
            return _componentProvinceIDs.Sum(x => ReturnIndProvVal(provinces[x],provinces));
        }

        public float ReturnPopScore(ProvinceObject tProv, List<ProvinceObject> provs)
        {
            float score = 0;
            if (tProv._population == Property.High) { score += 1.0f; }
            else if (tProv._population == Property.Medium) { score += 0.5f; }
            else if (tProv._population == Property.Low) { score += 0.25f; }

            return score;
        }
        public float ReturnIndProvVal(ProvinceObject tProv, List<ProvinceObject> provs) //Economic value per province
        {
            float score = ReturnPopScore(tProv, provs);

            if (tProv._isCoastal) { score += 0.2f; }
            score += 0.1f * (float)(tProv._adjacentProvIDs.Count(x=>provs[x]._ownerEmpire != null));

            return score;
        }

        public float ReturnProvPersonalVal(ProvinceObject tProv, List<ProvinceObject> provs) //Economic value + personal value
        {
            float score = ReturnIndProvVal(tProv,provs);
            if(tProv._localReligion == stateReligion && stateReligion != null) { score += 0.3f; }
            if(tProv._biome == provs[_componentProvinceIDs[0]]._biome) { score += 0.1f; }
            score += (float)(_componentProvinceIDs.Count(x => tProv._adjacentProvIDs.Contains(x))) * 0.25f; //Adjacency bonus
            score += (tProv._adjacentProvIDs.Contains(_componentProvinceIDs[0])) ? 1f : 0;
            if(tProv._cultureID == _cultureID) { score += 0.3f; }
            if(tProv._adjacentProvIDs.Count(x=>provs[x]._biome != 0 && _componentProvinceIDs.Contains(x)) == tProv._adjacentProvIDs.Count(x => provs[x]._biome != 0)) { score += 5.0f; } //If prov owns all adjacents, add a lot to score
            return score;
        }
        public int ReturnTechTotal()
        {
            return milTech + ecoTech + dipTech + logTech + culTech;
        }
        public float ReturnEcoScore(List<ProvinceObject> provinces) //Get economics score for this empire
        {
            return ((float)(milTech + ecoTech + dipTech + logTech + culTech)/10.0f) + _componentProvinceIDs.Sum(x => ReturnIndividualEcoScore(provinces[x], provinces));
        }
        public float ReturnIndividualEcoScore(ProvinceObject tProv, List<ProvinceObject> provs) //Get economics score for a single province
        {
            float multiplier = 1; //Higher economic output if it is the same religion as the empire in question
            if(stateReligion == null) { multiplier = 0.8f; }
            else if(stateReligion != null && tProv._localReligion != stateReligion) { multiplier = 0.9f; }

            return (ReturnIndProvVal(tProv,provs) * (1.0f + ((float)(ecoTech)/5.0f))) * multiplier;
        }
        public void RecalculateMilMax(List<ProvinceObject> provinces) //Finds the appropriate max military. Redone every time mil is recruited
        {
            maxMil = Convert.ToInt32(Math.Floor(Math.Min(100000000,
                (20.0f) + ((float)(milTech) * 20.0f) + (ReturnProvsVal(provinces) * 10.0f))));
        }
        public float ExpectedMilIncrease(ref List<ProvinceObject> provinces)
        {
            float milInc = ((float)Math.Round(1.0f+(float)Math.Min(100000000,
                ReturnEcoScore(provinces)/2.0f),2));

            if (occupationCooldown > 0) { milInc /= 2.0f; } //During war, military recruitment is slowed to provide reinforcements

            return milInc;
        }
        public void RecruitMil(ref List<Culture> cultures, ref List<ProvinceObject> provinces) //Every month recalculate military gain
        {
            RecalculateMilMax(provinces);
            float trueMilExp = ExpectedMilIncrease(ref provinces);
            if (leftoverMil < 0) // If there is a reinforcement debt
            {
                leftoverMil += trueMilExp;
            }
            else
            {
                leftoverMil += trueMilExp - (float)Math.Floor(trueMilExp);
                curMil += (float)Math.Floor(trueMilExp);
                if (leftoverMil >= 1) //If the leftovers are enough to increment
                {
                    curMil += (float)Math.Floor(leftoverMil);
                    leftoverMil -= (float)Math.Floor(leftoverMil);
                }
                if (curMil > maxMil) { curMil = maxMil; }
            }
        }

        public void TakeLoss(float losses)
        {
            int intLoss = Convert.ToInt32(Math.Floor(losses));
            float leftoverLosses = losses - (float)(intLoss);

            if(curMil - intLoss < 0)
            {
                leftoverLosses += curMil - (float)(intLoss);
                curMil = 0;
            }
            else
            {
                curMil -= intLoss;
            }

            leftoverLosses = (float)Math.Round(leftoverLosses, 2);
            leftoverMil -= leftoverLosses; //Reduce leftover losses by
        }
        public void PollForAction(ref Date currentDate, ref List<Culture> cultures, ref List<Empire> empires, ref List<ProvinceObject> provs, ref List<Religion> religions, ref System.Random rnd)
        {
            if (_exists) //If this empire is active
            {
                AgeMechanics(ref currentDate, ref cultures, ref empires, ref provs, ref rnd);
                if (BlockIfWar()) { WarAI(ref cultures, empires, provs, ref religions, ref rnd, ref currentDate); } //If in a war, actions will occur regardless of action chance. empires have their own cooldown for battles.


                if (CheckForUpdate(ref rnd))
                {

                    //Chance of ruler making an action
                    double actChance = Math.Min(0.85f, ((float)(dipTech) / 300.0f));
                    if (rnd.NextDouble() <= actChance)
                    {
                        AI(curRuler.CalculateRandomActsOrder(ref rnd), ref cultures, empires, provs, ref religions, ref rnd, ref currentDate);
                    }
                }
            }
        }
        public void AgeMechanics(ref Date currentDate, ref List<Culture> cultures, ref List<Empire> empires, ref List<ProvinceObject> provs, ref System.Random rnd)
        {
            if (currentDate.day == curRuler.birthday.day && currentDate.month == curRuler.birthday.month) //On birthday
            {
                curRuler.age++;
            }

            if ((currentDate.day >= curRuler.deathday.day && currentDate.month >= curRuler.deathday.month && curRuler.age == curRuler.deathday.age) || (curRuler.age > curRuler.deathday.age)) //on deathday
            {
                //Replace ruler
                Ruler tmpRuler = new Ruler(curRuler, this, ref cultures, ref empires, ref provs, ref rnd);
                curRuler = tmpRuler;
            }
        }
        public List<int> ReturnAdjacentIDs(ref List<ProvinceObject> provs, bool isDistinct)
        {
            List<int> pSet = provs.Where(x => x._ownerEmpire == this).SelectMany(y => y._adjacentProvIDs).ToList();
            if(isDistinct) { pSet = pSet.Distinct().ToList(); }
            return pSet.Where(z => !_componentProvinceIDs.Contains(z)).ToList();
        }

        public void WarAI(ref List<Culture> cultures, List<Empire> empires, List<ProvinceObject> provs, ref List<Religion> religions, ref System.Random rnd, ref Date date)
        {
            List<Empire> warredEmpires = opinions.Where(x => x.Value._isWar).Select(y => empires[y.Key]).ToList();
            if (warredEmpires.Count <= 0) { return; }

            if(occupationCooldown > 0) { occupationCooldown--; return; } //Reset the cooldown between attacks

            foreach (Empire target in warredEmpires)
            {
                //If either does not exist or if no provinces are adjacent anymore
                if (!target._exists || _exists == false || !target.ReturnAdjacentIDs(ref provs, true).Any(x => _componentProvinceIDs.Contains(x)) || !ReturnAdjacentIDs(ref provs, true).Any(x => target._componentProvinceIDs.Contains(x)))
                {
                    if (target.opinions.ContainsKey(_id)) { target.opinions[_id]._isWar = false; }
                    if (opinions.ContainsKey(target._id)) { opinions[target._id]._isWar = false; }
                }
                else
                {
                    if (rnd.NextDouble() < Math.Min(0.7f, Math.Max(0.2f, curRuler.rulerPersona["per_Attack"]/2.0f)))
                    {

                        (ProvinceObject thisTarget, float riskRewardVal, bool belowChance, bool aboveRisk)[] targetables = ReturnAdjacentIDs(ref provs, true).Where(x => target._componentProvinceIDs.Contains(x) && Actions.CanConquer(provs[x],this,ref provs)).Select(y=>(provs[y],0.0f,false,false)).ToArray();

                        int tCount = targetables.Count();

                        if (targetables.Count() != 0)
                        {
                            for (int i = 0; i < tCount; i++) //Append targetables values
                            {
                                (int attackerMaxCost, int defenderMaxCost, float attackerVicChance) predictedBattleResults = Actions.BattleStats(targetables[i].thisTarget, this, provs);
                                if (curRuler.GetRulerMilitaryRisk(this) > predictedBattleResults.attackerMaxCost) { targetables[i].aboveRisk = true; }
                                if (predictedBattleResults.attackerVicChance < (1.0f - curRuler.rulerPersona["per_Risk"])) { targetables[i].belowChance = true; }
                                targetables[i].riskRewardVal = curRuler.ValueRiskRatio(ReturnIndProvVal(targetables[i].thisTarget, provs), predictedBattleResults.attackerMaxCost);
                            }


                            //If it doesnt encompass the entire set, remove all where chance is too low
                            if (targetables.Where(x => x.belowChance == true).Count() < targetables.Count()) { targetables = targetables.Where(x => x.belowChance != true).ToArray(); }

                            //If this doesnt encompass the entire set, remove all where the risk is more than the ruler wants to afford
                            if (targetables.Where(x => x.aboveRisk == true).Count() < targetables.Count()) { targetables = targetables.Where(x => x.aboveRisk != true).ToArray(); }

                            if (targetables.Count() > 0)
                            {
                                targetables.OrderByDescending(x => x.riskRewardVal);

                                //TODO maybe add some randomisation here based on ruler personality?

                                //Set a cooldown for each nation relating to their time for the transfer of power
                                occupationCooldown = Math.Max(occupationCooldown, Convert.ToInt32(Math.Ceiling(200.0f * ReturnPopScore(targetables[0].thisTarget, provs))));
                                targetables[0].thisTarget._ownerEmpire.occupationCooldown = Math.Max(targetables[0].thisTarget._ownerEmpire.occupationCooldown, Convert.ToInt32(Math.Ceiling(200.0f * ReturnPopScore(targetables[0].thisTarget, provs))));

                                if (Actions.ConquerLand(targetables[0].thisTarget,this,ref provs)) //Evaluate battle
                                {
                                    targetables[0].thisTarget.updateText = "Occupied by " + _empireName;
                                    Debug.Log(_id + " TOOK LAND");
                                }
                                else
                                {
                                    targetables[0].thisTarget.updateText = "Defended by " + targetables[0].thisTarget._ownerEmpire._empireName;
                                    Debug.Log(_id + " FAILED TO TAKE LAND");
                                }

                            }
                        }
                        else
                        {
                            Debug.Log("NO TARGETABLES");
                        }
                    }
                }
            }

        }

        private void AI(List<string> actBuffer, ref List<Culture> cultures, List<Empire> empires, List<ProvinceObject> provs, ref List<Religion> religions, ref System.Random rnd, ref Date date)
        {
            foreach (string newAct in actBuffer) //iterate through listed actions
            {
                switch (newAct)
                {
                    case "per_Idle":
                        return;
                    case "per_Colonize":
                        {
                            if (BlockIfWar()) { break; }
                            (bool canColonise, List<(int targetID, float valueRisk)> targets) canColony = curRuler.CanColoniseAdjs(ref provs, this);
                            if (canColony.canColonise)
                            {
                                Debug.Log(_id + " COLONISING");
                                if(canColony.targets.Count <= 0) { break; }
                                int[] targets = canColony.targets.OrderByDescending(x => x.valueRisk).Select(y=>y.targetID).ToArray(); //Highest value targets

                                foreach(int t in targets)
                                {
                                    if (DiplomaticConsiderations((Func< Dictionary<Empire, (int value, int time, string type)>>) delegate { return Act.Actions.ColonyOpinionMods(provs[t], this, provs, empires); },2)) //Check all diplomatic considerations
                                    {
                                        Actions.ColonizeLand(provs[t], this, provs, ref empires, ref date);
                                        provs[t].updateText = "Colonised by " + _empireName;
                                        return;
                                    }
                                }
                                break;
                            }
                            break;
                        }
                    case "per_DevelopTech":
                        {
                            if (rnd.Next(0, 6-Math.Min(3,_componentProvinceIDs.Count)) == 1)
                            {
                                Debug.Log(_id + " DEVELOPING TECH");
                                string devTech = curRuler.ReturnNextTech(this, ref rnd);
                                ref int techVar = ref TechStringToVar(devTech);
                                techVar++;

                                if(devTech == "Diplomacy") //Diplomacy tech boosts grants a bonus to all nations with opinions of this nation
                                {
                                    List<int> opEmpires = opinions.Keys.ToList();
                                    List<Empire> empMods = empires.Where(x => opEmpires.Contains(x._id)).ToList();
                                    
                                    foreach(Empire e in empMods)
                                    {
                                        Actions.AddNewModifier(e, this, 1000, 10, (date.day, date.month, date.year), "DIPTECH");
                                    }
                                }
                                else if(devTech == "Culture") //Culture tech devs grant a bonus to all nations with shared culture
                                {
                                    List<int> opEmpires = opinions.Keys.ToList();
                                    List<Empire> empMods = empires.Where(x => opEmpires.Contains(x._id)).ToList();

                                    foreach (Empire e in empMods)
                                    {
                                        Actions.AddNewModifier(e, this, 1000, 5, (date.day, date.month, date.year), "CULTECH");
                                    }
                                }

                                provs[_componentProvinceIDs[0]].updateText = "Developed " + devTech + " level " + techVar;
                                return;
                            }
                            else
                            {
                                break;
                            }
                        }
                    case "per_LearnTech":
                        {
                            (int mMil, int mEco, int mDip, int mLog, int mCul, Empire? hTech) maxVals = LearnableTechs(ref empires, ref cultures, provs); //Learn from any non-rival non-fearful adjacents
                            if (!IsLesser((milTech, ecoTech, dipTech, logTech, culTech),(maxVals.mMil,maxVals.mEco,maxVals.mDip,maxVals.mLog,maxVals.mCul))) { break; } //If already max tech, move to next action.

                            (string biggestDif, int dif) techWithDif = ("",-1);

                            if(maxVals.mMil > milTech) { if ((maxVals.mMil-milTech) > techWithDif.dif) { techWithDif.biggestDif = "Military"; techWithDif.dif = (maxVals.mMil- milTech); } }
                            if (maxVals.mEco > ecoTech) { if ((maxVals.mEco-ecoTech) > techWithDif.dif) { techWithDif.biggestDif = "Economic"; techWithDif.dif = (maxVals.mEco-ecoTech); } }
                            if (maxVals.mDip > dipTech) { if ((maxVals.mDip-dipTech) > techWithDif.dif) { techWithDif.biggestDif = "Diplomacy"; techWithDif.dif = (maxVals.mDip-dipTech); } }
                            if (maxVals.mLog > logTech) { if ((maxVals.mLog-logTech) > techWithDif.dif) { techWithDif.biggestDif = "Logistics"; techWithDif.dif = (maxVals.mLog-logTech); } }
                            if (maxVals.mCul > culTech) { if ((maxVals.mCul-culTech) > techWithDif.dif) { techWithDif.biggestDif = "Culture"; techWithDif.dif = (maxVals.mCul-culTech); } }

                            if (Actions.UpdateTech(ref empires, _id, techWithDif.biggestDif)) //Attempt update (add 1 to tech)
                            {
                                provs[_componentProvinceIDs[0]].updateText = "Learned " + techWithDif.biggestDif + " level " + TechStringToVar(techWithDif.biggestDif);

                                if(maxVals.hTech != null)
                                {
                                    Act.Actions.AddNewModifier(this, maxVals.hTech, 1825, 10, (date.day, date.month, date.year), "LEARNED");
                                }
                                return;
                            }
                            else
                            {
                                break;
                            }
                        }
                    case "per_SpreadReligion":
                        {
                            if (BlockIfWar()) { break; }
                            if (!curRuler.hasAdoptedRel) //If they have not changed the state religion in their lifetime, they may change the empire religion
                            {
                                List<Religion> relCounts = new List<Religion>() { };
                                foreach (int x in _componentProvinceIDs)
                                {
                                    if (provs[x]._localReligion != null)
                                    {
                                        relCounts.Add(provs[x]._localReligion);
                                    }
                                }
                                if (relCounts.Count <= 0) { break; }
                                Religion majorityRel = relCounts.GroupBy(x => x).OrderByDescending(y => y.Count()).Select(z => z.Key).First(); //Select the most common rel in the set
                                curRuler.hasAdoptedRel = true;
                                if (majorityRel == stateReligion) { break; } //If the state religion has not changed, do not update state religion. 

                                Act.Actions.SetStateReligion(ref provs, empires, religions, _id, majorityRel._id,ref date);
                                provs[_componentProvinceIDs[0]].updateText = "Converted to " + majorityRel._name;
                                provs[_componentProvinceIDs[0]]._localReligion = stateReligion;
                                Debug.Log(_id + " ADOPTED RELIGION " + majorityRel._id);

                                return;
                            }
                            else
                            {
                                if (stateReligion != null)
                                {
                                    Debug.Log("ATTEMPTED SPREAD REL");
                                    bool relPolled = false; //Check if any religions were polled. If non were polled, choose next action
                                    foreach (int x in _componentProvinceIDs)
                                    {
                                        if (provs[x]._localReligion != stateReligion)
                                        {
                                            relPolled = true;
                                            int rndVal = rnd.Next(0, 100);

                                            if (provs[x]._localReligion == null)
                                            {
                                                if (rndVal <= 33)//1/3 of changing a religion for a non-religious province
                                                {
                                                    Act.Actions.SetReligion(ref provs, ref religions, x, stateReligion._id);
                                                    provs[x].updateText = "Adopted state religion";
                                                }
                                            }
                                            else
                                            {
                                                if (rndVal <= 20)
                                                { //1/5 of changing religion for religious province
                                                    Act.Actions.SetReligion(ref provs, ref religions, x, stateReligion._id);
                                                    provs[x].updateText = "Adopted state religion";
                                                }
                                            }
                                        }
                                    }

                                    if (!relPolled) { break; }
                                    else { return; }

                                }
                            }
                        }
                        break;
                    case "per_IncreaseOpinion":
                        {
                            if (BlockIfWar()) { break; }
                            if (opinions.Count > 0)
                            {
                                int[] targetEmpireIDs = opinions.Where(y=>!y.Value._rival && y.Value._isRelevant).Select(x => x.Key).ToArray();
                                int tCount = targetEmpireIDs.Count();

                                for (int i = 0; i < tCount; i++) //Shuffle set
                                {
                                    int tStore = targetEmpireIDs[i];
                                    int target = rnd.Next(0, tCount);
                                    targetEmpireIDs[i] = targetEmpireIDs[target];
                                    targetEmpireIDs[target] = tStore;
                                }

                                foreach (int t in targetEmpireIDs)
                                {
                                    if (DiplomaticConsiderations((Func<Dictionary<Empire, (int value, int time, string type)>>)delegate { return Act.Actions.EnvoyMod(empires[t],empires); },0)) //Check all diplomatic considerations
                                    {
                                        Act.Actions.DiplomaticEnvoy(empires[t], this, (date.day, date.month, date.year), ref rnd, ref empires);
                                        
                                        return;
                                    }
                                }
                            }
                        }
                        break;
                    case "per_DeclareWar":
                        //TODO add ending war and truce modifier
                        {
                            if (BlockIfWar()) { break; }
                            if(curMil > curRuler.GetRulerMilitaryRisk(this)) //If the ruler is above their risk threshold
                            {

                                Dictionary<Empire, float> potentialWarTargets = WarTargets(empires, cultures, provs);

                                if (potentialWarTargets.Count <= 0) { break; }
                                else
                                {
                                    float maxScore = potentialWarTargets.Values.Max();
                                    Empire target = potentialWarTargets.First(x => x.Value >= maxScore).Key;

                                    Debug.Log(_id + " DECLARES WAR ON " + target._id);

                                    provs[_componentProvinceIDs[0]].updateText = "Declared war on " + target._empireName;
                                    provs[target._componentProvinceIDs[0]].updateText = "Declared war on by " + _empireName;

                                    target.opinions[_id]._isWar = true;
                                    opinions[target._id]._isWar = true;

                                    Actions.AddNewModifier(target, this, 3650, -50, (date.day, date.month, date.year), "WAR");
                                    Actions.AddNewModifier(this, target, 3650, -50, (date.day, date.month, date.year), "WAR");
                                    return;

                                }
                            }
                        }
                        break;
                    default: 
                        {
                            break;
                        }
                }
            }
        }

        public bool BlockIfWar()
        {
            if (opinions.Count > 0)
            {
                if (opinions.Any(x => x.Value._isWar))
                {
                    return true;
                }
            }
            return false;
        }

        private bool DiplomaticConsiderations(Func<Dictionary<Empire, (int value, int time, string type)>> compAction, int defaultWeighting) //Checks if the ruler is willing to take this action depending on its opinion stats
        {
            Dictionary<Empire, (int value, int time, string type)> impactedNations = compAction(); //invoke the specified method
            float negOpinion = 0;
            float posOpinion = 0;

            if(impactedNations.Count <= 0) { return true; }

            foreach(KeyValuePair<Empire, (int value, int time, string type)> imp in impactedNations)
            {

                if(opinions.ContainsKey(imp.Key._id))
                {
                    Opinion op = opinions[imp.Key._id];

                    int weightVal = Math.Abs(imp.Value.value);
                    bool isHit = false; //checks if any opinion modifiers were found
                    if (imp.Value.value < 0) //If this would give a negative opinion penalty for a target
                    {
                        if (op._fear) { isHit = true; negOpinion += (weightVal * (1-curRuler.rulerPersona["per_Risk"])); }
                        else if (op._rival) { isHit = true; posOpinion += (weightVal * curRuler.rulerPersona["per_Insult"]); }
                        else if (op._ally) { isHit = true; posOpinion += (weightVal * 0.2f); } //Stepping on allies toes has a minimal impact
                    }
                    else if(imp.Value.value > 0)
                    {
                        if (op._ally) { isHit = true; posOpinion += weightVal * curRuler.rulerPersona["per_IncreaseOpinion"]; }
                        else if(op._fear) { isHit = true; posOpinion += weightVal * (1 - curRuler.rulerPersona["per_Risk"]); } //If this would satisfy a feared empire, less risky nations will attempt to please their feared nation.
                    }

                    if(isHit)
                    {
                        posOpinion += defaultWeighting; //append the default value. This means only significant changes apply, as well as the desire to do this action is accounted for
                    }
                }

            }

            if (posOpinion < negOpinion) { return false; }
            else { return true; }
        }
        private bool IsLesser((int mMil, int mEco, int mDip, int mLog, int mCul) subject, (int mMil, int mEco, int mDip, int mLog, int mCul) comparitor) //Returns if subject is less than comparitor in any categories
        {
            if(subject.mMil < comparitor.mMil) { return true; }
            if(subject.mEco < comparitor.mEco) { return true; }
            if(subject.mDip < comparitor.mDip) { return true; }
            if(subject.mLog < comparitor.mLog) { return true; }
            if(subject.mCul < comparitor.mCul) { return true; }
            return false;
        }

        private ref int TechStringToVar(string techName)
        {
            switch (techName)
            {
                case "Military":
                    return ref milTech;
                case "Economic":
                    return ref ecoTech;
                case "Diplomacy":
                    return ref dipTech;
                case "Logistics":
                    return ref logTech;
                case "Culture":
                    return ref culTech;
                default: //Should not occur but defaults to miltech
                    return ref milTech;
            }
        }
        private (int mMil, int mEco, int mDip, int mLog, int mCul, Empire? hTechTotal) LearnableTechs(ref List<Empire> empires, ref List<Culture> cultures, List<ProvinceObject> provs)
        {
            (int mMil, int mEco, int mDip, int mLog, int mCul, Empire? hTechTotal) mTechs = (1, 1, 1, 1, 1,null);
            List<int> adjIds = ReturnAdjacentIDs(ref provs, true);
            List<ProvinceObject> adjacentProvsDiffEmp = provs.Where(x => adjIds.Contains(x._id) && x._ownerEmpire != null && x._ownerEmpire != this).ToList();
            List<Empire> adjEmpires = adjacentProvsDiffEmp.Select(x => x._ownerEmpire).Distinct().ToList();

            int prevTechTotal = -1;

            foreach (Empire x in adjEmpires)
            {

                if (x.opinions.ContainsKey(_id)) //Must have an opinion
                {
                    Opinion tOp = x.opinions[_id];

                    if (x.milTech > mTechs.mMil) { mTechs.mMil = x.milTech; }
                    if (x.ecoTech > mTechs.mEco) { mTechs.mEco = x.ecoTech; }
                    if (x.dipTech > mTechs.mDip) { mTechs.mDip = x.dipTech; }
                    if (x.logTech > mTechs.mLog) { mTechs.mLog = x.logTech; }
                    if (x.culTech > mTechs.mCul) { mTechs.mCul = x.culTech; }

                    if(x.ReturnTechTotal() > prevTechTotal)
                    {
                        mTechs.hTechTotal = x;
                        prevTechTotal = x.ReturnTechTotal();
                    }
                }
            }

            return mTechs;
        }

        private Dictionary<Empire,float> WarTargets(List<Empire> empires, List<Culture> cultures, List<ProvinceObject> provs) //Potential targets for war. Empire vs Score
        {
            Dictionary<Empire, float> targetEmpires = new Dictionary<Empire, float>() { };
            List<Empire> potentialTargets = new List<Empire>();
            List<int> adjIDs = ReturnAdjacentIDs(ref provs, true).Where(x=>provs[x]._biome != 0).ToList();
            potentialTargets = opinions.Where(x => !x.Value._isWar && adjIDs.Any(y => empires[x.Key]._componentProvinceIDs.Contains(y)) && !x.Value._ally && !x.Value._fear && empires[x.Key]._exists && !x.Value.modifiers.Any(y=> y.typestring == "TRUCE")).Select(z=>empires[z.Key]).ToList(); //Get all adjacent non-allies non feared

            if(potentialTargets.Count == 0) { return targetEmpires; } //Return empty set if there are no potential targets

            foreach(Empire target in potentialTargets)
            {
                float score = 0; //Targeting score
                score += target._componentProvinceIDs.Select(x => ReturnProvPersonalVal(provs[x], provs)).Sum() / target._componentProvinceIDs.Count();

                {
                    List<ProvinceObject> enemyBorderProvinces = target._componentProvinceIDs.Where(x => adjIDs.Contains(x)).Select(y => provs[y]).ToList(); //Attackable provinces by this empire
                    List<ProvinceObject> myBorderProvinces = new List<ProvinceObject>();
                    {
                        List<int> myAdjIDs = target.ReturnAdjacentIDs(ref provs, true).Where(x => provs[x]._biome != 0).ToList();
                        myBorderProvinces = _componentProvinceIDs.Where(x => myAdjIDs.Contains(x)).Select(y => provs[y]).ToList();
                    }

                    foreach(ProvinceObject enemyBorder in enemyBorderProvinces) //Evaluate potential costs for each location of enemy
                    {
                        (int attackerMaxCost, int defenderMaxCost, float attackerVicChance) = Actions.BattleStats(enemyBorder, this, provs); //Theoretical battle stats for each battle
                        if(attackerMaxCost < curRuler.GetRulerMilitaryRisk(this)) //Foreach province they are willing to start by attacking
                        {
                            float provVal = Math.Min(1,ReturnProvPersonalVal(enemyBorder, provs)); //Adds the personal value again to emphasise the impacts of border provinces
                            score += curRuler.rulerPersona["per_Risk"] > attackerVicChance ? (1+attackerVicChance) * provVal : (1-attackerVicChance) * -provVal;
                        }
                    }

                    foreach (ProvinceObject myBorder in myBorderProvinces) //Evaluate potential costs for each location of this empire
                    {
                        (int attackerMaxCost, int defenderMaxCost, float attackerVicChance) = Actions.BattleStats(myBorder, this, provs); //Theoretical battle stats for each battle
                        float provVal = Math.Min(1, ReturnProvPersonalVal(myBorder, provs)); //Adds the personal value again to emphasise the impacts of border provinces
                        if (defenderMaxCost < curRuler.GetRulerMilitaryRisk(this)) { score += (1 + attackerVicChance) * -provVal; }
                        else
                        {
                            score += curRuler.rulerPersona["per_Risk"] > (1 - attackerVicChance) ? (1 - attackerVicChance) * provVal : (1 + attackerVicChance) * -provVal;
                        }
                    }
                }

                //Opinion modifiers
                float opMod = 1;
                if (opinions[target._id]._rival) { opMod+=1f; } 
                else if(opinions[target._id].lastOpinion < 0) { opMod += 0.1f; }
                else if(opinions[target._id].lastOpinion > 50) { opMod -= 0.25f; }

                if(curMil > target.curMil * (1.5f-curRuler.rulerPersona["per_Risk"])) { opMod += 0.25f ; }
                else { opMod -= 0.5f; }

                if(maxMil > target.maxMil * (1.5f - curRuler.rulerPersona["per_Risk"])) { opMod += 0.1f; }
                else { opMod -= 0.1f; }

                if (ExpectedMilIncrease(ref provs) > target.ExpectedMilIncrease(ref provs) * (1.5f - curRuler.rulerPersona["per_Risk"])) { opMod += 0.3f; }
                else { opMod -= 0.3f; }

                targetEmpires.Add(target, score * opMod);
            }

            //Remove based on diplomatic concerns
            foreach (Empire emp in targetEmpires.Keys.ToArray())
            {
                if (!DiplomaticConsiderations((Func<Dictionary<Empire, (int value, int time, string type)>>)delegate { return Act.Actions.WarOpinionMods(provs[emp._componentProvinceIDs[0]], this, provs, empires); },5))
                {
                    Debug.Log("DIPLO STOPPED WAR");
                    targetEmpires.Remove(emp);
                }
            }

            //Remove lowest values and then append opinion considerations

            if (targetEmpires.Count >= 4)
            {
                float midScore = 0;
                {
                    List<float> vals = targetEmpires.Values.ToList();
                    vals.Sort();
                    midScore = vals[Convert.ToInt32(Math.Floor((float)(targetEmpires.Count) / (2.0f)))]; //Get the median value of the set
                }
                
                foreach (Empire tEmp in targetEmpires.Keys.ToArray())
                {
                    if(targetEmpires[tEmp] < midScore) { targetEmpires.Remove(tEmp); }
                }
            }

            return targetEmpires;

        }
        public Dictionary<int, float> ReturnTargetNormValues(List<int> tProvs, List<ProvinceObject> provs, bool includeOwned) //0-1 value of province values
        {
            Dictionary<int,float> pVals = new Dictionary<int, float>();

            List<ProvinceObject> incProvs = new List<ProvinceObject>();

            foreach (int prov in tProvs)
            {
                if(!includeOwned || provs[prov]._ownerEmpire != null)
                {
                    incProvs.Add(provs[prov]);
                }
            }

            float min = incProvs.Min(x => ReturnProvPersonalVal(x, provs));
            float max = incProvs.Max(x => ReturnProvPersonalVal(x, provs));

            foreach(ProvinceObject t in incProvs)
            {
                pVals[t._id] = (ReturnProvPersonalVal(t,provs) - min) / (max - min);
            }

            return pVals;
        }

        public void PollOpinions(ref Date curDate, ref List<Empire> empires, ref List<ProvinceObject> provs, ref List<Culture> cultures)
        {
            List<Empire> targets = provs.Where(x => x._cultureID == _cultureID && x._ownerEmpire != null).Select(y => y._ownerEmpire).ToList(); //Get all empires in cultures
            {
                List<int> adjIds = ReturnAdjacentIDs(ref provs, true);

                foreach(int i in adjIds)
                {
                    if(provs[i]._ownerEmpire != null)
                    {
                        if(!targets.Contains(provs[i]._ownerEmpire))
                        {
                            targets.Add(provs[i]._ownerEmpire); //If adjacent add to set of targets
                        }
                    }
                }
            }

            targets = targets.Where(x => !opinions.ContainsKey(x._id) && x._id != _id).ToList(); //Remove empires with existing opinions and any potential targets to self
            targets = targets.Distinct().ToList(); //Make targets distinct

            foreach(Empire x in targets)
            {
                opinions.Add(x._id,new Opinion(this, x)); //Add to set of opinions
            }

            foreach(Opinion op in opinions.Values.ToArray())
            {
                if (op.IsOpinionValid(this, ref empires, ref provs))
                {
                    op.RecalculateOpinion(this, ref empires, ref curDate,ref provs);
                }
                else
                {
                    opinions.Remove(op.targetEmpireID);
                }
            }
        }
    }

    public class Ruler
    {
        public string fName = "NULL";
        public string lName = "NULL";
        public int age;
        public (int day, int month) birthday;
        public (int day, int month, int age) deathday;
        private List<string> nameBuffer = new List<string>() { };
        private string rTitle = "NULL";
        public bool hasAdoptedRel; //If they have adopted a religion in their lifetime, they may not do so again.

        //Personality values
        public Dictionary<string, float> rulerPersona = new Dictionary<string, float>() {
            {"per_DeclareWar",0 },
            {"per_DevelopTech",0},
            {"per_LearnTech",0 },
            {"per_Idle",0 },
            {"per_SpreadReligion",0 },
            {"per_IncreaseOpinion",0 },
            {"per_SpawnRebellion",0 },
            {"per_Colonize",0 },
            {"per_Teach",0 },
            {"per_Attack",0 },
            {"per_Risk",0 }, //Willingness to lose military for colony/willingness to fight low chance battles
            {"per_Insult",0 } //Willingness to act in rude ways to rivals
        };

        public static Dictionary<string,(string low1,string low2,string high1,string high2)> personalityNames = new Dictionary<string, (string low1, string low2, string high1, string high2)>(){
            {"per_DeclareWar",("Peaceful","Peacemaker","Aggressive","Warmonger")},
            {"per_DevelopTech",("Orthodox","Traditionalist","Educated","Philosopher")},
            {"per_LearnTech",("Stubborn","Conservative","Progressive","Erudite")},
            {"per_Idle",("Active","Activist","Indecisive","Hedonist") },
            {"per_SpreadReligion",("Sceptical","Atheist","Faithful","Saint") },
            {"per_IncreaseOpinion",("Shy","Isolationist","Gregarious","Diplomat") },
            {"per_SpawnRebellion",("Beloved","Lawmaker","Controversial","Tyrant") },
            {"per_Colonize",("Insular","Stray","Adventurous","Explorer") },
            {"per_Teach",("Absent","Misanthrope","Loving","Educator")},
            {"per_Attack",("Weak","Defender","Strong","Fighter")},
            {"per_Risk",("Craven","Coward","Brazen","Gambler")},
            {"per_Insult",("Humble","Sycophant","Spiteful","Loudmouth") },
        };

        public static List<string> techNames = new List<string>()
        {
            {"Military"},
            {"Economic"},
            {"Diplomacy"},
            {"Logistics"},
            {"Culture"},
        };

        //Tech focuses (0-5) Military, Economics, Diplomacy, Logistics, Culture
        public int[] techFocus = new int[2];
        public Ruler(Ruler previousRuler, Empire ownedEmpire, ref List<Culture> cultures, ref List<Empire> empires, ref List<ProvinceObject> provs, ref System.Random rnd)
        {
            //TODO add names and make previous ruler details not apply if last name is changed
            bool newDyn = false;
            hasAdoptedRel = false;

            if(rnd.Next(0,10) == 2 || previousRuler == null) //Dynasty Replacement chance
            {
                newDyn = true;
            }

            if (nameBuffer.Count == 0)
            {
                nameBuffer = cultures[ownedEmpire._cultureID].LoadNameBuffer(25, ref rnd); //Load 25 names to minimize the amount of file accessing done at a time
            }

            fName = nameBuffer[0];
            nameBuffer.RemoveAt(0);

            if(newDyn == true) //If replacing a ruler with a new dynasty
            {
                if (previousRuler != null)
                {
                    if(ownedEmpire._componentProvinceIDs.Count > 0) { provs[ownedEmpire._componentProvinceIDs[0]].updateText = "New Dynasty"; }

                    List<string> applicableDyn = empires.Where(t => t.opinions.ContainsKey(ownedEmpire._id)
                    && t._cultureID == ownedEmpire._cultureID && (t.stateReligion == ownedEmpire.stateReligion)
                    && t.curRuler.lName != previousRuler.lName && t._id != ownedEmpire._id
                    && t.opinions.First(x => x.Value.targetEmpireID == ownedEmpire._id).Value.lastOpinion > 20).Select(l=> l.curRuler.lName).ToList();
                    //Get possible married dynasties in culture group

                    if (applicableDyn.Count > 0)
                    {
                        if (rnd.Next(0, 25) == 1) //Take dynasty from within culture group
                        {
                            lName = applicableDyn[rnd.Next(0, applicableDyn.Count)]; //Get dynasty from other nation
                        }
                        else
                        {
                            lName = cultures[ownedEmpire._cultureID].LoadDynasty(ref empires, ref rnd);
                        }
                    }
                    else
                    {
                        lName = cultures[ownedEmpire._cultureID].LoadDynasty(ref empires, ref rnd);
                    }
                }
                else
                {
                    lName = cultures[ownedEmpire._cultureID].LoadDynasty(ref empires, ref rnd);
                }

                foreach (string personality in rulerPersona.Keys.ToArray()) //Entirely random stats
                {
                    rulerPersona[personality] = ((float)(rnd.Next(0, 101))) / 100.0f;
                }

                techFocus[0] = rnd.Next(0, 5);
            }
            else
            {
                if(rnd.Next(0,30) == 15)
                {
                    lName = cultures[ownedEmpire._cultureID].LoadDynasty(ref empires, ref rnd);
                }
                else
                {
                    lName = previousRuler.lName;
                }
                foreach (string personality in previousRuler.rulerPersona.Keys.ToArray()) //Stats based on variance from previous ruler
                {
                    float orient = (rnd.Next(0, 2) == 0 ? -1 : 1);
                    float offset = 1 - rnd.Next(0, (int)((0.001f+previousRuler.rulerPersona["per_Teach"]) * 100));
                    offset = Math.Max(1 - Math.Abs(Math.Min(Math.Abs(previousRuler.rulerPersona[personality] - 1), offset)),offset);
                    rulerPersona[personality] = Math.Min(Math.Max(0,(previousRuler.rulerPersona[personality] + (orient * offset)) ),1);
                }

                techFocus[0] = previousRuler.techFocus[rnd.Next(0, 2)];
            }

            //TODO maybe modify this - bad form
            while (true)
            {
                techFocus[1] = rnd.Next(0, 5);
                if (techFocus[1] != techFocus[0])
                {
                    break;
                }
            }

            //Person data
            birthday.month = rnd.Next(1, 13);
            int maxBday = Calendar.Calendar.monthSizes[((Calendar.Calendar.Months)(birthday.month)).ToString()];
            if(maxBday == -1)
            {
                maxBday = 28;
            }
            birthday.day = rnd.Next(1, maxBday + 1);

            deathday.month = rnd.Next(1, 13);
            int maxDDay = Calendar.Calendar.monthSizes[((Calendar.Calendar.Months)(birthday.month)).ToString()];
            if (maxDDay == -1)
            {
                maxDDay = 28;
            }
            deathday.day = rnd.Next(1, maxDDay + 1);
            age = rnd.Next(18, 50);

            deathday.age = rnd.Next(age+5, 72 + Convert.ToInt32(Math.Floor((float)(ownedEmpire.ReturnTechTotal()) / 50)));

            if(provs[ownedEmpire._componentProvinceIDs[0]].updateText == "")
            {
                provs[ownedEmpire._componentProvinceIDs[0]].updateText = "New Ruler";
            }
        }

        public (string,string) ReturnTechFocuses()
        {
            return (techNames[techFocus[0]], techNames[techFocus[1]]);
        }

        public string ReturnNextTech(Empire ownedEmpire, ref System.Random rnd) //Return the next tech to develop
        {
            List<(string, int)> techVals = new List<(string, int)>()
            {
                {("Military", ownedEmpire.milTech) },
                {("Economic", ownedEmpire.ecoTech) },
                {("Diplomacy", ownedEmpire.dipTech) },
                {("Logistics", ownedEmpire.logTech) },
                {("Culture",ownedEmpire.culTech) }
            };

            for(int i=0;i<techVals.Count;i++) //Shuffle to remove bias to original order
            {
                (string, int) tVal = techVals[i];
                int target = rnd.Next(0, 5);
                techVals[i] = techVals[target];
                techVals[target] = tVal;
            }
            techVals = techVals.OrderBy(x => x.Item2).Take(3).ToList(); //Ascending order

            (string, string) focuses = ReturnTechFocuses();

            //Return the lowest tech unless ruler focuses contains the other tech

            for(int i=0;i<3;i++)
            {
                if(focuses.Item1 == techVals[i].Item1) { return focuses.Item1; }
                if (focuses.Item2 == techVals[i].Item1) { return focuses.Item2; }
            }

            return techVals[0].Item1;


        }
        public string GetRulerPersonality()
        {
            if (rTitle == "NULL")
            {
                List<(string personality, float power)> PersonalityValues = new List<(string personality, float power)>();
                foreach (KeyValuePair<string, float> personality in rulerPersona)
                {
                    PersonalityValues.Add((personality.Key, personality.Value));
                }
                PersonalityValues = PersonalityValues.OrderBy(x => (float)Math.Abs(x.power - 0.5f)).ToList();

                string title = "";

                {
                    (string personality, bool IsHigh) persona1 = (PersonalityValues[0].personality, PersonalityValues[0].power <= 0.5f ? false : true);
                    (string personality, bool IsHigh) persona2 = (PersonalityValues[1].personality, PersonalityValues[1].power <= 0.5f ? false : true);

                    title += (persona2.IsHigh == true ? personalityNames[persona2.personality].high1 : personalityNames[persona2.personality].low1) + " ";
                    title += persona1.IsHigh == true ? personalityNames[persona1.personality].high2 : personalityNames[persona1.personality].low2;
                }

                rTitle = title;
            }
            return rTitle;
        }
        public Ruler()
        {

        }
        public List<string> CalculateRandomActsOrder(ref System.Random rnd) //Weights random acts
        {
            //TODO add calculation on if each act is even possible using ruler stats?

            List<string> acts = new List<string>();
            List<string> weightedRandomOrder = new List<string>();

            acts.AddRange(rulerPersona.Keys);
            //Remove non-action personality traits
            acts.Remove("per_Teach");
            acts.Remove("per_Risk");
            acts.Remove("per_Insult");

            int actCount = acts.Count;
            for(int i = 0;i<actCount;i++)
            {
                string store = acts[i];
                int index = rnd.Next(0, actCount);
                acts[i] = acts[index];
                acts[index] = store;
            }

            while(acts.Count > 0)
            {
                if (acts.Count == 1)
                {
                    weightedRandomOrder.Add(acts[0]);
                    break;
                }
                else
                {
                    float weightMax = rulerPersona.Where(x => acts.Contains(x.Key)).Select(y => y.Value).Max(); //Get max of floats in set
                    float multiplier = 10;
                    {
                        string[] weightSet = weightMax.ToString().Split('.');
                        if (weightSet.Length > 1)
                        {
                            multiplier = Convert.ToInt32(Math.Pow(10, weightSet[1].Length+1)); //Gets the number of decimal points
                        }
                    }

                    float rndFloat = (float)rnd.Next(0, Convert.ToInt32(weightMax * multiplier)) / multiplier; //Random number from 0 to max

                    for (int i = 0; i < acts.Count; i++)
                    {
                        if (rndFloat <= rulerPersona[acts[i]])
                        {
                            weightedRandomOrder.Add(acts[i]);
                            acts.Remove(acts[i]);
                            break;
                        }
                    }
                }
            }

            weightedRandomOrder = weightedRandomOrder.TakeWhile(x => x != "per_Idle").ToList(); //When an idle call is made, the ruler will always idle
            weightedRandomOrder.Add("per_Idle");

            return weightedRandomOrder;

        }

        public float GetRulerMilitaryRisk(Empire thisEmpire) //Amount ruler is willing to risk per cost
        {
            float perCost = (thisEmpire.maxMil / 2.0f);
            return (perCost * Math.Min(1.0f,Math.Max(0.1f,rulerPersona["per_Risk"]))); //At max risk, willing to risk 50% of the max mil. at min risk, willing to risk 5% of the max mil.
        }
        public float ValueRiskRatio(float value, float cost) //Returns the float value which the ruler values it at
        {
            return value / cost;
        }
        public (bool, List<(int, float)>) CanColoniseAdjs(ref List<ProvinceObject> provs, Empire thisEmpire) //Returns all colonisable items. ID and RiskRatio
        {
            (bool anyT, List<(int,float)> items) colonyTargets = (false, new List<(int,float)>() { });
            List<int> adjIds = thisEmpire.ReturnAdjacentIDs(ref provs, true); //Set of all adjacent provs
            Dictionary<int, float> ecoValsNorm = thisEmpire.ReturnTargetNormValues(adjIds, provs, false);

            foreach (int x in ecoValsNorm.Keys)
            {
                (bool isAble, int cost) colonyAbility = Act.Actions.CanColonize(provs[x], thisEmpire, ref provs);
                if(colonyAbility.isAble && colonyAbility.cost > GetRulerMilitaryRisk(thisEmpire)) { colonyAbility.isAble = false; }
                if (colonyAbility.isAble)
                {
                    colonyTargets.anyT = true;
                    colonyTargets.items.Add((x, ValueRiskRatio(ecoValsNorm[x],colonyAbility.cost)));
                }
            }

            if(colonyTargets.items.Count <= 0)
            {
                colonyTargets.anyT = false;
            }

            return colonyTargets;
        }
    }

    public class Opinion
    {
        public int targetEmpireID; //The empire target
        public List<Modifier> modifiers = new List<Modifier>();
        public float lastOpinion = 0;

        public bool _fear = false;
        public bool _ally = false;
        public bool _rival = false;
        public bool _isRelevant = true;

        public bool _isWar = false;
        public Opinion(Empire creatorEmpire, Empire targetEmpire)
        {
            targetEmpireID = targetEmpire._id;
        }
        public Opinion()
        {

        }
        public bool IsOpinionValid(Empire myEmpire, ref List<Empire> empires, ref List<ProvinceObject> provs) //Checks if the opinion should still be listed
        {
            if(!myEmpire._exists || !empires[targetEmpireID]._exists) { return false; } //If either is dead, opinion should be removed
            if(myEmpire._cultureID != empires[targetEmpireID]._cultureID) //If either are not in the same culture
            {
                List<int> myEmpireAdjs = myEmpire.ReturnAdjacentIDs(ref provs, true);
                List<int> targetComponents = empires[targetEmpireID]._componentProvinceIDs.ToList();

                if(!targetComponents.Any(x=>myEmpireAdjs.Contains(x))) //If no adjacent provinces
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return true;
            }
        }
        public float RecalculateOpinion(Empire myEmpire, ref List<Empire> empires, ref Date curDate, ref List<ProvinceObject> provs)
        {
            float overallOpinion = 0;
            Empire targetEmpire = empires[targetEmpireID];

            float maxorminOpinion = 150; //-150 to 150

            if (myEmpire.stateReligion == null && targetEmpire.stateReligion == null) { overallOpinion -= Math.Min(maxorminOpinion,5); } //Pagan opinion is negative
            else if(myEmpire.stateReligion != targetEmpire.stateReligion) { overallOpinion -= Math.Min(maxorminOpinion, 25); ; } //different religions penalty 
            else { overallOpinion += Math.Min(maxorminOpinion, 15); } //Same religion is positive opinion

            if (myEmpire.curRuler.lName == targetEmpire.curRuler.lName) { overallOpinion += Math.Min(maxorminOpinion, 75); ; } //Same dynasty opinion
            if(myEmpire._cultureID == targetEmpire._cultureID) { overallOpinion += Math.Min(maxorminOpinion, 10); }

            //Former opinion modifiers
            if (_ally) { overallOpinion += Math.Min(maxorminOpinion, 50);}
            if (_rival) { overallOpinion -= Math.Min(maxorminOpinion, 25); }

            foreach (Modifier x in modifiers.ToArray())
            {
                if (!x.IsValid(ref curDate)) { modifiers.Remove(x); } //If after cooldown date
                else
                {
                    overallOpinion += x.opinionModifier; //Add opinion modifier to overall opinion
                }
            }

            overallOpinion = Math.Max(-maxorminOpinion,Math.Min(maxorminOpinion, overallOpinion)); //Limit to opinion set
            lastOpinion = overallOpinion;

            _isRelevant = IsRelevant(ref empires, myEmpire, ref provs);
            _ally = DoesAlly(ref empires, myEmpire, ref provs);
            _fear = DoesFear(ref empires, myEmpire, ref provs);
            _rival = DoesRival(ref empires, myEmpire, ref provs);
            return overallOpinion;
        }
        public bool DoesFear(ref List<Empire> empires, Empire myEmpire, ref List<ProvinceObject> provs) //Returns if the current empire is afraid of the target empire. They will attempt to not act against an empire they fear
        {
            if (_ally) { return false; } //allies are not feared
            Empire target = empires[targetEmpireID];
            if(!target.opinions.ContainsKey(myEmpire._id)) { return false; }
            Opinion targetOpinion = target.opinions[myEmpire._id];
            if (targetOpinion == null) { return false; }
            if (targetOpinion._ally) { return false; }
            if (targetOpinion.lastOpinion > 50) { return false; } //A nation with >50% favour in the opponents opinion will never fear the other
            if(myEmpire.ExpectedMilIncrease(ref provs) < target.ExpectedMilIncrease(ref provs) / 2.0f || myEmpire.curMil < target.curMil / 2.0f || myEmpire.maxMil < target.maxMil / 2.0f) { return true; } //Large military advantages will scare an empire
            return false;
        }

        public bool DoesRival(ref List<Empire> empires, Empire myEmpire, ref List<ProvinceObject> provs) //Returns if the current empire considers the other empire a rival. they will act against eachother if true
        {
            if(_fear || _ally) { return false; } //feared nations and allies are not rivals
            Empire target = empires[targetEmpireID];
            if (!target.opinions.ContainsKey(myEmpire._id)) { return false; }
            Opinion targetOpinion = target.opinions[myEmpire._id];
            if (targetOpinion == null) { return false; }
            if (targetOpinion._ally) { return false; }
            if (targetOpinion.lastOpinion > 50 && lastOpinion > 50) { return false; } //Both must have low opinion to be rivals
            if(lastOpinion < -70) { return true; } //Always rivals if very low opinion

            int rivalScore = 0;
            {
                //If economic rivalry
                if (target._cultureID == myEmpire._cultureID)
                {
                    List<Empire> eByEconomy = empires.Where(x => x._cultureID == myEmpire._cultureID).OrderBy(y => y.percentageEco).ToList();

                    if(eByEconomy.Count >= 5)
                    {
                        int indexOfSelf = eByEconomy.IndexOf(myEmpire);
                        int indexOfEnemy = eByEconomy.IndexOf(target);

                        if(indexOfSelf - 1 == indexOfEnemy || indexOfSelf + 1 == indexOfEnemy) { rivalScore++; }
                    }
                }
            }
            if (Math.Abs(target.ReturnTechTotal() - myEmpire.ReturnTechTotal()) > 5) { rivalScore--; } //If the difference in tech is high, reduce rival score
            if(myEmpire.ReturnAdjacentIDs(ref provs, true).Count(x => target._componentProvinceIDs.Contains(x)) >= Math.Min(myEmpire._componentProvinceIDs.Count(),5)) { rivalScore++; } //If too many adjacents

            if (rivalScore > 0) { return true; }
            else { return false; }
        }
        public bool DoesAlly(ref List<Empire> empires, Empire myEmpire, ref List<ProvinceObject> provs) //Returns if the current empire considers the other an ally
        {
            if (!_isRelevant || _isWar) { return false; }
            Empire target = empires[targetEmpireID];
            if (!target.opinions.ContainsKey(myEmpire._id)) { return false; }
            Opinion targetOpinion = target.opinions[myEmpire._id];
            if (targetOpinion.lastOpinion > 100 || lastOpinion > 100) { return true; } //Very high opinion bonuses give automatic ally status

            if (targetOpinion.lastOpinion < 50 || lastOpinion < 50) { return false; }
            int allyScore = 0;
            if (_ally || targetOpinion._ally) { allyScore += 3; }
            {
                List<int> myOps = myEmpire.opinions.Where(x => (x.Value._fear || x.Value._rival) && x.Value.targetEmpireID != targetEmpireID).Select(y => y.Value.targetEmpireID).ToList();

                foreach (int x in myOps) //For each overlapping fear/rival or countered ally. The enemy of my enemy is my friend.
                {

                    if (target.opinions.ContainsKey(x))
                    {
                        Opinion tOp = target.opinions[x];

                        if (tOp._ally) { allyScore--; }
                        if (tOp._fear || tOp._rival) { allyScore++; }
                    }
                }
            }

            if (target.maxMil < myEmpire.maxMil * 0.75f) { allyScore -= 2; }
            if (target._cultureID == myEmpire._cultureID) { allyScore++; }


            if (allyScore > 0) { return true; } //If high opinion and high reason to ally
            return false;
        }

        public bool IsRelevant(ref List<Empire> empires, Empire myEmpire, ref List<ProvinceObject> provs) //If the empire considers this empire relevant in anyway
        {
            if(_fear || _ally || _rival) { return true; }
            Empire target = empires[targetEmpireID];
            if(target.maxMil > myEmpire.curMil * 0.75f) { return true; } //If military advantage at this current time
            if(lastOpinion > 50) { return true; }

            if (target._cultureID == myEmpire._cultureID)
            {
                List<Empire> eByEconomy = empires.Where(x => x._cultureID == myEmpire._cultureID).OrderBy(y => y.percentageEco).ToList();

                if (eByEconomy.Count >= 5)
                {
                    int indexOfSelf = eByEconomy.IndexOf(myEmpire);
                    int indexOfEnemy = eByEconomy.IndexOf(target);

                    if (indexOfSelf - 2 <= indexOfEnemy) { return true; } //Relevant economically
                }
            }

            if(myEmpire.curRuler.lName == target.curRuler.lName) { return true; }
            if(myEmpire.ReturnTechTotal() < target.ReturnTechTotal() - 5) { return true; } //If large enough technology difference
            return false;
        }

    }
    public class Modifier //Stores temporary modifications to opinions between empires
    {
        public string typestring = "";
        public (int day, int month, int year) timeOutDate; //When the modifier will end
        public float opinionModifier;
        public Modifier()
        {

        }
        public Modifier((int,int,int) tDate,float opinionChange, string type)
        {
            timeOutDate = tDate;
            opinionModifier = opinionChange;
            typestring = type;
        }
        public Modifier(int days, float opinionChange, ref Date curDate, string type)
        {
            Date tmpDate = Calendar.Calendar.ReturnDate(days, ref curDate);
            timeOutDate = (tmpDate.day, tmpDate.month, tmpDate.year);
            opinionModifier = opinionChange;
            typestring = type;
        }
        public bool IsValid(ref Date curDate)
        {
            Date tmpDate = new Date();
            tmpDate.day = timeOutDate.day;
            tmpDate.month = timeOutDate.month;
            tmpDate.year = timeOutDate.year;
 
            return !Calendar.Calendar.IsAfterDate(curDate, tmpDate);
        }

    }

}