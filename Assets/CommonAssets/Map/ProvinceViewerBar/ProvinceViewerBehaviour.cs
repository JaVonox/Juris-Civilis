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
    public Text geoDetailsVal;
    public Text cultureVal;
    enum HeightEnum
    {
        Flat = 0,
        Craggy = 1,
        Alpine = 2,
        Deep = 3,
    }
    enum TempEnum
    {
        Cold = 0,
        Warm = 1,
        Hot = 2,
        Damp = 3,
    }
    enum RainEnum
    {
        Dry = 0,
        NA = 1,
        Wet = 2,
        Harsh = 3,
    }
    enum FloraEnum
    {
        Infertile = 0,
        NA = 1,
        Fertile = 2,
        Barren = 3,
    }
    enum CoastalEnum
    {
        Internal = 0,
        Coastal = 1,
    }

    public void Start()
    {
        container.SetActive(false);
    }
    public void DisplayProvince(ProvinceObject newSelection, ref List<Culture> cultures) //Updates the selection based on the provincial data provided
    {
        provName.text = newSelection._cityName.ToString();
        biomeName.text = BiomesObject.activeBiomes[newSelection._biome]._name.ToString();
        geoDetailsVal.text = ((CoastalEnum)(Convert.ToInt32(newSelection._isCoastal))).ToString() + "/" + ((HeightEnum)((int)newSelection._elProp)).ToString() + "/" + ((TempEnum)((int)newSelection._tmpProp)).ToString() + "/" + ((RainEnum)((int)newSelection._rainProp)).ToString() + "/" + ((FloraEnum)((int)newSelection._floraProp)).ToString();
        cultureVal.text = "Culture: " + cultures[newSelection._cultureID]._name;
        container.SetActive(true);
    }
}
