/*!
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: MIT-0
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    [SerializeField, Tooltip("Passive detection mesh")]
    private GameObject passiveDetectionMesh;

    [SerializeField, Tooltip("Active detection mesh")]
    private GameObject activeDetectionMesh;

    public float fadeSpeed = 10f;
    public float fadeHoldTime = 1f;

    private Material passiveMaterial;

    private Color passiveColor;

    private Material activeMaterial;

    private Color activeColor;

    private float alpha;
    private float holdTimestamp;

    enum Fade { In, Out, Hold, None };

    private Fade fadeState = Fade.None;


    private void Awake()
    {
        passiveMaterial = passiveDetectionMesh.GetComponent<MeshRenderer>().material;
        passiveColor = passiveMaterial.color;
        passiveColor.a = 1f;

        activeMaterial = activeDetectionMesh.GetComponent<MeshRenderer>().material;
        activeColor = activeMaterial.color;
        activeColor.a = 1f;
    }

    private void Update()
    {
        Fader();
    }

    private void Fader()
    {
        if (fadeState == Fade.None) { return; }

        if (fadeState == Fade.In)
        {
            alpha = Mathf.Lerp(alpha, 1f, fadeSpeed * Time.deltaTime);
            passiveMaterial.color = new Color(passiveColor.r, passiveColor.g, passiveColor.b, alpha);
            activeMaterial.color = new Color(activeColor.r, activeColor.g, activeColor.b, alpha);
            if (Mathf.Abs(1f - alpha) < 0.001f)
            {
                passiveMaterial.color = passiveColor;
                activeMaterial.color = activeColor;
                fadeState = Fade.Hold;
                holdTimestamp = Time.time;
                alpha = 1f;
            }
        }
        else if (fadeState == Fade.Hold)
        {
            if (Time.time - holdTimestamp > fadeHoldTime)
            {
                fadeState = Fade.Out;
            }
        }
        else if (fadeState == Fade.Out)
        {
            alpha = Mathf.Lerp(alpha, 0f, fadeSpeed * Time.deltaTime);
            passiveMaterial.color = new Color(passiveColor.r, passiveColor.g, passiveColor.b, alpha);
            activeMaterial.color = new Color(activeColor.r, activeColor.g, activeColor.b, alpha);
            if (Mathf.Abs(alpha - 0f) < 0.001f)
            {
                passiveMaterial.color = passiveColor;
                activeMaterial.color = activeColor;
                Clear();
            }
        }
    }



    public void SetPassiveDetection()
    {
        alpha = 0f;
        fadeState = Fade.In;
        passiveDetectionMesh.SetActive(true);
        activeDetectionMesh.SetActive(false);
    }

    public void SetActiveDetection()
    {
        alpha = 0f;
        fadeState = Fade.In;
        passiveDetectionMesh.SetActive(false);
        activeDetectionMesh.SetActive(true);
    }

    public void Clear()
    {
        alpha = 0f;
        fadeState = Fade.None;
        passiveDetectionMesh.SetActive(false);
        activeDetectionMesh.SetActive(false);
    }
}
