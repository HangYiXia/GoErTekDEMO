using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public class FocusTarget : MonoBehaviour
{
    private bool gaze, focus, longFocus;
    private float focusTime;
    private float targetScale;

    public bool focusEffect;


    private void Awake()
    {
        targetScale = transform.localScale.x;
    }

    public void FoucsOn()
    {
        gaze = true;
        //Debug.Log("FoucsOn " + name);
    }

    private void Update()
    {
        if (focus != gaze)
        {
            focus = gaze;
            if (focus) FocusIn(); else FocusOut();
            focusTime = 0;
            longFocus = false;
        }
        gaze = false;

        if (focus && !longFocus)
        {
            focusTime += Time.deltaTime;
            if (focusTime >= VACController.focusTime)
            {
                //Debug.Log("focus event " + transform.position);
                focusEvent?.Invoke(transform.position);
                longFocus = true;
            }
        }
    }

    public UnityEvent<Vector3> focusEvent;

    public void FocusIn()
    {
        //Debug.Log("FocusIn " + name);
        if (focusEffect)
            transform.DOScale(targetScale * 1.2f, 0.2f);
    }

    public void FocusOut()
    {
        //Debug.Log("FocusOut " + name);
        if (focusEffect)
            transform.DOScale(targetScale, 0.2f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.name == "gazePoint") FocusIn();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.name == "gazePoint") FocusOut();
    }
}
