using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TruckMovement : MonoBehaviour
{

    private const float wheelRotationSpeed = -500f;

    private float noiseFrequencyX = 2f;
    private float noiseFrequencyY = 4f;
    private float noiseAmplitudeX = 0.5f;
    private float noiseAmplitudeY = 0.5f;

    private float x = 0;
    private float y = 0;
    private Vector3 startPos;
    private Quaternion startRotation;

    public GameObject[] wheels;

    public bool mainMenu = false;

    void Start() {
        startPos = transform.localPosition;
        startRotation = transform.localRotation;

        if(mainMenu) {
            noiseFrequencyX = 1f;
            noiseAmplitudeX = 11f;
            noiseAmplitudeY = 0.2f;
        }
    }

    void Update()
    {
        if((!mainMenu) && (!GameScript.alive)) return;

        x += Time.deltaTime * noiseFrequencyX;
        y += Time.deltaTime * noiseFrequencyY;

        transform.localPosition = startPos + new Vector3(Mathf.PerlinNoise(x, 0) * noiseAmplitudeX, Mathf.PerlinNoise(0, y) * noiseAmplitudeY, 0);
        if(!GameScript.expertMode) transform.rotation = startRotation;


        float wheelDelta = Time.deltaTime * wheelRotationSpeed;
        foreach (GameObject wheel in wheels)
        {
            wheel.transform.Rotate(0, 0, wheelDelta);
        }
    }
}
