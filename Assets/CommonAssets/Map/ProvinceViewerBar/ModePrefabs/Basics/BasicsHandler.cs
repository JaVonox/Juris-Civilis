using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WorldProperties;
using ProvViewEnums;

public class BasicsHandler : MonoBehaviour
{
    public Text provName;
    public Text biomeName;
    public Text geoDetailsVal;
    public Text cultureVal;
    public Text popVal;

    public void BasicsInfo(ProvinceObject newSelection, List<Culture> culturesSet)
    {
        provName.text = newSelection._cityName.ToString();
        popVal.text = ((PopulationEnum)(int)newSelection._population).ToString();
        biomeName.text = BiomesObject.activeBiomes[newSelection._biome]._name.ToString();
        geoDetailsVal.text = ((CoastalEnum)(Convert.ToInt32(newSelection._isCoastal))).ToString() + "/" + ((HeightEnum)((int)newSelection._elProp)).ToString() + "/" + ((TempEnum)((int)newSelection._tmpProp)).ToString() + "/" + ((RainEnum)((int)newSelection._rainProp)).ToString() + "/" + ((FloraEnum)((int)newSelection._floraProp)).ToString();
        cultureVal.text = "Culture: " + culturesSet[newSelection._cultureID]._name;
    }
}
