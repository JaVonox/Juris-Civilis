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
        public List<Rebellion> rebels = new List<Rebellion>(); //Active rebels

        public float maxMil;
        public float curMil;
        public float leftoverMil;
        public int timeUntilNextUpdate;

        public int occupationCooldown; //The amount of time before this nation can attack again
        public int techPoints; //carried over chance for tech gain
        public float warExhaustion = 0; //War exhaustion is the represented value of how tired each nation is with a war. This always increases as a war continues - both when making gains and losses.
        
        public bool updateComponents; //bool for changing empire data
        public string lastCapital; //stores name of last capital 

        public Empire(string name, ProvinceObject startingProvince, ref List<Culture> cultures, ref List<Empire> empires, ref List<ProvinceObject> provs, ref System.Random rnd, ref Date curDate) //Constructor for an empire - used when a new empire is spawned
        {
            //Setting up a new empire

            int nextID = empires.FirstOrDefault(x => x._exists == false) != null ? empires.FirstOrDefault(x => x._exists == false)._id : empires.Count(); //Find first ID that is not in use
            _id = nextID;

            Empire[] removedEmpireOpinions = empires.Where(x => x.opinions.Keys.Contains(_id)).ToArray(); //Get opinions of previous empires if applicable
            foreach(Empire x in removedEmpireOpinions)
            {
                x.opinions.Remove(_id); //Remove the opinion from the other empire
            }

            _exists = true;

            _componentProvinceIDs.Add(startingProvince._id);
            startingProvince.NewOwner(this, empires); //Append self to set of owner
            _componentProvinceIDs.ListChanged += CheckEmpireExists;

            if (empires.Any(x => x._empireName == name && x._exists))
            {
                _empireName = "New " + name;
                _empireCol = new Color(((float)rnd.Next(0, 100)) / 100.0f, ((float)rnd.Next(0, 100)) / 100.0f, ((float)rnd.Next(0, 100)) / 100.0f);
            }
            else
            {
                _empireName = name;
                _empireCol = startingProvince._provCol;
            }

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
            techPoints = 0; //Start with low techpoints to prevent nations for instantly developing high tech
            warExhaustion = 25.0f;
            lastCapital = startingProvince._cityName;
            updateComponents = false;

            provs[_componentProvinceIDs[0]].updateText = "New Nation";
            provs[_componentProvinceIDs[0]]._unrest = -1; //Offset for the ruler penalty

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
        private void CheckEmpireExists(object sender, ListChangedEventArgs e) //Updates whenever list of components changes
        {
            if (_componentProvinceIDs.Count == 0) //If no more component provs, kill the empire
            {
                _exists = false;
                _cultureID = -1;
            }

            if (rebels.Count > 0)
            {
                List<Rebellion> fixRebels = rebels.Where(x => !x._provinceIDs.All(y => _componentProvinceIDs.Contains(y))).ToList();

                List<Rebellion> removed = new List<Rebellion>() { };
                foreach(Rebellion rel in fixRebels)
                {
                    List<int> provsToRemove = rel._provinceIDs.Where(y => !_componentProvinceIDs.Contains(y)).ToList();

                    foreach(int prov in provsToRemove)
                    {
                        if(!rel.RemoveIfApplicable(prov))
                        {
                            removed.Add(rel);
                        }
                    }

                }

                foreach(Rebellion remover in removed)
                {
                    rebels.Remove(remover);
                }
            }

            updateComponents = true;
        }

        private void UpdateNation(List<ProvinceObject> provs) //Updates capital if needed
        {
            if(_componentProvinceIDs.Count == 0)
            {
                _exists = false;
            }

            if (_exists)
            {
                
                if (provs[_componentProvinceIDs[0]]._cultureID != _cultureID) //Update the culture if the capital is no longer the correct culture
                {
                    _cultureID = provs[_componentProvinceIDs[0]]._cultureID;
                    Actions.IncreaseUnrest("cultureSwitch", this, provs, null);
                }

                if(provs[_componentProvinceIDs[0]]._cityName != _empireName && _componentProvinceIDs.Any(x=>provs[x]._cityName == _empireName)) //Make the original capital the capital again
                {
                    int capitalIndex = _componentProvinceIDs.IndexOf(_componentProvinceIDs.First(x => provs[x]._cityName == _empireName));

                    int zeroID = _componentProvinceIDs[0];
                    int capID = _componentProvinceIDs[capitalIndex];

                    _componentProvinceIDs[capitalIndex] = zeroID;
                    _componentProvinceIDs[0] = capID;

                    if (provs[_componentProvinceIDs[0]]._cultureID != _cultureID) //Update the culture if the capital is no longer the correct culture
                    {
                        _cultureID = provs[_componentProvinceIDs[0]]._cultureID;
                    }
                }
                else if(provs[_componentProvinceIDs[0]]._cityName != lastCapital) //If former capital has changed and the original capital does not exist
                {
                    List<ProvinceObject> newCapitalTargets = _componentProvinceIDs.Where(x => provs[x]._cultureID == _cultureID).Select(y => provs[y]).ToList(); //Get all provinces with appropriate culture
                    if(newCapitalTargets.Any(x=>x._population == Property.High)) { newCapitalTargets = newCapitalTargets.Where(x => x._population == Property.High).ToList(); } //Get high pop area
                    else if(newCapitalTargets.Any(x=>x._population == Property.Medium)) { newCapitalTargets = newCapitalTargets.Where(x => x._population == Property.Medium).ToList(); } //If no high pop, get medium pop

                    newCapitalTargets.OrderByDescending(x => ReturnProvPersonalVal(x, provs, false));

                    if (newCapitalTargets.Count > 0)
                    {
                        _componentProvinceIDs[0] = newCapitalTargets[0]._id;
                        Actions.IncreaseUnrest("capitalChanged", this, provs, null);
                    }
                }

                lastCapital = provs[_componentProvinceIDs[0]]._cityName;
            }
            updateComponents = false;
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
        public float ReturnIndProvVal(ProvinceObject tProv, List<ProvinceObject> provs) //prov value
        {
            float score = ReturnPopScore(tProv, provs);

            if (tProv._isCoastal) { score += 0.2f; }
            score += 0.1f * (float)(tProv._adjacentProvIDs.Count(x=>provs[x]._ownerEmpire != null));

            return score;
        }

        public float ReturnProvPersonalVal(ProvinceObject tProv, List<ProvinceObject> provs, bool includeAdjacenyBias) //Economic value + personal value
        {
            float score = ReturnIndividualEcoScore(tProv,provs,false);
            if(tProv._localReligion == stateReligion && stateReligion != null) { score += 0.3f; }
            if(tProv._biome == provs[_componentProvinceIDs[0]]._biome) { score += 0.1f; }
            score += (float)(_componentProvinceIDs.Count(x => tProv._adjacentProvIDs.Contains(x))) * 0.25f; //Adjacency bonus
            score += (tProv._adjacentProvIDs.Contains(_componentProvinceIDs[0])) ? 1f : 0;
            if(tProv._cultureID == _cultureID) { score += 0.3f; }
            if (includeAdjacenyBias && tProv._adjacentProvIDs.All(x => provs[x]._ownerEmpire == this || provs[x]._biome == 0)) { score += 20.0f; } //Extra value for enclaves + islands
            return score;
        }
        public int ReturnTechTotal()
        {
            return milTech + ecoTech + dipTech + logTech + culTech;
        }
        public float ReturnEcoScore(List<ProvinceObject> provinces, bool includeUnrest) //Get economics score for this empire
        {
            return ((float)(milTech + ecoTech + dipTech + logTech + culTech)/10.0f) + _componentProvinceIDs.Sum(x => ReturnIndividualEcoScore(provinces[x], provinces, includeUnrest));
        }

        public float ReturnEcoScoreFromSet(List<ProvinceObject> provinces, List<ProvinceObject> targets, bool includeUnrest)
        {
            return ((float)(milTech + ecoTech + dipTech + logTech + culTech) / 10.0f) + targets.Sum(x => ReturnIndividualEcoScore(x, provinces, includeUnrest));
        }
        public float ReturnAllPersonalVal(List<ProvinceObject> provinces) //Get economics score for this empire
        {
            return ((float)(milTech + ecoTech + dipTech + logTech + culTech) / 10.0f) + _componentProvinceIDs.Sum(x => ReturnProvPersonalVal(provinces[x], provinces,false));
        }
        public float ReturnIndividualEcoScore(ProvinceObject tProv, List<ProvinceObject> provs, bool includeUnrest) //Get economics score for a single province
        {
            float multiplier = 1; //Higher economic output if it is the same religion as the empire in question
            if(stateReligion == null) { multiplier -= 0.2f; }
            else if(stateReligion != null && tProv._localReligion != stateReligion) { multiplier -= 0.1f; }

            if(tProv._cultureID != _cultureID) { multiplier -= 0.1f; }
            if(includeUnrest)
            {
                multiplier -= Math.Min(0.8f,(tProv._unrest / 5.0f)); //Every point of unrest gives -0.2 to multiplier
                if (tProv._ownerEmpire.rebels.Any(x => x.IsContained(tProv._id))) { multiplier = 0f; } //Rebels give 0 Eco output
            }
            multiplier = Math.Max(0.0f, multiplier);
            return (ReturnIndProvVal(tProv,provs) * (1.0f + ((float)(ecoTech)/2.0f))) * multiplier;
        }
        public void RecalculateMilMax(List<ProvinceObject> provinces) //Finds the appropriate max military. Redone every time mil is recruited
        {
            maxMil = Convert.ToInt32(Math.Floor(Math.Min(100000000,
                (20.0f) + ((float)(milTech) * 70.0f) + (ReturnProvsVal(provinces) * 20.0f))));
        }
        public float ExpectedMilIncrease(ref List<ProvinceObject> provinces)
        {
            float milInc = ((float)Math.Round(1.0f+(float)Math.Min(100000000,
                ReturnEcoScore(provinces,true)),2));

            return milInc;
        }
        public float RebelMilIncrease(Rebellion reb, List<ProvinceObject> provinces)
        {
            List<ProvinceObject> rebelProvinces = reb._provinceIDs.Select(x => provinces[x]).ToList();

            float milInc = ((float)Math.Round(1.0f + (float)Math.Min(100000000,
            ReturnEcoScoreFromSet(provinces, rebelProvinces, false)), 2)); //Rebels get extra military from their occupied provinces

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

        public float ReturnWarValueCoefficient(bool isAll, bool isGreater, List<ProvinceObject> provs, ProvinceObject? provTaken, Empire target)
        {
            float t1;
            float t2;
            if(isAll)
            {
                t1 = ReturnAllPersonalVal(provs) / target.ReturnAllPersonalVal(provs);
                t2 = target.ReturnAllPersonalVal(provs) / ReturnAllPersonalVal(provs);
            }
            else
            {
                t1 = ReturnProvPersonalVal(provTaken, provs, false) / (target.ReturnProvPersonalVal(provTaken, provs, false)+ReturnProvPersonalVal(provTaken,provs,false));
                t2 = target.ReturnProvPersonalVal(provTaken, provs, false) / (ReturnProvPersonalVal(provTaken, provs, false)+ target.ReturnProvPersonalVal(provTaken, provs, false));
            }

            if(isGreater)
            {
                return t1 > t2 ? t1 : t2;
            }
            else
            {
                return t1 < t2 ? t1 : t2;
            }
        }
        public void TakeLoss(float losses, bool isWinner, bool isAttacker, Empire? enemy, ProvinceObject provTaken, ref List<ProvinceObject> provinces)
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
            leftoverMil -= leftoverLosses;

            if (IsInWar() && enemy != null) //Take war exhaustion proportional to losses
            {
                bool enemyIsRebels = enemy.opinions[_id].isRebels != null; //True if the enemy are rebels

                float multiplier = isWinner ? 0.5f : 1f;
                
                if(isWinner && isAttacker)
                {
                    opinions[enemy._id]._capturedProvinces += 1;
                }
                else if(!isWinner && !isAttacker)
                {
                    opinions[enemy._id]._capturedProvinces -= 1;
                }

                if (!enemyIsRebels)
                {
                    warExhaustion += losses * (multiplier + ReturnWarValueCoefficient(false, true, provinces, provTaken, enemy)); //Add exhaustion based on the value of the province and lost power
                }
                else //If the enemy is a rebel group
                {
                    if(isWinner || isAttacker) //If attacking or winning
                    {
                        warExhaustion += 0; //No exhaustion gain for beating rebels. This means an empire will only stop fighting rebels if they lose
                    }
                    else if(!isWinner) //If defending and lost
                    {
                        warExhaustion += losses * (multiplier + ReturnWarValueCoefficient(false, true, provinces, provTaken, enemy)) * 1.5f; //Increased war exhaustion from failing to win against rebels
                    }
                }
            }
        }
        public void PollForAction(ref Date currentDate, ref List<Culture> cultures, ref List<Empire> empires, ref List<ProvinceObject> provs, ref List<Religion> religions, ref System.Random rnd)
        {

            if (updateComponents) { UpdateNation(provs); }
            if (_exists) //If this empire is active
            {
                AgeMechanics(ref currentDate, ref cultures, ref empires, ref provs, ref rnd);
                if (IsInWar()) { WarAI(ref cultures, empires, provs, ref religions, ref rnd, ref currentDate); } //If in a war, actions will occur regardless of action chance. empires have their own cooldown for battles.
                else if(warExhaustion > 0) { warExhaustion-= Math.Max(10,(warExhaustion / 10.0f)); }
                if(warExhaustion < 0) { warExhaustion = 0; }
                if (CheckForUpdate(ref rnd))
                {

                    //Chance of ruler making an action
                    double actChance = Math.Min(0.15f, ((float)(dipTech) / 600.0f));
                    if (rnd.NextDouble() <= actChance)
                    {
                        AI(curRuler.CalculateRandomActsOrder(ref rnd), ref cultures, empires, provs, ref religions, ref rnd, ref currentDate);
                    }
                    if (rebels.Count > 0)
                    {
                        foreach (Rebellion r in rebels.ToArray())
                        {
                            actChance = Math.Min(0.15f, ((float)(dipTech) / 300.0f));
                            if (rnd.NextDouble() <= actChance)
                            {
                                RebelAI(r,ref cultures, empires, provs, ref religions, ref rnd, ref currentDate);
                            }
                            else
                            {
                                RebelAct(r, ref cultures, empires, provs, ref religions, ref rnd, ref currentDate);
                            }
                        }
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

            if (occupationCooldown > 0) { occupationCooldown--; } //lower the cooldown between attacks
        }
        public List<int> ReturnAdjacentIDs(ref List<ProvinceObject> provs, bool isDistinct)
        {
            List<int> pSet = provs.Where(x => x._ownerEmpire == this).SelectMany(y => y._adjacentProvIDs).ToList();
            if(isDistinct) { pSet = pSet.Distinct().ToList(); }
            return pSet.Where(z => !_componentProvinceIDs.Contains(z)).ToList();
        }
        public void ReduceUnrest(List<ProvinceObject> provs, ref System.Random rnd)
        {
            if (_exists)
            {
                if (rnd.NextDouble() < curRuler.rulerPersona["per_Calm"])
                {
                    List<ProvinceObject> unrestToReduce = _componentProvinceIDs.Where(x => provs[x]._unrest > 1.0f && (rebels.Count == 0 || !rebels.Any(y => y.IsContained(x)))).Select(y => provs[y]).ToList();

                    if (unrestToReduce.Count() > 0)
                    {
                        float unrestReduction = curRuler.rulerPersona["per_Calm"] + (0.03f * (float)(culTech));
                        float multiplier = (float)((float)(rnd.NextDouble()) / 2.0f);

                        unrestReduction = unrestReduction * multiplier;
                        int reduceTargets = rnd.Next(1, unrestToReduce.Count + 1);
                        foreach (ProvinceObject redProv in unrestToReduce)
                        {
                            if (reduceTargets <= 0) { break; }
                            redProv._unrest -= unrestReduction;
                            if (redProv._unrest < -2) { redProv._unrest = -2; }
                            reduceTargets--;
                        }
                    }
                }

                List<ProvinceObject>? disconnectedProvs = new List<ProvinceObject>();
                disconnectedProvs = ReturnDisconnectedLand(provs);

                if (disconnectedProvs != null)
                {

                    foreach (ProvinceObject leftProv in disconnectedProvs)
                    {
                        if (leftProv._unrest < GetUnrestCap())
                        {
                            leftProv._unrest += 0.2f; //Raise unrest per year if the province is not in connected land
                        }
                    }
                }
            }

        }
        public void AppendTechPoints(List<ProvinceObject> provs, ref List<Culture> cultures, List<Empire> empires, ref Date curDate) //Add tech points per month
        {
            //Add tech per month
            if (_exists && _componentProvinceIDs.Count > 0)
            {
                //Tech is learnt at 1000 when develop tech action is taken
                float newTechPoints = 1.0f + curRuler.rulerPersona["per_DevelopTech"] + curRuler.rulerPersona["per_LearnTech"]; //Sum of learning stats (1 to 3)
                newTechPoints += Math.Min(5.0f, _componentProvinceIDs.Sum(x => ReturnPopScore(provs[x], provs))); //Add total of popscore 

                techPoints += Convert.ToInt32(Math.Max(2,Math.Ceiling(newTechPoints))); 
                techPoints = Math.Min(techPoints, 500);
            }
        }
        public void RebelAI(Rebellion thisRebel, ref List<Culture> cultures, List<Empire> empires, List<ProvinceObject> provs, ref List<Religion> religions, ref System.Random rnd, ref Date date)
        {
            thisRebel.AddAnyAppropriate(provs, this, ref rebels, GetUnrestCap()); //And any relevant to the rebellion

            RebelType rType = thisRebel._type;

            switch(rType) //Increase unrest in constituients
            {
                case RebelType.Culture:
                    {
                        List<int> impacted = _componentProvinceIDs.Where(x => provs[x]._cultureID == Convert.ToInt32(thisRebel.targetType) && provs[x]._unrest >= (GetUnrestCap() / 3.0f)).ToList();
                    }
                    break;
                case RebelType.Religion:
                    {
                        List<int> impacted = _componentProvinceIDs.Where(x => provs[x]._localReligion != null && provs[x]._localReligion._id == Convert.ToInt32(thisRebel.targetType) && provs[x]._unrest >= (GetUnrestCap() / 3.0f)).ToList();
                    }
                    break;
                case RebelType.Revolution:
                    {
                        List<int> impacted = _componentProvinceIDs.Where(x=> provs[x]._unrest >= (GetUnrestCap() / 3.0f)).ToList();
                    }
                    break;
                case RebelType.Separatist: //Seperatists can only spread to adjacent same culture provinces - but adjacent provinces of any culture can join them manually.
                    {
                        List<int> impacted = _componentProvinceIDs.Where(x => provs[x]._unrest >= (GetUnrestCap() / 3.0f) && provs[x]._cultureID == Convert.ToInt32(thisRebel.targetType) && !_componentProvinceIDs.Contains(x)).ToList();
                    }
                    break;
                default:
                    break;
            }
        }

        public void RebelAct(Rebellion thisRebel, ref List<Culture> cultures, List<Empire> empires, List<ProvinceObject> provs, ref List<Religion> religions, ref System.Random rnd, ref Date date)
        {
            if(_exists == false)
            {
                rebels.Remove(thisRebel); //Delete rebellion
            }
            else
            { 
            thisRebel.pollCooldown--;
                if (thisRebel.pollCooldown <= 0)
                {
                    RebelType rType = thisRebel._type;

                    switch (rType) //Increase unrest in constituients
                    {
                        case RebelType.Culture:
                            {
                                if (thisRebel.rebelStrength >= curMil * 0.7f)
                                {
                                    MakeRebelCountry(thisRebel, ref cultures, empires, provs, ref religions, ref rnd, ref date);
                                }
                                else
                                {
                                    thisRebel.pollCooldown--;
                                }
                            }
                            break;
                        case RebelType.Religion:
                            {
                                if (thisRebel.rebelStrength >= curMil * 0.7f)
                                {
                                    MakeRebelCountry(thisRebel, ref cultures, empires, provs, ref religions, ref rnd, ref date);
                                }
                                else
                                {
                                    thisRebel.pollCooldown--;
                                }
                            }
                            break;
                        case RebelType.Revolution: //Revolutionaries just want to depose the ruler
                            {
                                if (thisRebel.rebelStrength >= curMil * 0.6f)
                                {
                                    //REBELS ATTACK HERE
                                    if (Actions.RevolutionaryRebelsAttack(this, thisRebel, ref provs, ref rnd)) //If successful revolt
                                    {
                                        foreach (int provID in thisRebel._provinceIDs)
                                        {
                                            provs[provID]._unrest *= 0.6f;
                                        }
                                        rebels.Remove(thisRebel); //Delete rebellion
                                        curRuler = new Ruler(curRuler, this, ref cultures, ref empires, ref provs, ref rnd); //New ruler is added to replace old ruler.
                                        provs[_componentProvinceIDs[0]].updateText = "Rebels Depose Ruler";
                                    }
                                    else
                                    {
                                        if (_componentProvinceIDs.Count == 0) { _exists = false; return; }
                                        foreach (int provID in thisRebel._provinceIDs)
                                        {
                                            provs[provID]._unrest *= 0.4f;
                                        }
                                        foreach (int id in _componentProvinceIDs) //Winning the fight reduces unrest everywhere
                                        {
                                            provs[id]._unrest *= 0.6f;
                                        }
                                        rebels.Remove(thisRebel); //Delete rebellion
                                        provs[_componentProvinceIDs[0]].updateText = "Rebels are Defeated";

                                    }
                                }
                                else
                                {
                                    thisRebel.pollCooldown--;
                                }
                            }
                            break;
                        case RebelType.Separatist: //Seperatists make a new country
                            {
                                if (thisRebel.rebelStrength >= curMil * 0.7f)
                                {
                                    MakeRebelCountry(thisRebel, ref cultures, empires, provs, ref religions, ref rnd, ref date);
                                }
                                else
                                {
                                    thisRebel.pollCooldown--;
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        public void MakeRebelCountry(Rebellion thisRebel, ref List<Culture> cultures, List<Empire> empires, List<ProvinceObject> provs, ref List<Religion> religions, ref System.Random rnd, ref Date date) //Used for spawning rebel countries (starting at war)
        {
            List<int> grantedProvs = thisRebel._provinceIDs;
            ProvinceObject capital = provs[grantedProvs[0]];
            Empire newEmpire = new Empire(capital._cityName, capital, ref cultures, ref empires, ref provs, ref rnd,ref date);

            foreach (int x in grantedProvs.ToArray())
            {
                if (!newEmpire._componentProvinceIDs.Contains(x))
                {
                    newEmpire._componentProvinceIDs.Add(x);
                    provs[x].NewOwner(newEmpire, empires);
                    provs[x]._unrest = 0; //Reset unrest
                }
            }

            if (empires.Count - 1 < newEmpire._id)
            {
                empires.Add(newEmpire);
            }
            else
            {
                empires[newEmpire._id] = newEmpire;
            }

            empires[newEmpire._id].milTech = milTech;
            empires[newEmpire._id].ecoTech = ecoTech;
            empires[newEmpire._id].dipTech = dipTech;
            empires[newEmpire._id].logTech = logTech;
            empires[newEmpire._id].culTech = culTech;
            empires[newEmpire._id].RecalculateMilMax(provs);
            empires[newEmpire._id].curMil = Math.Min(empires[newEmpire._id].maxMil, thisRebel.rebelStrength);

            //Start war between the two countries
            empires[newEmpire._id].opinions.Add(_id, new Opinion(newEmpire, this));
            opinions.Add(newEmpire._id, new Opinion(this, newEmpire));
            opinions[newEmpire._id].isRebels = thisRebel._type; //Set rebel type
            empires[newEmpire._id].opinions[_id].StartWarMetrics(ref empires, empires[newEmpire._id], ref provs);
            opinions[newEmpire._id].StartWarMetrics(ref empires, this, ref provs);  

            rebels.Remove(thisRebel); //Delete seperatist from set
        }

        public List<ProvinceObject> ReturnDisconnectedLand(List<ProvinceObject> provs) //Find set of land which is not connected to the capital
        {
            if (!_exists || _componentProvinceIDs.Count <= 1) { return null; }
            List<ProvinceObject> connectionProvs = new List<ProvinceObject>() { provs[_componentProvinceIDs[0]] };
            List<ProvinceObject> bankedProvs = new List<ProvinceObject>() { };

            while (connectionProvs.Count > 0) //Append each connection in the set
            {
                List<ProvinceObject> newConnects = connectionProvs[0]._adjacentProvIDs.Select(x => provs[x]).Where(y => y._ownerEmpire == this && !connectionProvs.Contains(y) && !bankedProvs.Contains(y)).ToList(); //Get all provinces not in set
                bankedProvs.Add(connectionProvs[0]);
                connectionProvs.RemoveAt(0);
                if (newConnects.Count > 0)
                {
                    connectionProvs.AddRange(newConnects);
                }
            }

            if(_componentProvinceIDs.Count == bankedProvs.Count) { return null; } //Return null if all provinces are connected

            return _componentProvinceIDs.Where(x => !bankedProvs.Contains(provs[x])).Select(y => provs[y]).ToList();

        }
        public void WarAI(ref List<Culture> cultures, List<Empire> empires, List<ProvinceObject> provs, ref List<Religion> religions, ref System.Random rnd, ref Date date)
        {
            List<Empire> warredEmpires = opinions.Where(x => x.Value._isWar).Select(y => empires[y.Key]).ToList();
            if (warredEmpires.Count <= 0) { return; }

            if(occupationCooldown > 0) { return; } //Reset the cooldown between attacks

            foreach (Empire target in warredEmpires)
            {
                if (!target.opinions.ContainsKey(_id)) { if (opinions.ContainsKey(target._id)) { opinions[target._id]._isWar = false; } } //Remove from set if the target has no longer got an opinion of this nation
                else
                {
                    if (!opinions[target._id].PollEndWar(ref empires, this, ref provs, ref date)) //Attempt to end war
                    {
                        if (!target._exists || _exists == false || !target.ReturnAdjacentIDs(ref provs, true).Any(x => _componentProvinceIDs.Contains(x)) || !ReturnAdjacentIDs(ref provs, true).Any(x => target._componentProvinceIDs.Contains(x)))
                        {
                            if (target.opinions.ContainsKey(_id)) { target.opinions[_id]._isWar = false; }
                            if (opinions.ContainsKey(target._id)) { opinions[target._id]._isWar = false; }
                        }
                        else
                        {
                            if (warExhaustion > opinions[target._id]._maxWarExhaustion) //If over the limit for war exhaustion and unable to surrender
                            {
                                Actions.IncreaseUnrest("warWeary", this, provs, null);
                            }

                            if (rnd.NextDouble() < Math.Min(0.7f, Math.Max(0.2f, curRuler.rulerPersona["per_Attack"] / 2.0f)))
                            {

                                (ProvinceObject thisTarget, float riskRewardVal, bool belowChance, bool aboveRisk)[] targetables = ReturnAdjacentIDs(ref provs, true).Where(x => target._componentProvinceIDs.Contains(x) && Actions.CanConquer(provs[x], this, ref provs)).Select(y => (provs[y], 0.0f, false, false)).ToArray();

                                int tCount = targetables.Count();

                                if (targetables.Count() != 0)
                                {
                                    for (int i = 0; i < tCount; i++) //Append targetables values
                                    {
                                        (int attackerMaxCost, int defenderMaxCost, float attackerVicChance) predictedBattleResults = Actions.BattleStats(targetables[i].thisTarget, this, provs);
                                        if (curRuler.GetRulerMilitaryRisk(this) > predictedBattleResults.attackerMaxCost) { targetables[i].aboveRisk = true; }
                                        if (predictedBattleResults.attackerVicChance < (1.0f - curRuler.rulerPersona["per_Risk"])) { targetables[i].belowChance = true; }
                                        targetables[i].riskRewardVal = curRuler.ValueRiskRatio(ReturnProvPersonalVal(targetables[i].thisTarget, provs, false), predictedBattleResults.attackerMaxCost);
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

                                        if (Actions.ConquerLand(targetables[0].thisTarget, this, ref provs)) //Evaluate battle
                                        {
                                            if (target.ReturnTechTotal() >= ReturnTechTotal()) //If the loser has any tech to learn from
                                            {
                                                techPoints += rnd.Next(1, 10);
                                            }
                                            targetables[0].thisTarget.updateText = "Occupied by " + _empireName;
                                        }
                                        else
                                        {
                                            if (ReturnTechTotal() >= target.ReturnTechTotal()) //If the loser has any tech to learn from
                                            {
                                                target.techPoints += rnd.Next(1, 10);
                                            }
                                            targetables[0].thisTarget.updateText = "Defended by " + targetables[0].thisTarget._ownerEmpire._empireName;
                                        }

                                    }
                                }
                                else
                                {
                                    warExhaustion += 10;
                                }
                            }
                        }
                    }
                }
            }

        }
        public void AI(List<string> actBuffer, ref List<Culture> cultures, List<Empire> empires, List<ProvinceObject> provs, ref List<Religion> religions, ref System.Random rnd, ref Date date)
        {

            foreach (string newAct in actBuffer) //iterate through listed actions
            {
                if(_componentProvinceIDs.Count == 0) { _exists = false; }
                if(_exists == false) { return; }
                switch (newAct)
                {
                    case "per_Idle":
                        return;
                    case "per_Colonize":
                        {
                            if (IsInWar()) { break; }
                            (bool canColonise, List<(int targetID, float valueRisk)> targets) canColony = curRuler.CanColoniseAdjs(ref provs, this);
                            if (canColony.canColonise)
                            {
                                if(canColony.targets.Count <= 0) { break; }
                                int[] targets = canColony.targets.OrderByDescending(x => x.valueRisk).Select(y=>y.targetID).ToArray(); //Highest value targets

                                foreach(int t in targets)
                                {
                                    if (DiplomaticConsiderations((Func< Dictionary<Empire, (int value, int time, string type)>>) delegate { return Act.Actions.ColonyOpinionMods(provs[t], this, provs, empires); },2)) //Check all diplomatic considerations
                                    {
                                        Actions.ColonizeLand(provs[t], this, provs, ref empires, ref date);
                                        Actions.IncreaseUnrest("colony", this, provs, new List<int>() { t });
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
                            if (techPoints >= 250)
                            {
                                int techProgress = Convert.ToInt32(1 + Math.Floor(((float)(ReturnTechTotal())) / 10.0f));

                                techPoints = -1 * (ReturnTechTotal()*techProgress); //Reset tech score

                                string devTech = curRuler.ReturnNextTech(this, ref rnd);
                                ref int techVar = ref TechStringToVar(devTech);

                                if (Actions.UpdateTech(ref empires, _id, devTech,1)) //Attempt update (add 1 to tech)
                                {
                                    if (devTech == "Diplomacy") //Diplomacy tech boosts grants a bonus to all nations with opinions of this nation
                                    {
                                        List<int> opEmpires = opinions.Keys.ToList();
                                        List<Empire> empMods = empires.Where(x => opEmpires.Contains(x._id)).ToList();

                                        foreach (Empire e in empMods)
                                        {
                                            Actions.AddNewModifier(e, this, 1000, 10, (date.day, date.month, date.year), "DIPTECH");
                                        }
                                    }
                                    else if (devTech == "Culture") //Culture tech devs grant a bonus to all nations with shared culture
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
                                    Debug.Log("FAILED TECH UPDATE");
                                    break;
                                }
                            }
                            else
                            {
                                //int ind = actBuffer.IndexOf(newAct);
                                //List<string> newActBuffer = new List<string>();
                                //newActBuffer = actBuffer.TakeWhile(x => actBuffer.IndexOf(x) > ind).ToList(); //Get all actions after this action
                                //newActBuffer.Insert(0,"per_LearnTech" );
                                ////Add learn tech call to the act buffer at first position

                                //AI(newActBuffer, ref cultures, empires, provs, ref religions, ref rnd, ref date); //Rerun actions
                                //return; 
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

                            int max = techWithDif.dif;
                            int increment = rnd.Next(0, Math.Min(2, max)) + 1; //Increment tech by up to 3 at a time.
                            if (Actions.UpdateTech(ref empires, _id, techWithDif.biggestDif, increment)) //Attempt update (add 1 to tech)
                            {
                                if (techPoints > 0)
                                {
                                    techPoints = Convert.ToInt32(Math.Ceiling(((float)techPoints) * 0.75f));
                                }

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
                            if (IsInWar()) { break; }
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
                                if (majorityRel != stateReligion)
                                {
                                    //If the state religion has not changed, do not update state religion. 
                                    Actions.IncreaseUnrest("religionSwitch", this, provs, null);
                                    Act.Actions.SetStateReligion(ref provs, empires, religions, _id, majorityRel._id, ref date);
                                    provs[_componentProvinceIDs[0]].updateText = "Converted to " + majorityRel._name;
                                    provs[_componentProvinceIDs[0]]._localReligion = stateReligion;

                                    return; //return without spreading
                                }
                                //If no change - allow spreading of religion
                            }
                            
                            if (stateReligion != null)
                            {
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
                                                Actions.IncreaseUnrest("religionSwitch", this, provs, new List<int>(){x});
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
                        break;
                    case "per_IncreaseOpinion":
                        {
                            if (IsInWar()) { break; }
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
                        {
                            if (IsInWar()) { break; }
                            if(rebels.Count > 0) { break; }
                            if(warExhaustion > 0) { break; }
                            if(curMil > curRuler.GetRulerMilitaryRisk(this)) //If the ruler is above their risk threshold
                            {

                                Dictionary<Empire, float> potentialWarTargets = WarTargets(empires, cultures, provs);

                                if (potentialWarTargets.Count <= 0) { break; }
                                else
                                {
                                    float maxScore = potentialWarTargets.Values.Max();
                                    Empire target = potentialWarTargets.First(x => x.Value >= maxScore).Key;


                                    provs[_componentProvinceIDs[0]].updateText = "Declared war on " + target._empireName;
                                    provs[target._componentProvinceIDs[0]].updateText = "Declared war on by " + _empireName;

                                    target.opinions[_id].StartWarMetrics(ref empires,target,ref provs);
                                    opinions[target._id].StartWarMetrics(ref empires, this, ref provs);

                                    Actions.AddNewModifier(target, this, 3650, -50, (date.day, date.month, date.year), "WAR");
                                    Actions.AddNewModifier(this, target, 3650, -50, (date.day, date.month, date.year), "WAR");
                                    return;

                                }
                            }
                        }
                        break;
                    case "per_Attack": //Quash Rebellions - See war for attacking nations
                        {
                            if (rebels.Count <= 0) { break; }
                            if (IsInWar()) { break; } //There isnt time for rebel supression in war
                            //SupressRebels();

                            foreach(Rebellion x in rebels.ToArray()) //DECENT CHANCE OF THIS CAUSING AN ERROR
                            {
                                if (AttemptFinishRebels(x)) { Debug.Log("REBELSDEAD"); }
                            }

                            if (rebels.Count <= 0) { break; }
                            List<(Rebellion rebel, float ecoScore, float valueRisk)> rebelGroups = rebels.Select(x => (x, ReturnEcoScoreFromSet(provs, provs.Where(y=>x._provinceIDs.Contains(y._id)).ToList(), false),0.0f)).ToList();
                            rebelGroups.ForEach(x => x.valueRisk = curRuler.ValueRiskRatio(Math.Max(0.01f, x.ecoScore), Math.Max(0.01f, x.rebel.rebelStrength)));
                            rebelGroups.OrderByDescending(x => x.valueRisk);

                            //Military risk does not matter. rulers will always try and stop rebels.

                            //Select highest value/risk rebel

                            Rebellion tRebel = rebelGroups[0].rebel;

                            (ProvinceObject thisTarget, float riskRewardVal)[] targetables = tRebel._provinceIDs.Select(x => (provs[x],ReturnProvPersonalVal(provs[x],provs,false))).ToArray();

                            int tCount = targetables.Count();

                            if (targetables.Count() != 0)
                            {
                                targetables.OrderByDescending(x => x.riskRewardVal);

                                if (Actions.SupressRebels(this,tRebel,provs,ref rnd,targetables[0].thisTarget._id)) //Evaluate battle. Unrest modifiers are included here.
                                {
                                    targetables[0].thisTarget.updateText = "Rebels Suppressed";
                                }
                                else
                                {
                                    targetables[0].thisTarget.updateText = "Rebels held ground";
                                }
                            }
                            else
                            {
                                break;
                            }
                            return;

                        }
                    case "per_SpawnRebellion":
                        {
                            //Random number of component provinces to insult
                            int targets = Math.Max(1,rnd.Next(0, 1+Convert.ToInt32(Math.Ceiling((float)(_componentProvinceIDs.Count()) / 3.0f))));
                            List<int> insultedComponents = new List<int>() { };
                            for(int i = 0;i<targets;i++)
                            {
                                int id = _componentProvinceIDs[rnd.Next(0, _componentProvinceIDs.Count)];

                                if(!insultedComponents.Contains(id))
                                {
                                    insultedComponents.Add(id);
                                }

                            }

                            if (insultedComponents.Count > 0)
                            {
                                Actions.IncreaseUnrest("localPolitics", this, provs, insultedComponents); //Insult all provinces in set
                            }

                            if (_componentProvinceIDs.Count >= 5)
                            {
                                float unrestCap = GetUnrestCap();
                                List<int> restlessProvs = _componentProvinceIDs.Where(x => provs[x]._unrest >= unrestCap && !rebels.Any(y=>y.IsContained(x)) && x != _componentProvinceIDs[0]).ToList(); //All non-rebel provinces with high unrest

                                if (restlessProvs.Count > 0)
                                {
                                    int targetProv = restlessProvs.OrderByDescending(x => provs[x]._unrest).ToList()[0]; //Get highest unrest province
                                    bool hasAppend = false; //If the province has been added to an existing rebellion
                                    foreach (Rebellion r in rebels)
                                    { 
                                        if (r.IsMember(targetProv, provs,this))
                                        {
                                            r.AddProvince(targetProv, provs);
                                            hasAppend = true;
                                        }
                                    }

                                    if (!hasAppend) //If this is a new rebellion
                                    {
                                        Rebellion newRebels = new Rebellion(targetProv, provs);
                                        newRebels.DetermineType(provs, this); //Set new type
                                        newRebels.AddAnyAppropriate(provs, this, ref rebels, unrestCap);

                                        rebels.Add(newRebels); //Add new rebel group
                                        provs[targetProv].updateText = "Started a revolt";
                                    }
                                }
                            }
                            return;
                        }
                    case "per_StirUnrest": //Make unrest in rival country
                        {
                            if(opinions.Count == 0) { break; }

                            List<int> potentialTargets = opinions.Where(x => x.Value._rival && empires[x.Key]._exists).Select(y=>y.Key).ToList(); //All rivals
                            if(potentialTargets.Count == 0) { break; }

                            potentialTargets.OrderBy(x => opinions[x].lastOpinion);
                            //TODO add diplomatic concerns and opinion modifier

                            Empire target = empires[potentialTargets[0]];


                            //Random number of component provinces to insult
                            int provTargets = Math.Max(1,rnd.Next(0, 1+Convert.ToInt32(Math.Ceiling((float)(target._componentProvinceIDs.Count()) / 4.0f))));
                            List<int> insultedComponents = new List<int>() { };
                            for (int i = 0; i < provTargets; i++)
                            {
                                if (target._componentProvinceIDs.Count > 0)
                                {
                                    int id = target._componentProvinceIDs[rnd.Next(0, target._componentProvinceIDs.Count)];

                                    if (!insultedComponents.Contains(id))
                                    {
                                        insultedComponents.Add(id);
                                    }
                                }

                            }
                            if (insultedComponents.Count > 0)
                            {
                                Actions.IncreaseUnrest("localPolitics", target, provs, insultedComponents); //Insult all provinces in set
                                provs[_componentProvinceIDs[0]].updateText = "Spy Caused Unrest";
                            }
                            return;
                        }
                    default: 
                        {
                            break;
                        }
                }
            }
        }
        public float GetUnrestCap()
        {
            return  5.0f + ((float)(culTech) / 5.0f);
        }
        public bool IsInWar()
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

        public void UpdateOpinion(ref List<ProvinceObject> provinces, ref List<Culture> cults, ref List<Empire> empires, ref Date curDate) //Thread for updating opinions for each empire
        {
            List<int> targets = provinces.Where(x => x._cultureID == _cultureID && x._ownerEmpire != null).Select(y => y._ownerEmpire).Select(z => z._id).ToList(); //Get all empires in culture
            {
                List<int> adjIds = ReturnAdjacentIDs(ref provinces, true);

                foreach (int i in adjIds)
                {
                    if (provinces[i]._ownerEmpire != null)
                    {
                        if (!targets.Contains(provinces[i]._ownerEmpire._id))
                        {
                            targets.Add(provinces[i]._ownerEmpire._id); //If adjacent add to set of targets
                        }
                    }
                }
            }

            targets = targets.Where(x => !opinions.ContainsKey(x) && x != _id).ToList(); //Remove empires with existing opinions and any potential targets to self
            targets = targets.Distinct().ToList(); //Make targets distinct

            foreach (int tInt in targets)
            {
                opinions.Add(tInt, new Opinion(this, empires[tInt])); //Add to set of opinions
            }

            foreach (Opinion op in opinions.Values.ToArray())
            {
                if (op.IsOpinionValid(this, ref empires, ref provinces))
                {
                    op.RecalculateOpinion(this, ref empires, ref curDate, ref provinces, ref cults);
                }
                else
                {
                    opinions.Remove(op.targetEmpireID);
                }
            }

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
                        else if (op._ally) { isHit = true; negOpinion += (weightVal * 0.2f); } //Stepping on allies toes has a minimal impact
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
            potentialTargets = opinions.Where(x => !x.Value._isWar && adjIDs.Any(y => empires[x.Key]._componentProvinceIDs.Contains(y)) && !x.Value._ally && !x.Value._fear && empires[x.Key]._exists && !x.Value.modifiers.Any(y=> y.typestring == "TREATY")).Select(z=>empires[z.Key]).ToList(); //Get all adjacent non-allies non feared

            if(potentialTargets.Count == 0) { return targetEmpires; } //Return empty set if there are no potential targets

            foreach(Empire target in potentialTargets)
            {
                float score = 0; //Targeting score
                score += target._componentProvinceIDs.Select(x => ReturnProvPersonalVal(provs[x], provs,false)).Sum() / target._componentProvinceIDs.Count();

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
                            float provVal = Math.Min(1,ReturnProvPersonalVal(enemyBorder, provs,false)); //Adds the personal value again to emphasise the impacts of border provinces
                            score += curRuler.rulerPersona["per_Risk"] > attackerVicChance ? (1+attackerVicChance) * provVal : (1-attackerVicChance) * -provVal;
                        }
                    }

                    foreach (ProvinceObject myBorder in myBorderProvinces) //Evaluate potential costs for each location of this empire
                    {
                        (int attackerMaxCost, int defenderMaxCost, float attackerVicChance) = Actions.BattleStats(myBorder, this, provs); //Theoretical battle stats for each battle
                        float provVal = Math.Min(1, ReturnProvPersonalVal(myBorder, provs,false)); //Adds the personal value again to emphasise the impacts of border provinces
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
                if(!opinions.ContainsKey(emp._id) || !emp.opinions.ContainsKey(_id)) { targetEmpires.Remove(emp); }
                else if (!DiplomaticConsiderations((Func<Dictionary<Empire, (int value, int time, string type)>>)delegate { return Act.Actions.WarOpinionMods(provs[emp._componentProvinceIDs[0]], this, provs, empires); }, 5))
                {
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
        public Dictionary<int, float>? ReturnTargetNormValues(List<int> tProvs, List<ProvinceObject> provs, bool includeOwned) //0-1 value of province values
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

            if(incProvs.Count == 0) { return null; }
            float min = incProvs.Min(x => ReturnProvPersonalVal(x, provs,true));
            float max = incProvs.Max(x => ReturnProvPersonalVal(x, provs,true));

            foreach(ProvinceObject t in incProvs)
            {
                pVals[t._id] = (ReturnProvPersonalVal(t,provs,true) - min) / (max - min);
            }

            return pVals;
        }

        public bool AttemptFinishRebels(Rebellion rebelGroup)
        {
            if (rebelGroup._provinceIDs.Count == 0) { rebels.Remove(rebelGroup); return true; }
            else { return false; }
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
            {"per_StirUnrest",0 },
            {"per_Risk",0 }, //Willingness to lose military for colony/willingness to fight low chance battles
            {"per_Insult",0 }, //Willingness to act in rude ways to rivals
            {"per_Calm",0 } //Decrease in unrest
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
            {"per_Calm",("Hedonistic","Oppressor","Popular","Leader") },
            {"per_StirUnrest",("Trusting","Fool","Covert","Spymaster")}
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

            if (ownedEmpire._componentProvinceIDs.Count == 0)
            {
                ownedEmpire._exists = false;
            }

            if (ownedEmpire._exists)
            {
                bool newDyn = false;
                hasAdoptedRel = false;

                if (rnd.Next(0, 10) == 2 || previousRuler == null) //Dynasty Replacement chance
                {
                    newDyn = true;
                }

                if (nameBuffer.Count == 0)
                {
                    nameBuffer = cultures[ownedEmpire._cultureID].LoadNameBuffer(25, ref rnd); //Load 25 names to minimize the amount of file accessing done at a time
                }

                fName = nameBuffer[0];
                nameBuffer.RemoveAt(0);

                if (newDyn == true) //If replacing a ruler with a new dynasty
                {
                    Actions.IncreaseUnrest("newDynasty", ownedEmpire, provs, null); //When a new dynasty takes power, increase unrest more
                    if (previousRuler != null)
                    {
                        if (ownedEmpire._componentProvinceIDs.Count > 0) { provs[ownedEmpire._componentProvinceIDs[0]].updateText = "New Dynasty"; }

                        List<string> applicableDyn = empires.Where(t => t.opinions.ContainsKey(ownedEmpire._id)
                        && t._cultureID == ownedEmpire._cultureID && (t.stateReligion == ownedEmpire.stateReligion)
                        && t.curRuler.lName != previousRuler.lName && t._id != ownedEmpire._id
                        && t.opinions.First(x => x.Value.targetEmpireID == ownedEmpire._id).Value.lastOpinion > 20).Select(l => l.curRuler.lName).ToList();
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
                        rulerPersona[personality] = ((float)(rnd.Next(10, 91))) / 100.0f;
                    }

                    techFocus[0] = rnd.Next(0, 5);
                }
                else
                {
                    if (rnd.Next(0, 30) == 15)
                    {
                        Actions.IncreaseUnrest("newDynasty", ownedEmpire, provs, null); //When a new dynasty takes power, increase unrest more
                        lName = cultures[ownedEmpire._cultureID].LoadDynasty(ref empires, ref rnd);
                    }
                    else
                    {
                        Actions.IncreaseUnrest("newRuler", ownedEmpire, provs,null); //Increase all unrest when a new ruler takes power
                        lName = previousRuler.lName;
                    }

                    foreach (string personality in previousRuler.rulerPersona.Keys.ToArray()) //Stats based on variance from previous ruler
                    {
                        float orient = (rnd.Next(0, 2) == 0 ? -1.0f : 1.0f);
                        float curVal = previousRuler.rulerPersona["per_Teach"];
                        float potentialSpread = (1.0f - Math.Min(0.2f,Math.Min(0.8f,previousRuler.rulerPersona["per_Teach"]))); //Potential offset from last ruler value
                        float newValue = (float)Math.Round(orient * (float)(rnd.NextDouble()) * potentialSpread,2); //Range from 0 to spread value. then times by 1 or -1 to get direction
                        rulerPersona[personality] = Math.Min(Math.Max(0.1f, (previousRuler.rulerPersona[personality] + (newValue))), 0.9f); //Add or minus the new change to the set, limit to range 0.1f to 0.9f
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
                if (maxBday == -1)
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

                deathday.age = rnd.Next(age + 5, 72 + Convert.ToInt32(Math.Floor((float)(ownedEmpire.ReturnTechTotal()) / 50)));

                if (provs[ownedEmpire._componentProvinceIDs[0]].updateText == "")
                {
                    provs[ownedEmpire._componentProvinceIDs[0]].updateText = "New Ruler";
                }
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

            List<string> techsToIncrement = new List<string>();

            //Get either the lowest tech or one (or rarely, both) of the focuses

            techsToIncrement = techVals.OrderBy(x => x.Item2).Take(1).Select(x=>x.Item1).ToList(); //Take lowest level techs

            (string, string) focuses = ReturnTechFocuses();

            if (techsToIncrement.Contains(focuses.Item1))
            {
                techsToIncrement.Add(focuses.Item2);
            }
            else if (techsToIncrement.Contains(focuses.Item2))
            {
                techsToIncrement.Add(focuses.Item1);
            }
            else
            { 
                if (techVals[techVals.IndexOf(techVals.First(x => x.Item1 == focuses.Item1))].Item2 < techVals[techVals.IndexOf(techVals.First(x => x.Item1 == focuses.Item2))].Item2)
                {
                    techsToIncrement.Add(focuses.Item1);
                }
                else if (techVals[techVals.IndexOf(techVals.First(x => x.Item1 == focuses.Item1))].Item2 > techVals[techVals.IndexOf(techVals.First(x => x.Item1 == focuses.Item2))].Item2)
                {
                    techsToIncrement.Add(focuses.Item2);
                }
                else
                {
                    techsToIncrement.Add(focuses.Item1);
                    techsToIncrement.Add(focuses.Item2);
                }
            }

            int rndIndex = rnd.Next(0, techsToIncrement.Count());

            return techsToIncrement[rndIndex];


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
            acts.Remove("per_Calm");

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
            Dictionary<int, float>? values = thisEmpire.ReturnTargetNormValues(adjIds, provs, false);

            if(values == null) { return colonyTargets; }

            foreach (int x in values.Keys)
            {
                (bool isAble, int cost) colonyAbility = Act.Actions.CanColonize(provs[x], thisEmpire, ref provs);
                if(colonyAbility.isAble && colonyAbility.cost > GetRulerMilitaryRisk(thisEmpire)) { colonyAbility.isAble = false; }
                if (colonyAbility.isAble)
                {
                    colonyTargets.anyT = true;
                    colonyTargets.items.Add((x, ValueRiskRatio(values[x],colonyAbility.cost)));
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
        public BindingList<Modifier> modifiers = new BindingList<Modifier>();
        public float lastOpinion = 0;

        public bool _fear = false;
        public bool _ally = false;
        public bool _rival = false;
        public bool _isRelevant = true;
        //War variables
        public bool _isWar = false;
        public float _maxWarExhaustion = 25.0f; //The amount of war exhaustion each nation is willing to have before war will end
        public float _capturedProvinces = 0; //This value keeps track of the 
        public RebelType? isRebels = null; //Null if not a rebellion. If it is a rebellion, this is set to the rebel type until the end of the war
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
        public float RecalculateOpinion(Empire myEmpire, ref List<Empire> empires, ref Date curDate, ref List<ProvinceObject> provs, ref List<Culture> cults)
        {
            float overallOpinion = 0;
            Empire targetEmpire = empires[targetEmpireID];

            float maxorminOpinion = 150; //-150 to 150

            if (myEmpire.stateReligion == null && targetEmpire.stateReligion == null) { overallOpinion -= Math.Min(maxorminOpinion, 5); } //Pagan opinion is negative
            else if (myEmpire.stateReligion != targetEmpire.stateReligion) { overallOpinion -= Math.Min(maxorminOpinion, 25); } //different religions penalty 
            else { overallOpinion += Math.Min(maxorminOpinion, 15); } //Same religion is positive opinion

            if (myEmpire.curRuler.lName == targetEmpire.curRuler.lName) { overallOpinion += Math.Min(maxorminOpinion, 75); } //Same dynasty opinion
            if (myEmpire._cultureID == targetEmpire._cultureID) { overallOpinion += Math.Min(maxorminOpinion, 10); }

            //Former opinion modifiers
            if (_ally) { overallOpinion += Math.Min(maxorminOpinion, 50); }
            if (_rival) { overallOpinion -= Math.Min(maxorminOpinion, 25); }
            if (_isWar) { overallOpinion -= 200; }
            if (targetEmpire.opinions.ContainsKey(myEmpire._id))
            {
                overallOpinion -= Math.Min(Math.Max(0, targetEmpire.opinions[myEmpire._id]._capturedProvinces), 5) * 10; //Territory disputes
            }

            if (!_isWar && Math.Abs(_capturedProvinces) > 0) { _capturedProvinces += _capturedProvinces < 0 ? 0.05f : -0.05f; } //The opinion penalty for captured provinces decreases overtime

            if(!_isWar && isRebels != null)
            {
                isRebels = null;
            }
            foreach (Modifier x in modifiers.ToArray())
            {
                if (!x.IsValid(ref curDate)) { modifiers.Remove(x); } //If after cooldown date
                else
                {
                    overallOpinion += x.opinionModifier; //Add opinion modifier to overall opinion
                }
            }

            overallOpinion = Math.Max(-maxorminOpinion, Math.Min(maxorminOpinion, overallOpinion)); //Limit to opinion set
            lastOpinion = overallOpinion;

            _isRelevant = IsRelevant(ref empires, myEmpire, ref provs, ref cults);
            if (_isRelevant)
            {
                _ally = DoesAlly(ref empires, myEmpire, ref provs, ref cults);
                if (!_ally)
                {
                    _fear = DoesFear(ref empires, myEmpire, ref provs, ref cults);
                    _rival = DoesRival(ref empires, myEmpire, ref provs, ref cults);
                }
            }
            return overallOpinion;
        }

        public void StartWarMetrics(ref List<Empire> empires, Empire myEmpire, ref List<ProvinceObject> provs) //Initialise the war here
        {
            Empire enemy = empires[targetEmpireID];
            _capturedProvinces = 0; //reset captured provinces.
            _maxWarExhaustion = 25.0f;
            _maxWarExhaustion += myEmpire.maxMil * (1 + (myEmpire.ReturnAllPersonalVal(provs) / (myEmpire.ReturnAllPersonalVal(provs)+enemy.ReturnAllPersonalVal(provs)))); //Max mil by provincial value
            if (myEmpire.warExhaustion > 0) { _maxWarExhaustion += myEmpire.warExhaustion; } 

            _isWar = true;
        }

        public bool PollEndWar(ref List<Empire> empires, Empire myEmpire, ref List<ProvinceObject> provs, ref Date curDate) //End war
        {
            if (!_isWar) { return false; }
            Empire enemy = empires[targetEmpireID];

            if(myEmpire._exists == false || enemy._exists == false) { return true; } //The war handler should end this
            
            if(enemy.warExhaustion > enemy.opinions[myEmpire._id]._maxWarExhaustion && myEmpire.warExhaustion > _maxWarExhaustion) //If both nations are over their exhaustion capacity
            {
                _isWar = false;
                enemy.opinions[myEmpire._id]._isWar = false;

                if (myEmpire._componentProvinceIDs.Count == 0) { myEmpire._exists = false; }
                if (enemy._componentProvinceIDs.Count == 0) { enemy._exists = false; }

                if (myEmpire.opinions[enemy._id].isRebels != null || enemy.opinions[myEmpire._id].isRebels != null) //If either side are rebels. This should only fire when the rebels win as empires will fight to remove rebels unless forced to surrender
                {
                    RebelWarEnding(myEmpire, enemy, provs, ref curDate);
                }

                if (_capturedProvinces < 0)
                {
                    Actions.IncreaseUnrest("lostWar", myEmpire, provs, null); //Losing war unrest boost
                }

                if (myEmpire._exists && enemy._exists) {
                    provs[myEmpire._componentProvinceIDs[0]].updateText = "Made peace with " + enemy._empireName;
                    provs[enemy._componentProvinceIDs[0]].updateText = "Made peace with " + myEmpire._empireName;

                    int peaceDays = 3650; //10 Years + Diplomacy lower
                    int diploItem = Math.Min(50,myEmpire.dipTech > enemy.dipTech ? enemy.dipTech : myEmpire.dipTech);
                    peaceDays += (365 * diploItem);

                    //Peace treaties
                    Actions.AddNewModifier(enemy, myEmpire, peaceDays, -20, (curDate.day, curDate.month, curDate.year), "TREATY");
                    Actions.AddNewModifier(myEmpire, enemy, peaceDays, -20, (curDate.day, curDate.month, curDate.year), "TREATY");
                }

                return true;
            }
            return false;
        }
        private void RebelWarEnding(Empire myEmpire, Empire otherEmpire, List<ProvinceObject> provs, ref Date curDate) //Ends a war with rebels. This only occurs if the rebels win.
        {
            Empire ownerEmpire = myEmpire.opinions[otherEmpire._id].isRebels != null ? myEmpire : otherEmpire; //Selects the non-rebel empire
            Empire rebelEmpire = myEmpire == ownerEmpire ? otherEmpire : myEmpire; //The rebel empire
            RebelType rType = (RebelType)ownerEmpire.opinions[rebelEmpire._id].isRebels; //Get the rebel type

            switch(rType)
            {
                case RebelType.Culture: //For culture rebels, rebels return all provinces not of their culture
                    {
                        List<int> returnedProvs = rebelEmpire._componentProvinceIDs.Where(x => provs[x]._cultureID != rebelEmpire._cultureID).ToList();
                        if(returnedProvs.Count() >= rebelEmpire._componentProvinceIDs.Count) { break; } //If this composes all the rebel provs, end early

                        foreach(int provID in returnedProvs)
                        {
                            rebelEmpire._componentProvinceIDs.Remove(provID);
                            ownerEmpire._componentProvinceIDs.Add(provID);
                            provs[provID]._ownerEmpire = ownerEmpire;
                        }

                        break;
                    }
                case RebelType.Religion: //For religion rebels, remove all provinces not of the culture or religion
                    {
                        List<int> returnedProvs = rebelEmpire._componentProvinceIDs.Where(x => provs[x]._cultureID != rebelEmpire._cultureID || (provs[x]._localReligion != null && provs[x]._localReligion != rebelEmpire.stateReligion)).ToList();
                        if (returnedProvs.Count() >= rebelEmpire._componentProvinceIDs.Count) { break; } //If this composes all the rebel provs, end early

                        foreach (int provID in returnedProvs)
                        {
                            rebelEmpire._componentProvinceIDs.Remove(provID);
                            ownerEmpire._componentProvinceIDs.Add(provID);
                            provs[provID]._ownerEmpire = ownerEmpire;
                        }

                        break;
                    }
                default:
                    {
                        break;
                    }
            }

            ownerEmpire.opinions[rebelEmpire._id].isRebels = null; //Remove rebel flag
        }
        public bool DoesFear(ref List<Empire> empires, Empire myEmpire, ref List<ProvinceObject> provs, ref List<Culture> cults) //Returns if the current empire is afraid of the target empire. They will attempt to not act against an empire they fear
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

        public bool DoesRival(ref List<Empire> empires, Empire myEmpire, ref List<ProvinceObject> provs, ref List<Culture> cults) //Returns if the current empire considers the other empire a rival. they will act against eachother if true
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
            if (isEconomicallyRelevant(ref empires, ref cults, myEmpire, target, ref provs)) { rivalScore++; } //Economic rivalry

            if (Math.Abs(target.ReturnTechTotal() - myEmpire.ReturnTechTotal()) > 5) { rivalScore--; } //If the difference in tech is high, reduce rival score
            if(myEmpire.ReturnAdjacentIDs(ref provs, true).Count(x => target._componentProvinceIDs.Contains(x)) >= Math.Min(myEmpire._componentProvinceIDs.Count(),5)) { rivalScore++; } //If too many adjacents

            if (rivalScore > 0) { return true; }
            else { return false; }
        }
        public bool DoesAlly(ref List<Empire> empires, Empire myEmpire, ref List<ProvinceObject> provs, ref List<Culture> cults) //Returns if the current empire considers the other an ally
        {
            if (!_isRelevant || _isWar) { return false; }
            Empire target = empires[targetEmpireID];
            if (!target.opinions.ContainsKey(myEmpire._id)) { return false; }
            if (_capturedProvinces > 0 || target.opinions[myEmpire._id]._capturedProvinces > 0) { return false; } //If any provinces are disputed, then nations may not ally
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

        public bool IsRelevant(ref List<Empire> empires, Empire myEmpire, ref List<ProvinceObject> provs,ref List<Culture> cults) //If the empire considers this empire relevant in anyway
        {
            if(_fear || _ally || _rival) { return true; }
            Empire target = empires[targetEmpireID];
            if (!target.opinions.ContainsKey(myEmpire._id)) { return false; }
            if (_capturedProvinces > 0 || target.opinions[myEmpire._id]._capturedProvinces > 0) { return true; }
            if(target.maxMil > myEmpire.curMil * 0.75f) { return true; } //If military advantage at this current time
            if(lastOpinion > 50) { return true; }

            if(isEconomicallyRelevant(ref empires, ref cults, myEmpire, target, ref provs)) { return true; }

            if (myEmpire.curRuler.lName == target.curRuler.lName) { return true; }
            if(myEmpire.ReturnTechTotal() < target.ReturnTechTotal() - 5) { return true; } //If large enough technology difference
            return false;
        }

        private bool isEconomicallyRelevant(ref List<Empire> empires, ref List<Culture> cults, Empire myEmpire, Empire targetEmpire, ref List<ProvinceObject> provs)
        {
            if (targetEmpire._cultureID == myEmpire._cultureID)
            {
                if (cults[myEmpire._cultureID]._empireRanking == null) { cults[myEmpire._cultureID].RankEconomy(ref empires); } //If there is no eco ranking, rank it.
                List<int> eByEconomy = cults[myEmpire._cultureID]._empireRanking;

                if (!eByEconomy.Contains(targetEmpire._id) || !eByEconomy.Contains(myEmpire._id)) { return false; }

                if (eByEconomy.Count >= 5)
                {
                    int indexOfSelf = eByEconomy.IndexOf(myEmpire._id);
                    int indexOfEnemy = eByEconomy.IndexOf(targetEmpire._id);

                    if (indexOfSelf - 1 == indexOfEnemy || indexOfSelf + 1 == indexOfEnemy) { return true; }
                }
            }
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

    public enum RebelType
    {
        Culture,
        Religion,
        Revolution,
        Separatist,
    }
    public class Rebellion
    {
        public RebelType _type; //Rebellion Type
        public List<int> _provinceIDs = new List<int>();
        public string targetType; //Defines the target for a rebellion. If a nation already has a rebellion with the same type, it will be sorted into this
        public float rebelStrength = 0; //The strength of the rebels (mil values)
        public int pollCooldown = 100;
        public Rebellion(int provinceID, List<ProvinceObject> provs) //Make a new rebellion
        {
            AddProvince(provinceID, provs);
        }

        public Rebellion()
        {

        }
        public bool IsContained(int provID)
        {
            return _provinceIDs.Any(x => x == provID);
        }
        public void AddProvince(int provID, List<ProvinceObject> provs)
        {
            if (!_provinceIDs.Contains(provID))
            {
                _provinceIDs.Add(provID);
            }
        }

        public void AppendRebelStrength(List<ProvinceObject> provs, Empire myEmpire, ref System.Random rnd) //Add to rebel strength
        {
            rebelStrength += Math.Max(1,myEmpire.RebelMilIncrease(this, provs) * (float)(rnd.NextDouble())); //Add military strength to rebels
            float rebelMaxArmy = Math.Max(10,GetRebelMaxArmy(provs, myEmpire)); //Get rebel maximum size
            if (rebelStrength >= rebelMaxArmy) { rebelStrength = rebelMaxArmy; } 
        }
        public float GetRebelMaxArmy(List<ProvinceObject> provs, Empire myEmpire)
        {
            //Rebel max army = milincrease * (highest unrest + mean unrest)
            float milIncrease = myEmpire.RebelMilIncrease(this, provs);
            float maxUnrest = _provinceIDs.Max(x => provs[x]._unrest);
            float meanUnrest = _provinceIDs.Average(x => provs[x]._unrest);

            return milIncrease * (maxUnrest + meanUnrest);
        }

        public void TakeRebelLoss(List<ProvinceObject> provs, float losses, float enemyVicChance, int provCaptured, bool won, Empire myEmpire)
        {
            if(_provinceIDs.Count == 0) { myEmpire.AttemptFinishRebels(this); return; }
            rebelStrength -= losses; //Remove military strength from rebels. This can go negative.
            if (won) //If won
            {
                foreach(int prov in _provinceIDs) //Add unrest proportional to the chance of the empire winning - showing weakness
                {
                    provs[prov]._unrest += (1.0f - enemyVicChance);
                }

                foreach(int prov in myEmpire._componentProvinceIDs)
                {
                    provs[prov]._unrest += 0.2f;
                }
            }
            else
            {
                _provinceIDs.Remove(provCaptured);
                provs[provCaptured]._unrest = 0; //Reset unrest

                foreach (int prov in myEmpire._componentProvinceIDs)
                {
                    provs[prov]._unrest -= 0.2f;
                }
            }
        }
        public void DetermineType(List<ProvinceObject> provs, Empire myEmpire) //Determine which type of rebellion this will be + target type
        {
            if(myEmpire._cultureID != provs[_provinceIDs[0]]._cultureID) //If non-primary culture
            {
                int cultureID = provs[_provinceIDs[0]]._cultureID;
                float thisCultValue = myEmpire._componentProvinceIDs.Where(x => provs[x]._cultureID == cultureID).Sum(y=>myEmpire.ReturnIndProvVal(provs[y],provs));
                float primaryCultValue = myEmpire._componentProvinceIDs.Where(x => provs[x]._cultureID == myEmpire._cultureID).Sum(y => myEmpire.ReturnIndProvVal(provs[y], provs));

                if (thisCultValue > primaryCultValue * 0.30f) //If the culture is worth more than 30% of the primary culture value
                {
                    _type = RebelType.Culture;
                    targetType = cultureID.ToString();
                    return;
                }
            }
             
            if(myEmpire.stateReligion != null && provs[_provinceIDs[0]]._localReligion != null && provs[_provinceIDs[0]]._localReligion != myEmpire.stateReligion)
            {
                int relID = provs[_provinceIDs[0]]._localReligion._id;
                float thisRelVal = myEmpire._componentProvinceIDs.Where(x => provs[x]._localReligion != null && provs[x]._localReligion._id == relID).Sum(y => myEmpire.ReturnIndProvVal(provs[y], provs));
                float primaryRelVal = myEmpire._componentProvinceIDs.Where(x => provs[x]._localReligion != null && provs[x]._localReligion._id == myEmpire.stateReligion._id).Sum(y => myEmpire.ReturnIndProvVal(provs[y], provs));

                if (thisRelVal > primaryRelVal * 0.30f) //If the religion is worth more than 30% of the primary religion value
                {
                    _type = RebelType.Religion;
                    targetType = relID.ToString();
                    return;
                }
            }
            
            if(provs[_provinceIDs[0]]._population == Property.High && provs[_provinceIDs[0]]._id != myEmpire._componentProvinceIDs[0]) //Seperatists
            {
                //TODO maybe add more conditions?

                bool anyAdjacentUnrest = _provinceIDs.Any(x => provs[x]._adjacentProvIDs.Any(y=>provs[y]._unrest >= (myEmpire.GetUnrestCap() / 2.0f)));
                int cultureID = provs[_provinceIDs[0]]._cultureID;

                if (anyAdjacentUnrest)
                {
                    _type = RebelType.Separatist;
                    targetType = cultureID.ToString();
                    return;
                }
            }

            //Fallthrough - default
            
            _type = RebelType.Revolution;
        }

        public void AddAnyAppropriate(List<ProvinceObject> provs, Empire myEmpire, ref List<Rebellion> rebels, float baseUnrest) //And any relevant provinces to set
        {
            float minUnrest = baseUnrest * 0.75f;
            
            foreach(int id in myEmpire._componentProvinceIDs)
            {
                if (id != myEmpire._componentProvinceIDs[0])
                {
                    bool isFree = true;
                    if (rebels.Count > 0)
                    {
                        if (rebels.Any(x => x.IsContained(id)))
                        {
                            isFree = false;
                        }
                    }

                    if (isFree)
                    {
                        if (IsMember(id, provs, myEmpire) && provs[id]._unrest >= minUnrest) //If appropriate unrest level and fits condition, add to set.
                        {
                            AddProvince(id, provs);
                        }
                    }
                }
            }   
        }
        public bool RemoveIfApplicable(int id) //Returns true if still alive
        {
            if(IsContained(id))
            {
                _provinceIDs.Remove(id);

                if(_provinceIDs.Count <= 0)
                {
                    return false;
                }
            }
            return true;
        }
        public bool IsMember(int provinceID, List<ProvinceObject> provs, Empire empire) //Checks if this province can become a member of this group
        {
            if(provs[provinceID]._id == empire._componentProvinceIDs[0]) { return false; }
            if(_type == RebelType.Culture && provs[provinceID]._cultureID == Convert.ToInt32(targetType)) { return true; }
            else if (_type == RebelType.Religion && provs[provinceID]._localReligion != null && provs[provinceID]._localReligion._id == Convert.ToInt32(targetType)) { return true; }
            else if (_type == RebelType.Separatist && _provinceIDs.Any(x => provs[x]._adjacentProvIDs.Contains(provinceID))) { return true; }
            else if(_type == RebelType.Revolution && _provinceIDs.Any(x => provs[x]._adjacentProvIDs.Contains(provinceID)) && ((float)_provinceIDs.Count() < (float)(empire._componentProvinceIDs.Count()) / 2.0f)){ return true; }
            return false;
        }
    }

}