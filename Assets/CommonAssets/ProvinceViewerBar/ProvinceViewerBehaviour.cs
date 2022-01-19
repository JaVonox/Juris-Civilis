using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BiomeData;
using WorldProperties;

public class ProvinceViewerBehaviour : MonoBehaviour
{

    public GameObject container;
    public Text provName;
    public Text biomeName;
    public Text tempVal;
    public Text rainVal;
    public Text florVal;
    public Text cultureVal;
    public void Start()
    {
        container.SetActive(false);
    }
    public void DisplayProvince(ProvinceObject newSelection, ref List<Culture> cultures) //Updates the selection based on the provincial data provided
    {
        provName.text = newSelection._cityName.ToString();
        biomeName.text = BiomesObject.activeBiomes[newSelection._biome]._name.ToString();
        tempVal.text = "Temperature: " + newSelection._tmpProp;
        rainVal.text = "Rainfall: " + newSelection._rainProp;
        florVal.text = "Flora: " + newSelection._floraProp;
        cultureVal.text = "Culture: " + cultures[newSelection._cultureID]._name;
        container.SetActive(true);
    }
}
