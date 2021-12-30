using System.Collections;
using System.Collections.Generic;
using BiomeData;
using UnityEngine;


public class ProvinceRenderer : MonoBehaviour
{
    private Mesh _provinceMesh;

    public void RenderProvince(Province targetProv)
    {
        int verticesCount = targetProv._componentChunks.Count * 3;
        _provinceMesh = new Mesh();

        List<int> triangles = new List<int>();
        Vector3[] verticesSet = new Vector3[verticesCount]; //storage for vertices

        int i = 0;
        foreach (Chunk component in targetProv._componentChunks.Values)
        {
            for (int v = 0; v < 3; v++)
            {
                verticesSet[i+v].x = component.vertices[v].x;
                verticesSet[i+v].y = component.vertices[v].y;
                verticesSet[i+v].z = 0;
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
        //TODO make consistent
        Color[] colours = new Color[verticesCount];
        Color polyCol = new Color(Random.Range(0, 10) / 10f, Random.Range(0, 10) / 10f, Random.Range(0, 10) / 10f);

        for (int c = 0; c < verticesCount; c++)
        {
            colours[c] = polyCol;
        }

        _provinceMesh.colors = colours;

        //assign mesh to filter and collider
        GetComponent<MeshFilter>().sharedMesh = _provinceMesh;
        GetComponent<MeshCollider>().sharedMesh = _provinceMesh;
    }
}
