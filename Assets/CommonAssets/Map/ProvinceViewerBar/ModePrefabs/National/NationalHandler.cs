using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WorldProperties;
using ProvViewEnums;
using Empires;
using System.Linq;
public class NationalHandler : MonoBehaviour
{
    public Text EmpireName;
    public Text ProvName;
    public Button detailsBtn;
    public Image empireFlag;

    public GameObject detailsPrefab;
    private GameObject loadedMoreDetails;

    private ProvinceObject newSelec;
    private List<Culture> cultSet;
    private List<ProvinceObject> provSet;
    private List<Empire> empSet;

    //Summary Details
    public Text cultureText;
    public Text milText;
    public Text rulerText;
    public Text techText;
    public Text warsText;

    public float updateCounter;
    public void NationalInfo(ref ProvinceObject newSelection, ref List<Culture> culturesSet, ref List<ProvinceObject> provs, ref List<Empire> empires)
    {
        newSelec = newSelection;
        cultSet = culturesSet;
        provSet = provs;
        empSet = empires;
        updateCounter = 0;

        if (newSelection._ownerEmpire == null)
        {
            EmpireName.text = "Unowned Land";
            detailsBtn.interactable = false;
            empireFlag.color = Color.white;
            cultureText.text = "Unowned";
            milText.text = "";
            rulerText.text = "";
            techText.text = "";
            warsText.text = "";
        }
        else
        {
            Empire empireOwner = newSelection._ownerEmpire;
            EmpireName.text = "Owner: " + empireOwner._empireName;
            detailsBtn.onClick.AddListener(LoadDetails);
            detailsBtn.interactable = true;
            empireFlag.color = empireOwner._empireCol;
            cultureText.text = "Culture: " + culturesSet[empireOwner._cultureID]._name + " (" + Math.Round(empireOwner.percentageEco * 100.0f,0) + "%)";
            milText.text = "Military: " + Math.Round(empireOwner.curMil, 0) + "/" + Math.Round(empireOwner.maxMil, 0);

            Ruler tRuler = newSelection._ownerEmpire.curRuler;
            string lanType = culturesSet[empireOwner._cultureID]._nameType;
            switch (lanType) //Switch naming scheme for different languages
            {
                case "Asian":
                    rulerText.text = "Ruler: " + tRuler.lName + " " + tRuler.fName;
                    break;
                case "Pacific":
                    rulerText.text = "Ruler: " + tRuler.fName;
                    break;
                default:
                    rulerText.text = "Ruler: " + tRuler.fName + " " + tRuler.lName;
                    break;
            }

            techText.text = "Tech Scores: M:" + empireOwner.milTech + " E:" + empireOwner.ecoTech + " D:" + empireOwner.dipTech + " L:" + empireOwner.logTech + " C:" + empireOwner.culTech;

            if(empireOwner.opinions.Any(x=>x.Value._isWar == true) || empireOwner.rebels.Count > 0)
            {
                warsText.text = "Wars: " + empireOwner.opinions.Count(x => x.Value._isWar) + " Rebels: " + empireOwner.rebels.Count;
            }
            else
            {
                warsText.text = "At Peace";
            }
        }

        ProvName.text = newSelection._cityName;
    }
    void Update()
    {
        updateCounter += Time.deltaTime;

        if (updateCounter >= 0.5f)
        {
            NationalInfo(ref newSelec,ref cultSet,ref provSet,ref empSet);
            updateCounter = 0;
        }
    }

    public void LoadDetails()
    {
        if(GameObject.Find("EmpireViewer") == null)
        {
            loadedMoreDetails = Instantiate(detailsPrefab);
            loadedMoreDetails.name = "EmpireViewer";
            loadedMoreDetails.GetComponent<EmpireViewer>().UpdateData(newSelec._ownerEmpire, ref cultSet, ref provSet, empSet);
        }
        else
        {
            GameObject.Find("EmpireViewer").GetComponent<EmpireViewer>().UpdateData( newSelec._ownerEmpire, ref cultSet, ref provSet,  empSet);
        }
    }
}
