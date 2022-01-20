using System.Collections;
using System.Collections.Generic;
using BiomeData;
using WorldProperties;
using UnityEngine;
using System;

public class Selector : MonoBehaviour
{
    //This class is spawned when the system is in display mode. It copies a province mesh but increases scale thereby creating a "border" around the province selected
    private Mesh _selectorMesh;
    private GameObject _selfObject;
    public void SetData(ref GameObject self)
    {
        _selfObject = self;
        _selfObject.GetComponent<MeshRenderer>().enabled = false;
    }
    public void MoveSelector(Mesh meshToCopy, GameObject target, Vector3 centrePoint)
    {
        MeshCopier(meshToCopy, centrePoint);
        _selfObject.transform.position = target.transform.position;
        _selfObject.transform.position = new Vector3(_selfObject.transform.position.x, _selfObject.transform.position.y, _selfObject.transform.position.z + 0.5f); //Move behind selected object
        _selfObject.transform.rotation = Quaternion.Euler(180, 180, 0); //Flip to correct orientation
        _selfObject.transform.localScale = new Vector3(1.1f, 1.1f, 1);

        Color[] colours = new Color[_selectorMesh.vertices.Length];

        for (int c = 0; c < _selectorMesh.vertices.Length; c++)
        {
            colours[c] = new Color(0,0,0,0.7f); //Sets borders to black
        }

        _selectorMesh.colors = colours;

        _selfObject.GetComponent<MeshRenderer>().enabled = true;
    }
    private void MeshCopier(Mesh meshToCopy, Vector3 centrePoint)
    {
        _selectorMesh = new Mesh();

        int[] triangles = new int[meshToCopy.triangles.Length];
        Vector3[] verticesSet = new Vector3[meshToCopy.vertices.Length]; //storage for vertices

        verticesSet[0] = meshToCopy.vertices[0];

        int i = 0;
        for (int iter = 0; iter < meshToCopy.vertices.Length; iter += 3)
        {

            for (int v = 0; v < 3; v++)
            {
                verticesSet[i + v] = meshToCopy.vertices[i + v]; ;
            }

            i+=3;
        }

        for (int iter = 0; iter < meshToCopy.triangles.Length; iter++)
        {
            triangles[iter] = meshToCopy.triangles[iter];
        }

        //add vertices to mesh
        _selectorMesh.vertices = verticesSet;

        //add generated triangles to mesh
        int[] arrTriangles = triangles;
        _selectorMesh.triangles = arrTriangles;

        _selectorMesh.RecalculateNormals();

        //assign mesh to filter - generating a normal
        GetComponent<MeshFilter>().sharedMesh = _selectorMesh;
    }
}
