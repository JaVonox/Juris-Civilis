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
    public Button detailsBtn;

    public GameObject detailsPrefab;
    private GameObject loadedMoreDetails;

    private ProvinceObject newSelec;
    private List<Culture> cultSet;
    private List<ProvinceObject> provSet;
    public void NationalInfo(ref ProvinceObject newSelection, ref List<Culture> culturesSet, ref List<ProvinceObject> provs)
    {
        newSelec = newSelection;
        cultSet = culturesSet;
        provSet = provs;

        if(newSelection._ownerEmpire == null)
        {
            EmpireName.text = "Unowned Land";
            detailsBtn.interactable = false;
        }
        else
        {
            EmpireName.text = "Owner: " + newSelection._ownerEmpire._empireName;
            detailsBtn.onClick.AddListener(LoadDetails);
            detailsBtn.interactable = true;
        }

        ProvName.text = newSelection._cityName;
    }
    public void LoadDetails()
    {
        if(GameObject.Find("EmpireViewer") == null)
        {
            loadedMoreDetails = Instantiate(detailsPrefab);
            loadedMoreDetails.name = "EmpireViewer";
            loadedMoreDetails.GetComponent<EmpireViewer>().UpdateData(ref newSelec._ownerEmpire, ref cultSet, ref provSet);
        }
        else
        {
            GameObject.Find("EmpireViewer").GetComponent<EmpireViewer>().UpdateData(ref newSelec._ownerEmpire, ref cultSet, ref provSet);
        }
    }
}
