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

        public float maxMil;
        public float curMil;
        public float leftoverMil;
        public int timeUntilNextUpdate;
        public Empire(int id, string name, ProvinceObject startingProvince, ref List<Culture> cultures, ref List<Empire> empires, ref List<ProvinceObject> provs) //Constructor for an empire - used when a new empire is spawned
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

            provs[_componentProvinceIDs[0]].updateText = "New Nation";

            curRuler = new Ruler(null,this, ref cultures, ref empires, ref provs); //Create new random ruler
            RecruitMil(ref cultures, ref provs);

        }
        public bool CheckForUpdate(ref System.Random rnd)
        {
            timeUntilNextUpdate--;

            if(timeUntilNextUpdate <= 0)
            {
                timeUntilNextUpdate = rnd.Next(0, 8);
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
            return _componentProvinceIDs.Sum(x => ReturnIndProvVal(provinces[x]));
        }

        public float ReturnIndProvVal(ProvinceObject tProv) //Economic value per province
        {
            float score = 0;
            if(tProv._population == Property.High) { score += 1.0f; }
            else if(tProv._population == Property.Medium) { score += 0.5f; }
            else if(tProv._population == Property.Low) { score += 0.25f; }

            if (tProv._isCoastal) { score += 0.5f; }

            return score;
        }

        public float ReturnProvPersonalVal(ProvinceObject tProv, List<ProvinceObject> provs) //Economic value + personal value
        {
            float score = ReturnIndProvVal(tProv);
            if(tProv._localReligion == stateReligion && stateReligion != null) { score += 0.1f; }
            if(tProv._biome == provs[_componentProvinceIDs[0]]._biome) { score += 0.1f; }
            score += (float)(_componentProvinceIDs.Count(x => tProv._adjacentProvIDs.Contains(x))) * 0.25f; //Adjacency bonus
            score += (tProv._adjacentProvIDs.Contains(_componentProvinceIDs[0])) ? 1f : 0;
            if(tProv._cultureID == _cultureID) { score += 0.5f; }
            if(tProv._adjacentProvIDs.Count(x=>provs[x]._biome != 0 && _componentProvinceIDs.Contains(x)) == tProv._adjacentProvIDs.Count(x => provs[x]._biome != 0)) { score += 5.0f; } //If prov owns all adjacents, add a lot to score
            return score;
        }
        public int ReturnTechTotal()
        {
            return milTech + ecoTech + dipTech + logTech + culTech;
        }
        public float ReturnEcoScore(ref List<ProvinceObject> provinces) //Get economics score for this empire
        {
            return ((float)(this.ReturnTechTotal())/5.0f)*(0.1f+
                ((ReturnProvsVal(provinces))/20.0f)+((float)(ecoTech)/30.0f));
        }
        public float ReturnIndividualEcoScore(ProvinceObject tProv) //Get economics score for a single province
        {
            return ((float)(this.ReturnTechTotal()) / 5.0f) * (0.1f +
                ((ReturnIndProvVal(tProv)) / 20.0f) + ((float)(ecoTech) / 30.0f));
        }

        public void RecalculateMilMax(List<ProvinceObject> provinces) //Finds the appropriate max military. Redone every time mil is recruited
        {
            maxMil = ((25 + Math.Min(100000000,(float)Math.Floor(((float)(milTech) * (0.5f + (ReturnProvsVal(provinces)/10.0f)))))) * 10.0f) + (float)(Math.Floor(ReturnProvsVal(provinces) * 4.0f));
        }
        public float ExpectedMilIncrease(ref List<ProvinceObject> provinces)
        {
            return ((float)Math.Round(1.0f+(float)Math.Min(100000000,ReturnEcoScore(ref provinces)/2.0f),2)*5);
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
        public void PollForAction((int day, int month, int year) currentDate, ref List<Culture> cultures, ref List<Empire> empires, ref List<ProvinceObject> provs, ref System.Random rnd)
        {
            if (_exists) //If this empire is active
            {
                AgeMechanics(currentDate, ref cultures, ref empires, ref provs);

                if (CheckForUpdate(ref rnd))
                {
                    //Chance of ruler making an action
                    float actChance = Math.Min(0.85f, ((float)(dipTech) / 500.0f));
                    if (rnd.NextDouble() <= actChance)
                    {
                        AI(curRuler.CalculateRandomActsOrder(), ref cultures, ref empires, ref provs, ref rnd);
                    }
                }
            }
        }
        public void AgeMechanics((int day, int month, int year) currentDate, ref List<Culture> cultures, ref List<Empire> empires, ref List<ProvinceObject> provs)
        {
            if (currentDate.day == curRuler.birthday.day && currentDate.month == curRuler.birthday.month) //On birthday
            {
                curRuler.age++;
            }

            if ((currentDate.day >= curRuler.deathday.day && currentDate.month >= curRuler.deathday.month && curRuler.age == curRuler.deathday.age) || (curRuler.age > curRuler.deathday.age)) //on deathday
            {
                //Replace ruler
                Ruler tmpRuler = new Ruler(curRuler, this, ref cultures, ref empires, ref provs);
                curRuler = tmpRuler;
            }
        }
        public List<int> ReturnAdjacentIDs(ref List<ProvinceObject> provs, bool isDistinct)
        {
            List<int> pSet = provs.Where(x => x._ownerEmpire == this).SelectMany(y => y._adjacentProvIDs).ToList();
            if(isDistinct) { pSet = pSet.Distinct().ToList(); }
            return pSet.Where(z => !_componentProvinceIDs.Contains(z)).ToList();
        }

        private void AI(List<string> actBuffer, ref List<Culture> cultures, ref List<Empire> empires, ref List<ProvinceObject> provs, ref System.Random rnd)
        {
            foreach (string newAct in actBuffer) //iterate through listed actions
            {
                switch (newAct)
                {
                    case "per_Idle":
                        return;
                    case "per_Colonize":
                        {
                            (bool canColonise, List<(int targetID, float valueRisk)> targets) canColony = curRuler.CanColoniseAdjs(ref provs, this);
                            if (canColony.canColonise)
                            {
                                Debug.Log(_id + " COLONISING");
                                float maxValue = canColony.targets.Max(x => x.valueRisk);
                                int target = canColony.targets.Where(x => x.valueRisk >= maxValue).ToList()[0].targetID; //Get highest value/cost ratio province

                                Actions.ColonizeLand(provs[target], this, ref provs);
                                provs[target].updateText = "Colonised by " + _empireName;
                                return;
                            }
                            break;
                        }
                    case "per_DevelopTech":
                        {
                            if (rnd.Next(0, 4) == 1) //1/3 chance of successful development
                            {
                                Debug.Log(_id + " DEVELOPING TECH");
                                (string tech1, string tech2) techsDevelopable = curRuler.ReturnTechFocuses();
                                ref int t1 = ref TechStringToVar(techsDevelopable.tech1);
                                ref int t2 = ref TechStringToVar(techsDevelopable.tech2);

                                if (t1 >= t2) { t1++; provs[_componentProvinceIDs[0]].updateText = "Developed " + techsDevelopable.tech1 + " level " + t1; }
                                else { t2++; provs[_componentProvinceIDs[0]].updateText = "Developed " + techsDevelopable.tech2 + " level " + t2; }
                                return;
                            }
                            else
                            {
                                break;
                            }
                        }
                    case "per_LearnTech":
                        {
                            (int mMil, int mEco, int mDip, int mLog, int mCul) maxVals = MaxLearnableTechs(ref empires, ref cultures, provs); 
                            if (maxVals == (milTech, ecoTech, dipTech, logTech, culTech)) { break; } //If already max tech, move to next action.

                            Debug.Log(_id + " LEARNING TECH");
                            (string biggestDif, int dif) techWithDif = ("",-1);

                            if(maxVals.mMil > milTech) { if ((maxVals.mMil-milTech) > techWithDif.dif) { techWithDif.biggestDif = "Military"; techWithDif.dif = (maxVals.mMil- milTech); } }
                            if (maxVals.mEco > ecoTech) { if ((maxVals.mEco-ecoTech) > techWithDif.dif) { techWithDif.biggestDif = "Economic"; techWithDif.dif = (maxVals.mEco-ecoTech); } }
                            if (maxVals.mDip > dipTech) { if ((maxVals.mDip-dipTech) > techWithDif.dif) { techWithDif.biggestDif = "Diplomacy"; techWithDif.dif = (maxVals.mDip-dipTech); } }
                            if (maxVals.mLog > logTech) { if ((maxVals.mLog-logTech) > techWithDif.dif) { techWithDif.biggestDif = "Logistics"; techWithDif.dif = (maxVals.mLog-logTech); } }
                            if (maxVals.mCul > culTech) { if ((maxVals.mCul-culTech) > techWithDif.dif) { techWithDif.biggestDif = "Culture"; techWithDif.dif = (maxVals.mCul-culTech); } }

                            if (Actions.UpdateTech(ref empires, _id, techWithDif.biggestDif)) //Attempt update
                            {
                                provs[_componentProvinceIDs[0]].updateText = "Learned " + techWithDif.biggestDif + " level " + TechStringToVar(techWithDif.biggestDif);
                                return;
                            }
                            else
                            {
                                break;
                            }
                        }
                    default: 
                        {
                            break;
                        }
                }
            }
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
        private (int mMil, int mEco, int mDip, int mLog, int mCul) MaxLearnableTechs(ref List<Empire> empires, ref List<Culture> cultures, List<ProvinceObject> provs)
        {
            (int mMil, int mEco, int mDip, int mLog, int mCul) mTechs = cultures[_cultureID].CalculateMaxTech(ref empires);
            List<int> adjIds = ReturnAdjacentIDs(ref provs, true);
            List<ProvinceObject> adjacentProvsDiffEmp = provs.Where(x => adjIds.Contains(x._id) && x._ownerEmpire != null && x._ownerEmpire != this).ToList();
            List<Empire> adjEmpires = adjacentProvsDiffEmp.Select(x => x._ownerEmpire).Distinct().ToList();

            foreach (Empire x in adjEmpires)
            {
                if(x.milTech > mTechs.mMil) { mTechs.mMil = x.milTech; }
                if (x.ecoTech > mTechs.mEco) { mTechs.mEco = x.ecoTech; }
                if (x.dipTech > mTechs.mDip) { mTechs.mDip = x.dipTech; }
                if (x.logTech > mTechs.mLog) { mTechs.mLog = x.logTech; }
                if (x.culTech > mTechs.mCul) { mTechs.mCul = x.culTech; }
            }

            return mTechs;
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
    }

    public class Ruler
    {
        public static System.Random rulerRND = new System.Random();
        public string fName = "NULL";
        public string lName = "NULL";
        public int age;
        public (int day, int month) birthday;
        public (int day, int month, int age) deathday;
        private List<string> nameBuffer = new List<string>() { };
        private string rTitle = "NULL";

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
            {"per_Risk",0 } //Willingness to lose military for colony/willingness to fight low chance battles
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
            {"per_Risk",("Craven","Coward","Brazen","Gambler")}
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
        public Ruler(Ruler previousRuler, Empire ownedEmpire, ref List<Culture> cultures, ref List<Empire> empires, ref List<ProvinceObject> provs)
        {
            //TODO add names and make previous ruler details not apply if last name is changed
            bool newDyn = false;

            if(rulerRND.Next(0,10) == 2 || previousRuler == null) //Dynasty Replacement chance
            {
                newDyn = true;
            }

            if (nameBuffer.Count == 0)
            {
                nameBuffer = cultures[ownedEmpire._cultureID].LoadNameBuffer(25, ref rulerRND); //Load 25 names to minimize the amount of file accessing done at a time
            }

            fName = nameBuffer[0];
            nameBuffer.RemoveAt(0);

            if(newDyn == true) //If replacing a ruler with a new dynasty
            {
                if (previousRuler != null)
                {
                    List<string> applicableDyn = empires.Where(t => t._cultureID == ownedEmpire._cultureID && t.curRuler.lName != previousRuler.lName && t._id != ownedEmpire._id).Select(l=> l.curRuler.lName).ToList(); //Get all other dynasties in the same culture group
                    if (applicableDyn.Count > 0)
                    {
                        if (rulerRND.Next(0, 10 - Math.Min((int)Math.Floor(((float)(ownedEmpire.dipTech) / 100)), 5)) == 1) //Take dynasty from within culture group
                        {
                            lName = applicableDyn[rulerRND.Next(0, applicableDyn.Count)]; //Get dynasty from other nation
                        }
                        else
                        {
                            lName = cultures[ownedEmpire._cultureID].LoadDynasty(ref empires, ref rulerRND);
                        }
                    }
                    else
                    {
                        lName = cultures[ownedEmpire._cultureID].LoadDynasty(ref empires, ref rulerRND);
                    }
                }
                else
                {
                    lName = cultures[ownedEmpire._cultureID].LoadDynasty(ref empires, ref rulerRND);
                }

                foreach (string personality in rulerPersona.Keys.ToArray()) //Entirely random stats
                {
                    rulerPersona[personality] = ((float)(rulerRND.Next(0, 101))) / 100;
                }

                techFocus[0] = rulerRND.Next(0, 5);
            }
            else
            {
                if(rulerRND.Next(0,30) == 15)
                {
                    lName = cultures[ownedEmpire._cultureID].LoadDynasty(ref empires, ref rulerRND);
                }
                else
                {
                    lName = previousRuler.lName;
                }
                foreach (string personality in previousRuler.rulerPersona.Keys.ToArray()) //Stats based on variance from previous ruler
                {
                    float orient = (rulerRND.Next(0, 2) == 0 ? -1 : 1);
                    float offset = 1 - rulerRND.Next(0, (int)((0.001f+previousRuler.rulerPersona["per_Teach"]) * 100));
                    offset = Math.Max(1 - Math.Abs(Math.Min(Math.Abs(previousRuler.rulerPersona[personality] - 1), offset)),offset);
                    rulerPersona[personality] = Math.Min(Math.Max(0,(previousRuler.rulerPersona[personality] + (orient * offset)) ),1);
                }

                techFocus[0] = previousRuler.techFocus[rulerRND.Next(0, 2)];
            }

            //TODO maybe modify this - bad form
            while (true)
            {
                techFocus[1] = rulerRND.Next(0, 5);
                if (techFocus[1] != techFocus[0])
                {
                    break;
                }
            }

            //Person data
            birthday.month = rulerRND.Next(1, 13);
            int maxBday = Calendar.Calendar.monthSizes[((Calendar.Calendar.Months)(birthday.month)).ToString()];
            if(maxBday == -1)
            {
                maxBday = 28;
            }
            birthday.day = rulerRND.Next(1, maxBday + 1);

            deathday.month = rulerRND.Next(1, 13);
            int maxDDay = Calendar.Calendar.monthSizes[((Calendar.Calendar.Months)(birthday.month)).ToString()];
            if (maxDDay == -1)
            {
                maxDDay = 28;
            }
            deathday.day = rulerRND.Next(1, maxDDay + 1);
            age = rulerRND.Next(18, 70);

            deathday.age = rulerRND.Next(age, 72 + Convert.ToInt32(Math.Floor((float)(ownedEmpire.ReturnTechTotal()) / 50)));

            if(provs[ownedEmpire._componentProvinceIDs[0]].updateText == "")
            {
                provs[ownedEmpire._componentProvinceIDs[0]].updateText = "New Ruler";
            }
        }

        public (string,string) ReturnTechFocuses()
        {
            return (techNames[techFocus[0]], techNames[techFocus[1]]);
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
        public List<string> CalculateRandomActsOrder() //Weights random acts
        {
            //TODO add calculation on if each act is even possible using ruler stats?

            List<string> acts = new List<string>();
            List<string> weightedRandomOrder = new List<string>();

            acts.AddRange(rulerPersona.Keys);
            //Remove non-action personality traits
            acts.Remove("per_Teach");
            acts.Remove("per_Risk");

            int actCount = acts.Count;
            for(int i = 0;i<actCount;i++)
            {
                string store = acts[i];
                int index = rulerRND.Next(0, actCount);
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

                    float rndFloat = (float)rulerRND.Next(0, Convert.ToInt32(weightMax * multiplier)) / multiplier; //Random number from 0 to max

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

        private float GetRulerMilitaryRisk(ref Empire thisEmpire) //Amount ruler is willing to risk per cost
        {
            //TODO add economic value to calculation???
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
                if(colonyAbility.isAble && colonyAbility.cost > GetRulerMilitaryRisk(ref thisEmpire)) { colonyAbility.isAble = false; }
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
}