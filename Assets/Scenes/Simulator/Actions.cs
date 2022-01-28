using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Act
{
    public static class Actions //Contains all the actions that can be simulated
    {
        public static void ToggleDebug(GameObject debugRef)
        {
            debugRef.SetActive(!debugRef.active);
        }
    }
}
