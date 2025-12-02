using UnityEngine;
using System.IO;
using System;

public class CupController : MonoBehaviour
{
    public Material mat;
    public int sequence = 0;
    private string path;

    public bool numberReset = false;

    private String onesTexPath;
    private String tensTexPath;
    private Texture2D onesTex;
    private Texture2D tensTex;


    void Start()
    {
        sequence++;
        Debug.Log("Sequencet: " + sequence.ToString());

        if (!mat)
        {
            Debug.LogError("Cannot find material of Cup!");
        }


        ///  Cup number by sequence  ///

#if UNITY_EDITOR
        path = Application.dataPath + "/Arts/sequence.txt";
#else
        path = Application.persistentDataPath + "/sequence.txt";
#endif
        
        if (!File.Exists(path))
        {
            File.WriteAllText(path, "0");
        }
        sequence = int.Parse(File.ReadAllText(path));
        sequence++;

        if (sequence >= 100 || numberReset)
        {
            sequence = 0;
        }

        File.WriteAllText(path, sequence.ToString());

        int ones = sequence % 10;
        int tens = sequence / 10;

        onesTexPath = $"CupNumText/award_ones_{ones}";
        tensTexPath = $"CupNumText/award_tens_{tens}";

        onesTex = Resources.Load<Texture2D>(onesTexPath);
        tensTex = Resources.Load<Texture2D>(tensTexPath);

        mat.SetTexture("_OnesTex", onesTex);
        mat.SetTexture("_TensTex", tensTex);
    }
}

