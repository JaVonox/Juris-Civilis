using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; //objects
using BiomeData;
using WorldProperties;
using SaveLoad;
using Empires;
public class LoadMap : MonoBehaviour
{
    //Mapping elements
    private int mapWidth = 6000;
    private int mapHeight = 4000;
    public GameObject backMap;
    public GameObject maskMap;
    public GameObject loadedObjectsLayer;
    private GameObject selectorObject;
    private Texture2D backTexture;
    private Texture2D maskTexture;
    private Sprite mapSprite;
    private Sprite maskSprite;


    //Gets the unit length of the sprite
    float spriteWidth;
    float spriteHeight;

    //Script elements
    public GameObject provincePrefab;
    public GameObject selectorPrefab;

    private int selectedProvince; 
    private System.Random rnd = new System.Random();
    private Dictionary<int, GameObject> provinceSet = new Dictionary<int, GameObject>();

    public List<ProvinceObject> _provincesLoaded;
    public List<Culture> _culturesLoaded;
    public List<Religion> _religionsLoaded;
    public List<Empire> _empiresLoaded;

    //Asset refs
    private GameObject _mapModesPanel;
    private GameObject _provDetails;

    void Start()
    {
        selectedProvince = -1;

        selectorObject = Instantiate(selectorPrefab, loadedObjectsLayer.transform, false); //Add selector object - starts invisible
        selectorObject.name = "Selector";
        selectorObject.GetComponent<Selector>().SetData(ref selectorObject);
    }
    public void ApplyProperties(int mWidth, int mHeight, ref List<ProvinceObject> provLoad, ref List<Culture> cultLoad, ref GameObject mapModesPanel, ref GameObject detailsPanel, ref Texture2D mapTexture, ref Texture2D mask, ref List<Religion> religions, ref List<Empire> empires)
    {
        mapWidth = mWidth;
        mapHeight = mHeight;
        _provincesLoaded = provLoad;
        _culturesLoaded = cultLoad;
        _religionsLoaded = religions;
        _empiresLoaded = empires;

        _mapModesPanel = mapModesPanel;
        _provDetails = detailsPanel;

        backTexture = new Texture2D(mapWidth, mapHeight);
        backTexture = mapTexture;

        maskTexture = new Texture2D(mapWidth, mapHeight);
        maskTexture = mask;
        InitialiseMaps();
        
    }
    public void StartMap(ref GameObject mainCam)
    {
        _mapModesPanel.GetComponent<SidebarHandler>().AppendListener(UpdateMapMode);
        _mapModesPanel.GetComponent<SidebarHandler>().SetCameraBtns(mainCam);
    }
    public void PreLoadMap()
    {
        backTexture = new Texture2D(mapWidth, mapHeight);

        mapSprite = Sprite.Create(backTexture, new Rect(0, 0, mapWidth, mapHeight), Vector2.zero);
        backMap.GetComponent<SpriteRenderer>().sprite = mapSprite;

        Color[] pixSet = new Color[mapWidth * mapHeight]; //1D set of pixels

        for (int i = 0; i < mapWidth * mapHeight; i++)
        {
            pixSet[i] = new Color(0.61f, 0.4f, 0.23f); //Default colour
        }

        backTexture.SetPixels(pixSet, 0); //sets all the pixels
        backTexture.Apply();

        Bounds spriteBounds = mapSprite.bounds; //Get boundaries of the sprite in units
        spriteWidth = spriteBounds.size.x;
        spriteHeight = spriteBounds.size.y;


    }
    public void InitialiseMaps()
    {
        mapSprite = Sprite.Create(backTexture, new Rect(0, 0, mapWidth, mapHeight), Vector2.zero); //Apply backmap
        backMap.GetComponent<SpriteRenderer>().sprite = mapSprite;
        backTexture.Apply();

        maskSprite = Sprite.Create(maskTexture, new Rect(0, 0, mapWidth, mapHeight), Vector2.zero); //Apply masking
        maskMap.GetComponent<SpriteRenderer>().sprite = maskSprite;
        maskTexture.Apply();

        Bounds spriteBounds = mapSprite.bounds; //Get boundaries of the sprite in units
        spriteWidth = spriteBounds.size.x;
        spriteHeight = spriteBounds.size.y;
    }

    public void UpdateMapMode(string mapMode)
    {
        foreach (ProvinceObject tProv in _provincesLoaded) //Loop through and display all provinces
        {
            if (!provinceSet.ContainsKey(tProv._id)) // On the first setting of the mapmode
            {
                provinceSet.Add(tProv._id, Instantiate(provincePrefab, loadedObjectsLayer.transform, false));//Instantiate in local space of parent
                provinceSet[tProv._id].gameObject.name = "Prov_" + tProv._id;
                provinceSet[tProv._id].GetComponent<ProvinceRenderer>().RenderProvinceFromObject(tProv, spriteWidth, spriteHeight, mapWidth, mapHeight, mapMode, ref _culturesLoaded, ref _religionsLoaded, ref _provincesLoaded, ref _empiresLoaded );

                Vector3 newCentre = provinceSet[tProv._id].GetComponent<ProvinceRenderer>().ReturnCentreUnitSpace(spriteWidth, spriteHeight, mapWidth, mapHeight);
                provinceSet[tProv._id].transform.Translate(newCentre.x, newCentre.y, newCentre.z); //set the unitspace based provinces
                provinceSet[tProv._id].transform.Rotate(180, 180, 0); //Flip to correct orientation
                provinceSet[tProv._id].GetComponent<ProvinceRenderer>().SetClickAction(SelectProvince); //Append click action to allow selecting of province
            }
            else
            {
                provinceSet[tProv._id].GetComponent<ProvinceRenderer>().UpdateMesh(mapMode, ref _culturesLoaded, ref _religionsLoaded, ref _provincesLoaded, ref _empiresLoaded); //Updates the colours for the mesh for the appropriate mapmode
            }
        }
    }

    public void SelectProvince(ProvinceObject provToDisplay) //Updates province click
    {
        if (selectedProvince != provToDisplay._id)
        {

            if (provToDisplay._biome == 0)
            {
                provinceSet[provToDisplay._id].transform.Translate(0, 0, -15); //Move selected province forward (Oceans go infront of mask)
            }
            else
            {
                provinceSet[provToDisplay._id].transform.Translate(0, 0, -1); //Move selected province forward
            }

            provinceSet[provToDisplay._id].GetComponent<ProvinceRenderer>().FocusProvince(); //set new province into focus mode

            if (selectedProvince != -1)
            {
                if (_provincesLoaded[selectedProvince]._biome == 0)
                {
                    provinceSet[selectedProvince].transform.Translate(0, 0, 15); //Move selected province back to original position
                }
                else
                {
                    provinceSet[selectedProvince].transform.Translate(0, 0, 1);  //Move selected province back to original position
                }

                provinceSet[selectedProvince].GetComponent<ProvinceRenderer>().UnfocusProvince(); //unfocus previous province
            }

            selectorObject.GetComponent<Selector>().MoveSelector(provinceSet[provToDisplay._id].GetComponent<ProvinceRenderer>()._provinceMesh, provinceSet[provToDisplay._id].gameObject, provinceSet[provToDisplay._id].GetComponent<ProvinceRenderer>()._centrePoint);
            selectedProvince = provToDisplay._id;
        }

        _provDetails.GetComponent<ProvinceViewerBehaviour>().DisplayProvince(provToDisplay, ref _culturesLoaded, ref _provincesLoaded); //Change province viewer screen
    }
}
