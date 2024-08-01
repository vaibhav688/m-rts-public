using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class cardstats 
{
    [SerializeField]
    private int index;
    [SerializeField]
    private string name;
    [SerializeField]
    private Sprite icon ;
    [SerializeField]
    private GameObject prefab ;
    public int Index
    {
        get {return index; }
        set {index=value;}

    }
    public string Name{
        get {return name;}
        set {name=value; }
    }
    public Sprite Icon{
        get {return icon;}
        set {icon=value;}
    }
    public GameObject Prefab{
        get {return prefab;}
        set {prefab=value;}
    }
      
}
