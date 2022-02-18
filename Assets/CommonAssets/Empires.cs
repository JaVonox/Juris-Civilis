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
        private float leftoverMil;
        public Empire(int id, string name, ProvinceObject startingProvince, ref List<Culture> cultures, ref List<Empire> empires) //Constructor for an empire - used when a new empire is spawned
        {
            _id = id;
            _exists = true;
            _empireName = name;
            _componentProvinceIDs.Add(startingProvince._id);
            startingProvince.NewOwner(this); //Append self to set of owner
            _componentProvinceIDs.ListChanged += CheckEmpireExists;
            _empireCol = startingProvince._provCol;

            //TODO make new empire spawn with duplicate of lowest tech in their culture?
            milTech = 1;
            ecoTech = 1;
            dipTech = 1;
            logTech = 1;
            culTech = 1;
            _cultureID = startingProvince._cultureID;
            stateReligion = startingProvince._localReligion != null ? startingProvince._localReligion : null;

            curRuler = new Ruler(null,this, ref cultures, ref empires); //Create new random ruler
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
        public float ReturnPopScore(List<ProvinceObject> provinces)
        {
            return ((float)(_componentProvinceIDs.Count(y => provinces[y]._population == Property.High)) * 1.0f) + ((float)(_componentProvinceIDs.Count(y => provinces[y]._population == Property.Medium) * 0.5f)) + ((float)(_componentProvinceIDs.Count(y => provinces[y]._population == Property.Low) * 0.25f));
        }
        public int ReturnTechTotal()
        {
            return milTech + ecoTech + dipTech + logTech + culTech;
        }
        public float ReturnEcoScore(ref List<ProvinceObject> provinces) //Get economics score for this empire
        {
            return ((float)(this.ReturnTechTotal())/5.0f)*(0.1f+
                ((ReturnPopScore(provinces))/20.0f)+((float)(ecoTech)/30.0f));
        }

        public void RecalculateMilMax(List<ProvinceObject> provinces) //Finds the appropriate max military. Redone every time mil is recruited
        {
            maxMil = 25 + Math.Min(100000000,(float)Math.Floor(((float)(milTech) * (0.5f + (ReturnPopScore(provinces)/10.0f)))));
        }
        public float ExpectedMilIncrease(ref List<ProvinceObject> provinces)
        {
            return (float)Math.Round(1.0f+(float)Math.Min(100000000,ReturnEcoScore(ref provinces)/2.0f),2);
        }
        public void RecruitMil(ref List<Culture> cultures, ref List<ProvinceObject> provinces) //Every month recalculate military gain
        {
            RecalculateMilMax(provinces);
            float trueMilExp = ExpectedMilIncrease(ref provinces);
            leftoverMil += trueMilExp - (float)Math.Floor(trueMilExp);
            curMil += (float)Math.Floor(trueMilExp);
            if (leftoverMil >= 1) //If the leftovers are enough to increment
            {
                curMil += (float)Math.Floor(leftoverMil);
                leftoverMil -= (float)Math.Floor(leftoverMil);
            }
            if (curMil > maxMil) { curMil = maxMil; }
        }
        public void PollForAction((int day, int month, int year) currentDate, ref List<Culture> cultures, ref List<Empire> empires)
        {
            if (_exists) //If this empire is active
            {
                AgeMechanics(currentDate, ref cultures, ref empires);
            }
        }
        public void AgeMechanics((int day, int month, int year) currentDate, ref List<Culture> cultures, ref List<Empire> empires)
        {
            if (currentDate.day == curRuler.birthday.day && currentDate.month == curRuler.birthday.month) //On birthday
            {
                curRuler.age++;
            }

            if ((currentDate.day >= curRuler.deathday.day && currentDate.month >= curRuler.deathday.month && curRuler.age == curRuler.deathday.age) || (curRuler.age > curRuler.deathday.age)) //on deathday
            {
                //Replace ruler
                Ruler tmpRuler = new Ruler(curRuler, this, ref cultures, ref empires);
                curRuler = tmpRuler;
            }
        }
        public List<int> ReturnAdjacentIDs(ref List<ProvinceObject> provs)
        {
            return provs.Where(x => x._ownerEmpire == this).SelectMany(y => y._adjacentProvIDs).Distinct().Where(z => !_componentProvinceIDs.Contains(z)).ToList();
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
            {"per_Teach",0 }};

        //Tech focuses (0-5) Military, Economics, Diplomacy, Logistics, Culture
        public float[] techFocus = new float[2];
        public Ruler(Ruler previousRuler, Empire ownedEmpire, ref List<Culture> cultures, ref List<Empire> empires)
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

                techFocus[0] = rulerRND.Next(0, 6);
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

            while (true)
            {
                techFocus[1] = rulerRND.Next(0, 6);
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
        }
        public Ruler()
        {

        }
    }
}