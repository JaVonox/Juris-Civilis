using System.Collections;
using System.Collections.Generic;
using BiomeData;
using WorldProperties;
using UnityEngine;
using System;

public class ProvinceRenderer : MonoBehaviour
{
    public Mesh _provinceMesh;
    public Vector3 _centrePoint; //Worldspace centerpoint
    public ProvinceObject _myProvince; //Stores a reference to province
    private Action _clickActions;
    private Color currentColor;
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

    public void RenderProvinceFromObject(ProvinceObject targetProv, float spriteWidth, float spriteHeight, int mapWidth, int mapHeight, string propType, ref MapObject loadedMap) //Renders based on chunk data.
    {
        SetCentreObject(ref targetProv, mapWidth, mapHeight);

        _myProvince = targetProv; //Copies province reference
        _provinceMesh = new Mesh();

        List<int> triangles = new List<int>();
        Vector3[] verticesSet = new Vector3[targetProv._vertices.Count]; //storage for vertices

        verticesSet[0] = ChangeSpace(_centrePoint, spriteWidth, spriteHeight, mapWidth, mapHeight);


        int i = 0;
        for(int iter = 0; iter < targetProv._vertices.Count; iter+=3)
        {
            for (int v = 0; v < 3; v++)
            {
                verticesSet[i + v] = ChangeSpace(GetCentreRelative(targetProv._vertices[i + v]), spriteWidth, spriteHeight, mapWidth, mapHeight); //Calculate the -/+ value for the vertex based on the midpoint
            }

            //Winding order defines the face of a triangle
            //For unity, the triangle vertices must be in clockwise order to face the front.
            AppendInOrder(ref triangles, ref verticesSet, ref i);
        }

        //add vertices to mesh
        _provinceMesh.vertices = verticesSet;

        //add generated triangles to mesh
        int[] arrTriangles = triangles.ToArray();
        _provinceMesh.triangles = arrTriangles;

        //Set a colour for the polygon
        Color[] colours = new Color[verticesSet.Length];

        currentColor = GetColour(targetProv, propType, ref loadedMap.cultures);

        for (int c = 0; c < verticesSet.Length; c++)
        {
            colours[c] = currentColor;
        }

        _provinceMesh.colors = colours;
        _provinceMesh.RecalculateNormals();

        //assign mesh to filter and collider
        GetComponent<MeshFilter>().sharedMesh = _provinceMesh;
        GetComponent<MeshCollider>().sharedMesh = GetComponent<MeshFilter>().sharedMesh;
    }
    public void UpdateMesh(string propType, ref MapObject loadedMap)
    {
        Color[] colours = new Color[_provinceMesh.vertices.Length];

        currentColor = GetColour(_myProvince, propType, ref loadedMap.cultures);

        for (int c = 0; c < _provinceMesh.vertices.Length; c++)
        {
            colours[c] = currentColor;
        }

        _provinceMesh.colors = colours;
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

    private Color GetColour(ProvinceObject targetProv, string propType, ref List<Culture> cultures) //Returns colours based on parameters
    {
        //Constants
        Color highVal = new Color(0,1,0.014f,0.6f);
        Color medVal = new Color(0.81f, 0.56f, 0 ,0.6f);
        Color lowVal = new Color(1, 0.014f, 0, 0.6f);
        Color NAVal = new Color(0, 0, 0,0.5f);

        switch (propType)
        {
            case "Geography":
                Color geoCol = targetProv._provCol;
                geoCol.a = 0;
                return geoCol;
            case "National":
                Color tmpCol = targetProv._provCol;
                tmpCol.a = 0.4f;
                return tmpCol;
            case "Elevation":
                switch(targetProv._elProp)
                {
                    case Property.High:
                        return lowVal;
                    case Property.Medium:
                        return medVal;
                    case Property.Low:
                        return highVal;
                    case Property.NA:
                        return NAVal;
                }
                break;
            case "Temperature":
                switch (targetProv._tmpProp)
                {
                    case Property.High:
                        return highVal;
                    case Property.Medium:
                        return medVal;
                    case Property.Low:
                        return lowVal;
                    case Property.NA:
                        return NAVal;
                }
                break;
            case "Rainfall":
                switch (targetProv._rainProp)
                {
                    case Property.High:
                        return highVal;
                    case Property.Medium:
                        return medVal;
                    case Property.Low:
                        return lowVal;
                    case Property.NA:
                        return NAVal;
                }
                break;
            case "Flora":
                switch (targetProv._floraProp)
                {
                    case Property.High:
                        return highVal;
                    case Property.Medium:
                        return medVal;
                    case Property.Low:
                        return lowVal;
                    case Property.NA:
                        return NAVal;
                }
                break;
            case "Culture":
                Color cultCol = cultures[targetProv._cultureID]._cultureCol;
                cultCol.a = 0.7f;
                return cultCol;
            default:
                break;
        }

        return new Color(0.85f, 0, 0.6f,1); //Error Colour

    }
    public void FocusProvince() //Updates colours to focus mode
    {
        Color[] colours = new Color[_provinceMesh.vertices.Length];

        for (int c = 0; c < _provinceMesh.vertices.Length; c++)
        {
            colours[c] = new Color(currentColor.r,currentColor.g,currentColor.b,0.9f); //Selected province is fully opaque
        }

        _provinceMesh.colors = colours;
    }

    public void UnfocusProvince()
    {
        Color[] colours = new Color[_provinceMesh.vertices.Length];

        for (int c = 0; c < _provinceMesh.vertices.Length; c++)
        {
            colours[c] = currentColor; //Returns province to initial colours
        }

        _provinceMesh.colors = colours;
    }

    void OnMouseDown() //On click, process all the valid actions
    {
        _clickActions();
    }

}
