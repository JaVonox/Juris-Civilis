using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WorldProperties;
using BiomeData;
using Act;

namespace Empires //Handles empires and their existance. Actions they may take are in Actions.cs
{
    public class Empire
    {
        public int _id;
        public string _empireName;
        public List<ProvinceObject> _componentProvinces = new List<ProvinceObject>();
        public Color _empireCol;
        public Empire(int id, string name, ProvinceObject startingProvince) //Constructor for an empire - used when a new empire is spawned
        {
            _id = id;
            _empireName = name;
            _componentProvinces.Add(startingProvince);
            startingProvince.NewOwner(this); //Append self to set of owner
            _empireCol = startingProvince._provCol;
        }
    }
}