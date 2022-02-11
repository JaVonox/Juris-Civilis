using System.Collections;
using System.Collections.Generic;
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
        public int _id;
        public string _empireName;
        public List<int> _componentProvinceIDs = new List<int>();
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
        public float percentageEco; //Percentage of the culuture economy owned by this nation

        public float maxMil;
        public float curMil;
        public Empire(int id, string name, ProvinceObject startingProvince, ref List<Culture> cultures, ref List<Empire> empires) //Constructor for an empire - used when a new empire is spawned
        {
            _id = id;
            _empireName = name;
            _componentProvinceIDs.Add(startingProvince._id);
            startingProvince.NewOwner(this); //Append self to set of owner
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

        public float ReturnPopScore(List<ProvinceObject> provinces)
        {
            return (float)_componentProvinceIDs.Count(y => provinces[y]._population == Property.High) * 5 + (float)_componentProvinceIDs.Count(y => provinces[y]._population == Property.Medium) * 2 + (float)_componentProvinceIDs.Count(y => provinces[y]._population == Property.Low) * 0.5f;
        }
        public int ReturnTechTotal()
        {
            return milTech + ecoTech + dipTech + logTech + culTech;
        }
        public void RecalculateMilMax(List<ProvinceObject> provinces) //Finds the appropriate max military. Redone every time mil is recruited
        {
            maxMil = Math.Min(100000000,Math.Max(10,(float)Math.Floor((milTech * 25) * (ReturnPopScore(provinces)/5))));
        }
        public float ExpectedMilIncrease(ref List<Culture> cultures)
        {
            return (float)Math.Floor(Math.Min(maxMil / 3,Math.Max(1, (float)Math.Ceiling(((cultures[_cultureID]._economyScore / (255/(float)(logTech))) * percentageEco)))));
        }
        public void RecruitMil(ref List<Culture> cultures, ref List<ProvinceObject> provinces) //Every month recalculate military gain
        {
            RecalculateMilMax(provinces);
            curMil += ExpectedMilIncrease(ref cultures);
            if (curMil > maxMil) { curMil = maxMil; }
        }
        public void PollForAction((int day, int month, int year) currentDate, ref List<Culture> cultures, ref List<Empire> empires)
        {
            AgeMechanics(currentDate, ref cultures, ref empires);
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
            if (nameBuffer.Count == 0)
            {
                nameBuffer = cultures[ownedEmpire._cultureID].LoadNameBuffer(25, ref rulerRND); //Load 25 names to minimize the amount of file accessing done at a time
            }

            fName = nameBuffer[0];
            nameBuffer.RemoveAt(0);

            if(previousRuler == null) //If no predecessor exists
            {
                lName = cultures[ownedEmpire._cultureID].LoadDynasty(ref empires, ref rulerRND);
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