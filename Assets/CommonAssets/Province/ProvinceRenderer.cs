using System.Collections;
using System.Collections.Generic;
using BiomeData;
using UnityEngine;


public class ProvinceRenderer : MonoBehaviour
{
    private Mesh _provinceMesh;
    private Vector3 _centrePoint; //Worldspace centerpoint

    public Vector3 ReturnCentreUnitSpace(float spriteWidth, float spriteHeight, int mapWidth, int mapHeight)
    {
        return ChangeSpace(_centrePoint, spriteWidth, spriteHeight, mapWidth, mapHeight);
    }

    private void SetCentre(ref Province targetProv, int mapWidth, int mapHeight)
    {
        _centrePoint = targetProv.CalculateRelativeCenterPoint(); //gets the centerpoint relative to the map
    }
    
    private Vector3 GetCentreRelative(Vector3 point) //gets the vertex values with respect to the centerpoint
    {
        Vector3 retVector;

        retVector.x = (_centrePoint.x - point.x);
        retVector.y = (_centrePoint.y - point.y);
        retVector.z = _centrePoint.z;

        return retVector;
    }

    private Vector3 ChangeSpace(Vector3 point, float spriteWidth, float spriteHeight, int mapWidth, int mapHeight)
    {
        //Returns the coordinates based on unity space
        return new Vector3(((float)point.x / (float)mapWidth) * spriteWidth, ((float)point.y / (float)mapHeight) * spriteHeight, point.z);
    }

    public void RenderProvince(Province targetProv, float spriteWidth, float spriteHeight, int mapWidth, int mapHeight)
    {
        SetCentre(ref targetProv,mapWidth,mapHeight);

        int verticesCount = targetProv._componentChunks.Count * 3;
        _provinceMesh = new Mesh();

        List<int> triangles = new List<int>();
        Vector3[] verticesSet = new Vector3[verticesCount + 1]; //storage for vertices

        verticesSet[0] = ChangeSpace(_centrePoint, spriteWidth, spriteHeight, mapWidth, mapHeight);

        int i = 1;

        foreach (Chunk component in targetProv._componentChunks.Values)
        {
            for (int v = 0; v < 3; v++)
            {
                verticesSet[i + v] = ChangeSpace(GetCentreRelative(new Vector3(component.vertices[v].x,component.vertices[v].y,0)), spriteWidth, spriteHeight, mapWidth, mapHeight); //Calculate the -/+ value for the vertex based on the midpoint
            }

            triangles.Add(i);
            triangles.Add(i+1);
            triangles.Add(i+2);

            i += 3;
        }

        //add vertices to mesh
        _provinceMesh.vertices = verticesSet;

        //add generated triangles to mesh
        int[] arrTriangles = triangles.ToArray();
        _provinceMesh.triangles = arrTriangles;

        //Set a colour for the polygon
        Color[] colours = new Color[verticesSet.Length];
        Color polyCol = new Color(Random.Range(0, 10) / 10f, Random.Range(0, 10) / 10f, Random.Range(0, 10) / 10f);

        if(targetProv._startBiome == 0) //TODO temp to identify oceans
        {
            polyCol = new Color(0, 0, 0);
        }

        polyCol.a = 0.5f;

        for (int c = 0; c < verticesSet.Length; c++)
        {
            colours[c] = polyCol;
        }

        _provinceMesh.colors = colours;

        //assign mesh to filter and collider
        GetComponent<MeshFilter>().sharedMesh = _provinceMesh;
        GetComponent<MeshCollider>().sharedMesh = _provinceMesh;
    }
}
