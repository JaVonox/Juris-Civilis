using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WorldProperties;
using ProvViewEnums;

public class NationalHandler : MonoBehaviour
{
    public Text EmpireName;
    public Text ProvName;

    public void NationalInfo(ProvinceObject newSelection, List<Culture> culturesSet)
    {
        if(newSelection._ownerEmpire == null)
        {
            EmpireName.text = "Unowned Land";
        }
        else
        {
            EmpireName.text = "Owner: " + newSelection._ownerEmpire._empireName;
        }

        ProvName.text = newSelection._cityName;
    }
}
