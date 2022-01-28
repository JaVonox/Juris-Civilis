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
    public Text popVal;
    public List<Button> infoModeBtns = new List<Button>();
    public InfoMode activeInfoMode;
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
    enum PopulationEnum
    {
        Village = 0,
        Town = 1,
        City = 2,
        Empty = 3
    }
    public enum InfoMode
    {
        Basic = 0,
        Debug = 1,
        Close = 2
    }

    private Dictionary<int, Action<ProvinceObject>> actionRef = new Dictionary<int, Action<ProvinceObject>>();

    private ProvinceObject lastSelection;
    private List<Culture> culturesSet;
    public void Start()
    {
        container.SetActive(false);
        InitDictionary();
        AppendInfoModes();
    }

    private void InitDictionary()
    {
        actionRef.Add(0, BasicsInfo);
        actionRef.Add(1, DebugInfo);
        actionRef.Add(2, CloseTab);
    }
    public void AppendInfoModes()
    {
        foreach (Button btn in infoModeBtns)
        {
            btn.GetComponent<Button>().onClick.AddListener(delegate { SwitchInfoMode(btn); InteriorUpdate(); }); //add info switching events
        }

        activeInfoMode = (InfoMode)(0);
        SwitchInfoMode(infoModeBtns[0]); //Start by using terrain viewer mode
    }

    void SwitchInfoMode(Button sender)
    {
        activeInfoMode = (InfoMode)(infoModeBtns.IndexOf(sender)); //Set new mapmode

        foreach (Button modeBtn in infoModeBtns) //Swap map images and interactability
        {
            if (modeBtn != sender)
            {
                modeBtn.interactable = true;
                modeBtn.image.sprite = modeBtn.spriteState.highlightedSprite;
            }
            else
            {
                modeBtn.interactable = false;
                modeBtn.image.sprite = modeBtn.spriteState.pressedSprite;
            }
        }

    }
    private void InteriorUpdate() //Update data using interior data
    {
        actionRef[(int)activeInfoMode](lastSelection);
    }
    public void DisplayProvince(ProvinceObject newSelection, ref List<Culture> cultures) //Updates the selection based on the provincial data provided
    {
        lastSelection = newSelection;
        if(culturesSet == null) { culturesSet = cultures; }

        actionRef[(int)activeInfoMode](newSelection);
    }
    void BasicsInfo(ProvinceObject newSelection)
    {
        provName.text = newSelection._cityName.ToString();
        popVal.text = ((PopulationEnum)(int)newSelection._population).ToString();
        biomeName.text = BiomesObject.activeBiomes[newSelection._biome]._name.ToString();
        geoDetailsVal.text = ((CoastalEnum)(Convert.ToInt32(newSelection._isCoastal))).ToString() + "/" + ((HeightEnum)((int)newSelection._elProp)).ToString() + "/" + ((TempEnum)((int)newSelection._tmpProp)).ToString() + "/" + ((RainEnum)((int)newSelection._rainProp)).ToString() + "/" + ((FloraEnum)((int)newSelection._floraProp)).ToString();
        cultureVal.text = "Culture: " + culturesSet[newSelection._cultureID]._name;
        container.SetActive(true);
    }
    void DebugInfo(ProvinceObject newSelection)
    {
        provName.text = newSelection._id.ToString();
        popVal.text = "";
        biomeName.text = "";
        geoDetailsVal.text = "";
        cultureVal.text = "";
        container.SetActive(true);
    }

    void CloseTab(ProvinceObject newSelection)
    {
        //Close container and set info mode to default
        activeInfoMode = (InfoMode)(0);
        SwitchInfoMode(infoModeBtns[0]); //Sets graphic to default info mode
        container.SetActive(false);
    }
}
