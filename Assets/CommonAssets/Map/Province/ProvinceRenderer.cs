using System.Collections;
using System.Collections.Generic;
using BiomeData;
using WorldProperties;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI; //objects
using System;
using System.Linq;
using Empires;

public class ProvinceRenderer : MonoBehaviour
{
    public Mesh _provinceMesh;
    public Vector3 _centrePoint; //Worldspace centerpoint
    public ProvinceObject _myProvince; //Stores a reference to province
    private Action _clickActions;
    private Color currentColor;

    private Dictionary<GameObject,float> midText = new Dictionary<GameObject, float>(); //gameobject for displaying text
    public int _meshSize; //Unity has a weird thing where it has to make an array copy every time it wants to check vertices length. Setting this once stops accessing taking up too much data
    public int _triSize;

    public static string _lastMapMode;
    private bool isFocused = false;
    private float unfocusedAlpha = 0.6f; //Stores the would-be-alpha value for unfocusedprovs
    public Vector3 ReturnCentreUnitSpace(float spriteWidth, float spriteHeight, int mapWidth, int mapHeight)
    {
        return ChangeSpace(_centrePoint, spriteWidth, spriteHeight, mapWidth, mapHeight);
    }
    public void SetClickAction(Action<ProvinceObject> newAct)
    {
        _clickActions = (delegate { newAct(_myProvince); });
    }
    private void SetCentreObject(ref ProvinceObject targetProv, int mapWidth, int mapHeight)
    {
        _centrePoint = targetProv.CalculateRelativeCenterPoint(); //gets the centerpoint relative to the map
    }
    private Vector3 GetCentreRelative(Vector3 point) //gets the vertex values with respect to the centerpoint
    {
        Vector3 retVector;

        retVector.x = (_centrePoint.x - point.x);
        retVector.y = (_centrePoint.y - point.y);
        retVector.z = -2;

        return retVector;
    }

    private Vector3 ChangeSpace(Vector3 point, float spriteWidth, float spriteHeight, int mapWidth, int mapHeight)
    {
        //Returns the coordinates based on unity space
        return new Vector3(((float)point.x / (float)mapWidth) * spriteWidth, ((float)point.y / (float)mapHeight) * spriteHeight, -2);
    }

    static float sWidth = -1;
    static float sHeight = -1;
    static int mWidth = -1;
    static int mHeight = -1;
    public void RenderProvinceFromObject(ProvinceObject targetProv, float spriteWidth, float spriteHeight, int mapWidth, int mapHeight, string propType, ref List<Culture> cult, ref List<Religion> religions, ref List<ProvinceObject> provs, ref List<Empire> empires) //Renders based on chunk data.
    {
        SetCentreObject(ref targetProv, mapWidth, mapHeight);

        _myProvince = targetProv; //Copies province reference
        _provinceMesh = new Mesh();

        List<int> triangles = new List<int>();
        Vector3[] verticesSet = new Vector3[targetProv._vertices.Count]; //storage for vertices

        if (sWidth == -1) { sWidth = spriteWidth; }
        if(sHeight == -1) { sHeight = spriteHeight; }
        if(mWidth == -1) { mWidth = mapWidth;}
        if(mHeight == -1) { mHeight = mapHeight; }
        verticesSet[0] = ChangeSpace(_centrePoint, spriteWidth, spriteHeight, mapWidth, mapHeight);


        int i = 0;
        int tprovCount = targetProv._vertices.Count;
        for (int iter = 0; iter < tprovCount; iter+=3)
        {
            for (int v = 0; v < 3; v++)
            {
                verticesSet[i + v] = ChangeSpace(GetCentreRelative(targetProv._vertices[i + v]), spriteWidth, spriteHeight, mapWidth, mapHeight); //Calculate the -/+ value for the vertex based on the midpoint
            }

            //Winding order defines the face of a triangle
            //For unity, the triangle vertices must be in clockwise order to face the front.
            AppendInOrder(ref triangles, ref verticesSet, ref i);
        }
        int vLength = verticesSet.Length;

        //add vertices to mesh
        _provinceMesh.SetVertices(verticesSet);

        //add generated triangles to mesh
        _provinceMesh.SetTriangles(triangles, 0);
        _triSize = triangles.Count;

        //Set a colour for the polygon
        Color[] colours = new Color[vLength];

        currentColor = GetColour(targetProv, propType, ref cult, ref religions, ref provs, ref empires);

        for (int c = 0; c < vLength; c++)
        {
            colours[c] = currentColor;
        }

        _meshSize = _provinceMesh.vertices.Length;

        _provinceMesh.colors = colours;
        _provinceMesh.RecalculateNormals();

        //assign mesh to filter and collider
        GetComponent<MeshFilter>().sharedMesh = _provinceMesh;
        GetComponent<MeshCollider>().sharedMesh = _provinceMesh;
    }
    public void UpdateMesh(string propType, ref List<Culture> cult, ref List<Religion> religions, ref List<ProvinceObject> provs, ref List<Empire> empires)
    {
        Color[] colours = new Color[_meshSize];

        currentColor = GetColour(_myProvince, propType, ref cult, ref religions, ref provs, ref empires);

        for (int c = 0; c < _meshSize; c++)
        {
            colours[c] = currentColor;
        }

        _provinceMesh.SetColors(colours);
    }
    private void TextUpdate()
    {
        GameObject tmpGame = new GameObject();
        TextMesh t = tmpGame.AddComponent<TextMesh>();
        tmpGame.name = "Prov" + _myProvince._id + "TMESH";
        tmpGame.transform.SetParent(this.transform);
        t.text = _myProvince.updateText;
        t.fontSize = 30;
        t.color = Color.magenta;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAlignment.Center;
        t.anchor = TextAnchor.MiddleCenter;

        tmpGame.transform.localPosition = new Vector3(0, 0, -9); //Display over ocean + selection
        tmpGame.transform.localScale = new Vector3(0.2f, 0.2f);

        midText.Add(tmpGame, 2);

        _myProvince.updateText = "";
    }
    private void AppendInOrder(ref List<int> triangles, ref Vector3[] vertices, ref int i)
    {
        //This should only use the last three vertices appended
        //For this polygon structure, we can sort into counter clockwise order by:
        //find a midpoint for the triangle and then calculate the angle of each vertex to the midpoint, then sort in the order of angles
        //To get clockwise, we can just flip this set

        Vector3 midPoint = new Vector3((vertices[i].x + vertices[i + 1].x + vertices[i + 2].x) / 3, (vertices[i].y + vertices[i + 1].y + vertices[i + 2].y) / 3, vertices[i].z);
        //Gets the midpoint of the triangle

        Dictionary<float, int> idToAngle = new Dictionary<float, int>();
        List<float> anglesToSort = new List<float>();

        for (int l = i; l < i + 3; l++)
        {
            //Finds angle and appends to set
            float xDiff = vertices[l].x - midPoint.x;
            float yDiff = vertices[l].y - midPoint.y;
            float angleToMid = (float)(Math.Atan2(yDiff, xDiff) * 180.0 / Math.PI);

            if(idToAngle.ContainsKey(angleToMid)) { idToAngle.Add(angleToMid - 0.1f, l); }
            else { idToAngle.Add(angleToMid, l); }
            anglesToSort.Add(angleToMid);
        }

        anglesToSort.Sort(); //Sorts angles
        
        //append in reverse order to set to the correct face
        triangles.Add(idToAngle[anglesToSort[2]]);
        triangles.Add(idToAngle[anglesToSort[1]]);
        triangles.Add(idToAngle[anglesToSort[0]]);

        i+=3;
    }

    private Color GetColour(ProvinceObject targetProv, string propType, ref List<Culture> cultures, ref List<Religion> religions, ref List<ProvinceObject> provs, ref List<Empire> empires) //Returns colours based on parameters
    {
        //Constants
        Color highVal = new Color(0,1,0.014f,0.6f);
        Color medVal = new Color(0.81f, 0.56f, 0 ,0.6f);
        Color lowVal = new Color(1, 0.014f, 0, 0.6f);
        Color NAVal = new Color(0, 0, 0,0.5f);
        Color invVal = new Color(0, 0, 0, 0);

        if (isFocused) { 
            highVal.a = 0.9f;
            medVal.a = 0.9f;
            lowVal.a = 0.9f;
            NAVal.a = 0.9f;
        };

        if (propType == "-1") //Use last map mode command
        {
            propType = _lastMapMode;
        }
        else if(_lastMapMode != propType)
        {
            _lastMapMode = propType; //Update last stored map mode
        }

        try
        {
            switch (propType)
            {
                case "Geography":
                    {
                        Color geoCol = targetProv._provCol;
                        if (!isFocused) { geoCol.a = 0; };
                        unfocusedAlpha = 0;
                        return geoCol;
                    }
                case "National":
                    {
                        Color nationalCol;
                        if (targetProv._ownerEmpire != null)
                        {
                            nationalCol = targetProv._ownerEmpire._empireCol;
                            if (!isFocused) { nationalCol.a = 0.85f; } else { nationalCol.a = 0.9f; };
                            unfocusedAlpha = 0.85f;
                        }
                        else
                        {
                            nationalCol = targetProv._provCol;
                            if (!isFocused) { nationalCol.a = 0; } else { nationalCol.a = 0.9f; };
                            unfocusedAlpha = 0;
                        }

                        return nationalCol;
                    }
                case "Provinces":
                    {
                        Color provCols = targetProv._provCol;
                        if (!isFocused) { provCols.a = 0.4f; } else { provCols.a = 0.9f; };
                        unfocusedAlpha = 0.4f;
                        return provCols;
                    }
                case "Elevation":
                    {
                        Color eCol = new Color(0, 0, 0, 1);
                        switch (targetProv._elProp)
                        {
                            case Property.High:
                                eCol = lowVal;
                                break;
                            case Property.Medium:
                                eCol = medVal;
                                break;
                            case Property.Low:
                                eCol = highVal;
                                break;
                            case Property.NA:
                                eCol = NAVal;
                                break;
                        }
                        if (!isFocused) { eCol.a = 0.6f; } else { eCol.a = 0.9f; };
                        unfocusedAlpha = 0.6f;
                        return eCol;
                    }
                case "Temperature":
                    {
                        Color tCol = new Color(0, 0, 0, 1);
                        switch (targetProv._tmpProp)
                        {
                            case Property.High:
                                tCol = highVal;
                                break;
                            case Property.Medium:
                                tCol = medVal;
                                break;
                            case Property.Low:
                                tCol = lowVal;
                                break;
                            case Property.NA:
                                tCol = NAVal;
                                break;
                        }
                        if (!isFocused) { tCol.a = 0.6f; } else { tCol.a = 0.9f; };
                        unfocusedAlpha = 0.6f;
                        return tCol;
                    }
                case "Rainfall":
                    {
                        Color rCol = new Color(0, 0, 0, 1);
                        switch (targetProv._rainProp)
                        {
                            case Property.High:
                                rCol = highVal;
                                break;
                            case Property.Medium:
                                rCol = medVal;
                                break;
                            case Property.Low:
                                rCol = lowVal;
                                break;
                            case Property.NA:
                                rCol = NAVal;
                                break;
                        }
                        if (!isFocused) { rCol.a = 0.6f; } else { rCol.a = 0.9f; };
                        unfocusedAlpha = 0.6f;
                        return rCol;
                    }
                case "Flora":
                    {
                        Color fCol = new Color(0, 0, 0, 1);
                        switch (targetProv._floraProp)
                        {
                            case Property.High:
                                fCol = highVal;
                                break;
                            case Property.Medium:
                                fCol = medVal;
                                break;
                            case Property.Low:
                                fCol = lowVal;
                                break;
                            case Property.NA:
                                fCol = NAVal;
                                break;
                        }
                        if (!isFocused) { fCol.a = 0.6f; } else { fCol.a = 0.9f; };
                        unfocusedAlpha = 0.6f;
                        return fCol;
                    }
                case "Culture":
                    {
                        Color cultCol = cultures[targetProv._cultureID]._cultureCol;
                        if (!isFocused) { cultCol.a = 0.7f; } else { cultCol.a = 0.9f; };
                        unfocusedAlpha = 0.7f;
                        return cultCol;
                    }
                case "Population":
                    {
                        Color pCol = new Color(0, 0, 0, 1);
                        switch (targetProv._population)
                        {
                            case Property.High:
                                pCol = highVal;
                                break;
                            case Property.Medium:
                                pCol = medVal;
                                break;
                            case Property.Low:
                                pCol = lowVal;
                                break;
                            case Property.NA:
                                pCol = NAVal;
                                break;
                        }
                        if (!isFocused) { pCol.a = 0.6f; } else { pCol.a = 0.9f; };
                        unfocusedAlpha = 0.6f;
                        return pCol;
                    }
                case "Economy":
                    {
                        if (provs.Where(tprov => tprov._cultureID == targetProv._cultureID && tprov._ownerEmpire != null).Count() < 1) { unfocusedAlpha = 0.5f; return NAVal; }
                        List<float> ecoVals = cultures.Select(tC => tC._economyScore).ToList();
                        float minEco = ecoVals.Min();
                        float maxEco = ecoVals.Max();
                        float normalScoreEco = (cultures[targetProv._cultureID]._economyScore - minEco) / ((float)(maxEco - minEco) + 0.001f);
                        Color interpEcoVal = Color.Lerp(lowVal, highVal, normalScoreEco);
                        if (!isFocused) { interpEcoVal.a = 0.6f; } else { interpEcoVal.a = 0.9f; };
                        unfocusedAlpha = 0.6f;
                        return interpEcoVal;
                    }
                case "LocalEconomy":
                    {
                        if (targetProv._ownerEmpire == null) { unfocusedAlpha = 0.5f; return NAVal; }
                        float lMaxEco = (empires.Where(x => x._cultureID == targetProv._ownerEmpire._cultureID && x._exists).Max(x => x.percentageEco));
                        float lMinEco = (empires.Where(x => x._cultureID == targetProv._ownerEmpire._cultureID && x._exists).Min(x => x.percentageEco));
                        float lNormal = (targetProv._ownerEmpire.percentageEco - lMinEco) / (lMaxEco - lMinEco);
                        if(targetProv._ownerEmpire.percentageEco >= 0.99f) { lNormal = 1.0f; }
                        Color localEco = Color.Lerp(lowVal, highVal, lNormal);
                        if (!isFocused) { localEco.a = 0.6f; } else { localEco.a = 0.9f; };
                        unfocusedAlpha = 0.6f;
                        return localEco;
                    }
                case "Tech":
                    {
                        if (targetProv._ownerEmpire == null) { unfocusedAlpha = 0.5f; return NAVal; }
                        float minTech = 4;
                        float maxTech = empires.Select(emp => emp.ReturnTechTotal()).ToList().Max();
                        float normalScoreTech = (((float)targetProv._ownerEmpire.ReturnTechTotal()) - minTech) / ((float)(maxTech - minTech) + 0.001f);
                        Color techVal = Color.Lerp(lowVal, highVal, Math.Max(0, Math.Min(1, normalScoreTech)));
                        if (!isFocused) { techVal.a = 0.6f; } else { techVal.a = 0.9f; };
                        unfocusedAlpha = 0.6f;
                        return techVal;
                    }
                case "Religion":
                    {
                        if (targetProv._localReligion == null) { unfocusedAlpha = 0.5f; return NAVal; }
                        Color locRel = targetProv._localReligion._col;
                        if (!isFocused) { locRel.a = 0.6f; } else { locRel.a = 0.9f; };
                        unfocusedAlpha = 0.6f;
                        return locRel;
                    }
                case "StateReligion":
                    {
                        if (targetProv._ownerEmpire == null) { unfocusedAlpha = 0; return invVal; }
                        if (targetProv._ownerEmpire.stateReligion == null) { unfocusedAlpha = 0.5f; return NAVal; }
                        Color statRel = targetProv._ownerEmpire.stateReligion._col;
                        if (!isFocused) { statRel.a = 0.6f; } else { statRel.a = 0.9f; };
                        unfocusedAlpha = 0.6f;
                        return statRel;
                    }
                case "Military":
                    {
                        if (targetProv._ownerEmpire == null) { unfocusedAlpha = 0.5f; return NAVal; }
                        float minMin = 0;
                        float maxMil = empires.Select(emp => emp.curMil).ToList().Max();
                        float normalScoreMil = (((float)targetProv._ownerEmpire.curMil) - minMin) / ((float)(maxMil - minMin) + 0.001f);
                        Color milVal = Color.Lerp(lowVal, highVal, Math.Max(0, Math.Min(1, normalScoreMil)));
                        if (!isFocused) { milVal.a = 0.6f; } else { milVal.a = 0.9f; };
                        unfocusedAlpha = 0.6f;
                        return milVal;
                    }
                case "Language":
                    {
                        Color lCol = new Color(0, 0, 0, 1);
                        string cultureLang = cultures[targetProv._cultureID]._nameType;
                        unfocusedAlpha = 0.6f;
                        if (targetProv._biome == 0) { unfocusedAlpha = 0; return invVal; }
                        switch (cultureLang)
                        {
                            case "Asian":
                                lCol = new Color(0.98f, 0.53f, 0.01f, 0.6f);
                                break;
                            case "Colonial":
                                lCol = new Color(0.98f, 0.01f, 0.01f, 0.6f);
                                break;
                            case "European":
                                lCol = new Color(0.01f, 0.19f, 0.98f, 0.6f);
                                break;
                            case "Indian":
                                lCol = new Color(0.54f, 0.01f, 0.98f, 0.6f);
                                break;
                            case "Muslim":
                                lCol = new Color(0.01f, 0.98f, 0.04f, 0.6f);
                                break;
                            case "Latin":
                                lCol = new Color(0.91f, 0.16f, 0.78f, 0.6f);
                                break;
                            case "Pacific":
                                lCol = new Color(1, 1, 0, 0.6f);
                                break;
                            default:
                                lCol = invVal;
                                break;
                        }
                        if (!isFocused) { lCol.a = 0.6f; } else { lCol.a = 0.9f; };
                        unfocusedAlpha = 0.6f;
                        return lCol;
                    }
                default:
                    unfocusedAlpha = 1;
                    return new Color(0.85f, 0, 0.6f, 1); //Error Colour

            }
        }
        catch (Exception ex)
        {
            Debug.Log("Province Error - " + ex.ToString());
            return new Color(0.85f, 0, 0.6f, 1); //Error Colour
        }

    }
    public void FocusProvince() //Updates colours to focus mode
    {
        isFocused = true;
        Color[] colours = new Color[_meshSize];

        for (int c = 0; c < _meshSize; c++)
        {
            colours[c] = new Color(currentColor.r,currentColor.g,currentColor.b,0.9f); //Selected province is fully opaque
        }

        _provinceMesh.SetColors(colours);
    }

    public void UnfocusProvince()
    {
        isFocused = false;
        Color[] colours = new Color[_meshSize];

        Color tmpCol = currentColor;
        tmpCol.a = unfocusedAlpha;

        for (int c = 0; c < _meshSize; c++)
        {
            colours[c] = tmpCol; //Returns province to initial colours
        }

        _provinceMesh.SetColors(colours);
    }

    void Update()
    {

        if (_myProvince.updateText != "")
        {
            TextUpdate(); //Add new text update
        }

        if (midText.Count > 0)
        {
            List<GameObject> midIDs = midText.Keys.ToList();
            List<GameObject> destructables = new List<GameObject>();
            int midTextCount = midText.Count;
            float dTime = Time.deltaTime;

            foreach (GameObject m in midIDs) 
            {
                midText[m] -= dTime;

                if (midText[m] <= 0 && midText != null)
                {
                    destructables.Add(m);
                }
                else if (midText != null)
                {
                    m.transform.localPosition += new Vector3(0, -(0.5f * Time.deltaTime));
                }

                if (_myProvince.updateText != "")
                {
                    TextUpdate();
                }
            }

            int dCount = destructables.Count;
            for (int i = 0; i < dCount; i++)
            {
                midText.Remove(destructables[i]);
                Destroy(destructables[i]);
            }
        }
    }
    void OnMouseDown() //On click, process all the valid actions
    {
        if (!EventSystem.current.IsPointerOverGameObject()) //Stops clicks through the UI elements
        {
            _clickActions();
        }
    }

}
