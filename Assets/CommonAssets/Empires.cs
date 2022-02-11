using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WorldProperties;
using BiomeData;
using Act;
using System;
using System.Linq;

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
        public Empire(int id, string name, ProvinceObject startingProvince) //Constructor for an empire - used when a new empire is spawned
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
    }

    public class Ruler
    {

        public string fName;
        public string lName;
        public int age;
        public Ruler()
        {

        }
    }
}