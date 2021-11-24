using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraScript : MonoBehaviour
{

    const float cameraSpeed = 30f;

    public static float zoomSpeed = 0;
    const float zoomAcceleration = 0.2f;
    const float zoomSpeedCap = 1;

    const int maxZoomOrtho = 60;
    const int minZoomOrtho = 5;

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
        {
            transform.Translate(new Vector3(cameraSpeed * Time.deltaTime, 0, 0));
        }
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
        {
            transform.Translate(new Vector3(-cameraSpeed * Time.deltaTime, 0, 0));
        }
        if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
        {
            transform.Translate(new Vector3(0, -cameraSpeed * Time.deltaTime, 0));
        }
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
        {
            transform.Translate(new Vector3(0, cameraSpeed * Time.deltaTime, 0));
        }

        if ((Input.mouseScrollDelta.y > 0 || Input.GetKeyUp(KeyCode.PageUp)) && Camera.main.orthographicSize > minZoomOrtho) //checks for mouse wheel up or page up selected (for zooming in). also checks against the zoom cap of 20
        {
            zoomSpeed = System.Math.Max(-zoomSpeedCap, zoomSpeed > 0 ? -zoomAcceleration : (zoomSpeed > 0 ? zoomSpeed : zoomSpeed - zoomAcceleration)); //adds zoomAcceleration to zoomSpeed provided, while also resetting the zoomSpeed if it is currently zoomed in the opposite direction
            Camera.main.orthographicSize += zoomSpeed; //adds to orthographic size, essentially zooming in the image
        }
        else if ((Input.mouseScrollDelta.y < 0 || Input.GetKeyUp(KeyCode.PageDown)) && Camera.main.orthographicSize < maxZoomOrtho) //same function as previous segment but in reverse. zoom cap 430
        {
            zoomSpeed = System.Math.Min(zoomSpeedCap, zoomSpeed < 0 ? zoomAcceleration : (zoomSpeed < 0 ? zoomSpeed : zoomSpeed + zoomAcceleration));
            Camera.main.orthographicSize += zoomSpeed;
        }
        else //this is the zoomSpeed decay - it makes the zoom value lower when not in use
        {
            zoomSpeed = zoomSpeed > 0 ? (zoomSpeed - (zoomSpeed * Time.deltaTime)) : (zoomSpeed - (zoomSpeed * Time.deltaTime)); //zoomSpeed trends towards 0 over time
        }
    }

}

