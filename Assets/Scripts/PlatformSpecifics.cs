using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformSpecifics : MonoBehaviour
{

    public GameObject quitButton;
    
    void Awake() {
        #if UNITY_WEBGL
            quitButton.SetActive(false);
        #endif
    }

}
