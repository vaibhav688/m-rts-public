using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Playerstats : MonoBehaviour
{
    // Start is called before the first frame update

    public static float Resource_speed=1.5f;
    public static int Resource_Max=9;
    public static int Max_Hand_Size=4;

    public static string CANVAS="Canvas-HUD";
    [SerializeField]
    private Deck playersDeck;

    [SerializeField]
    private List<Image> resources;

    [SerializeField]
    private int score;

    [SerializeField]
    private float currResource;

    [SerializeField]
    private Text textCurrResource;

    [SerializeField]
    private Text textMaxResource;

    [SerializeField]
    private Text textScore;

    public Text TextScore
    {
        get {return textScore;}
    }

    public Text TextMaxResource
    {
        get {return textMaxResource;}
    }

    public Text TextCurrResource
    {
        get {return textCurrResource;}
        

    }

    public int Score
    {
        get {return score;}
        set {score=value;}
    
    }


    public Deck PlayersDeck
    {
        get {return playersDeck;}
        set {playersDeck=value;}

    }

    public List<Image> Resources
    {
        get {return resources;}
        

    }
    public float CurrResource
    {
        get 
        {
            return currResource;
        }
        set
        {
            currResource=value;

        }

    }
    public int GetCurrResource
    {
        get{
            return (int)currResource;
        }
    }

    private void Start()
    {
        playersDeck.Start();

    }
    private void update()
    {
        if(GetCurrResource < Resource_Max+1)
        {
            resources[GetCurrResource].fillAmount=currResource-GetCurrResource;
            currResource +=Time.deltaTime * Resource_speed;

        }
        UpdateText();
        UpdateDeck();
    }
    void UpdateText() {
        textCurrResource.text=GetCurrResource.ToString();
        textMaxResource.text=(Resource_Max+1).ToString();
        textScore.text=score.ToString();
    }
    void UpdateDeck() 
    {

    }



    
}
