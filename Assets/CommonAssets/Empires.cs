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
        public Empire(int id, string name, ProvinceObject startingProvince) //Constructor for an empire - used when a new empire is spawned
        {
            _id = id;
            _empireName = name;
            _componentProvinceIDs.Add(startingProvince._id);
            startingProvince.NewOwner(this); //Append self to set of owner
            _empireCol = startingProvince._provCol;
        }
        public Empire() //For loading
        {

        }
    }
}