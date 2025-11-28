using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PlayController))]
public class PlayControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PlayController playController = (PlayController)target;


        if (GUILayout.Button("Play"))
        {
            if (EditorApplication.isPlaying)
            {
                playController.StartPlay();
            }
            else
            {
                Debug.LogError("Play button should be used during play mode!");
            }
        }
    }
}
