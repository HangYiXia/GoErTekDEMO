using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdjustUISize : MonoBehaviour
{
    static private float MARKER_SIZE_IN_PIXELS = 225.0f;

    void Start()
    {
        RectTransform rt = GetComponent<RectTransform>();
        float markerIconSize = MARKER_SIZE_IN_PIXELS * (6 + 2) / 6;
        rt.sizeDelta = new Vector2(markerIconSize, markerIconSize);

        if ((rt.anchorMin - (new Vector2(0.5f, 0.5f))).magnitude <= 1e-5f &&
            (rt.anchorMax - (new Vector2(0.5f, 0.5f))).magnitude <= 1e-5f)
        {
            rt.anchoredPosition = new Vector2(0.0f, 0.0f);
        } else
        {
            float signX = Mathf.Sign(rt.anchoredPosition.x);
            float signY = Mathf.Sign(rt.anchoredPosition.y);
            rt.anchoredPosition = new Vector2(signX * markerIconSize * 0.5f, signY * markerIconSize * 0.5f);
        }
    }

    void Update()
    {

    }
}
