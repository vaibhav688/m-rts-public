
using UnityEngine;
using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Fusion;

public class Manager : NetworkBehaviour
{
    [Header("Mission")]
    public bool destroyCastle;
    public int killEnemies;
    public int timeSeconds;

    [Space(10)]
    public float fadespeed;
    public GameObject Minimap;
    public Slider minimapZoomSlider;

    private GameObject GameOverMenu;
    private GameObject VictoryMenu;
    private GameObject GamePanel;
    private GameObject characterButtons;

    public bool fading;
    private bool showMinimap;

    private GameObject showHideUnitsButton;
    private GameObject showHideMinimapButton;
    private GameObject unitsLabel;

    private GameObject playerCastleStrengthText;
    private GameObject enemyCastleStrengthText;
    private GameObject playerCastleStrengthBar;
    private GameObject enemyCastleStrengthBar;
    private Camera miniMapCamera;

    private GameObject missionPanel;

    private float playerCastleStrengthStart;
    private float enemyCastleStrengthStart;

    [Networked] public float playerCastleStrength { get; set; }
    [Networked] public float enemyCastleStrength { get; set; }
    [Networked] public  int enemiesKilled { get; set; }
    [Networked] public float time { get; set; }
    [Networked] public  NetworkBool gameOver { get; set; }
    [Networked] public  NetworkBool victory { get; set; }
    public static GameObject StartMenu;

    public static event Action RetreatTroops;
    public static event Action FightTroops;
    public AudioSource retreatAudio;
    public AudioSource fightAudio;

    public override void Spawned()
    {
        characterButtons = GameObject.Find("Character panel");
        showHideUnitsButton = GameObject.Find("Show/hide units");
        showHideMinimapButton = GameObject.Find("Show/hide minimap");
        unitsLabel = GameObject.Find("Amount of units label");

        playerCastleStrengthText = GameObject.Find("Player castle strength text");
        enemyCastleStrengthText = GameObject.Find("Enemy castle strength text");
        playerCastleStrengthBar = GameObject.Find("Player castle strength bar");
        enemyCastleStrengthBar = GameObject.Find("Enemy castle strength bar");

        miniMapCamera = GameObject.Find("Minimap camera").GetComponent<Camera>();

        StartMenu = GameObject.Find("Start panel");
        GameOverMenu = GameObject.Find("Game over panel");
        VictoryMenu = GameObject.Find("Victory panel");
        GamePanel = GameObject.Find("Game panel");

        missionPanel = GameObject.Find("Mission");

        minimapZoomSlider.value = miniMapCamera.orthographicSize;

        Time.timeScale = 0;

        StartMenu.SetActive(true);
        GameOverMenu.SetActive(false);
        VictoryMenu.SetActive(false);
        GamePanel.SetActive(false);

        gameOver = false;
        victory = false;

        GetCastleStrength();

        playerCastleStrengthStart = playerCastleStrength;
        enemyCastleStrengthStart = enemyCastleStrength;

        SetupMissionText();

        missionPanel.SetActive(false);
        time = 0;
    }

    private void SetupMissionText()
    {
        string mission = "";

        if (destroyCastle)
        {
            mission = "Destroy castle of the opponent";
        }
        else if (killEnemies > 0 && timeSeconds > 0)
        {
            mission = "- Kill " + killEnemies + " enemies. \n- Battle at least " + timeSeconds + " seconds.";
        }
        else if (killEnemies > 0 && timeSeconds <= 0)
        {
            mission = "Kill " + killEnemies + " enemies.";
        }
        else if (killEnemies <= 0 && timeSeconds > 0)
        {
            mission = "Battle at least " + timeSeconds + " seconds.";
        }
        else
        {
            mission = "No mission";
        }

        missionPanel.transform.Find("Mission text").gameObject.GetComponent<Text>().text = mission;
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority)
        {
            GetCastleStrength();
            time += Runner.DeltaTime;

            CheckWinLoseConditions();
        }
    }

    private void Update()
    {
        UpdateUIElements();
        HandleUIVisibility();
        HandleFading();
        HandleMinimapVisibility();
        UpdateUnitCounters();
    }

    private void UpdateUIElements()
    {
        if (playerCastleStrengthText != null)
            playerCastleStrengthText.GetComponent<Text>().text = "" + (int)playerCastleStrength;
        else
            Debug.LogError("playerCastleStrengthText is null");

        if (enemyCastleStrengthText != null)
            enemyCastleStrengthText.GetComponent<Text>().text = "" + (int)enemyCastleStrength;
        else
            Debug.LogError("enemyCastleStrengthText is null");

        if (enemyCastleStrengthBar != null)
            enemyCastleStrengthBar.GetComponent<Image>().fillAmount = enemyCastleStrength / enemyCastleStrengthStart;
        else
            Debug.LogError("enemyCastleStrengthBar is null");

        if (playerCastleStrengthBar != null)
            playerCastleStrengthBar.GetComponent<Image>().fillAmount = playerCastleStrength / playerCastleStrengthStart;
        else
            Debug.LogError("playerCastleStrengthBar is null");
    }

    private void HandleUIVisibility()
    {
        if (gameOver && !GameOverMenu.activeSelf)
        {
            GamePanel.SetActive(false);
            GameOverMenu.SetActive(true);
        }

        if (victory && !VictoryMenu.activeSelf)
        {
            GamePanel.SetActive(false);
            VictoryMenu.SetActive(true);
        }
    }

    private void HandleFading()
    {
        if (fading && StartMenu.GetComponent<CanvasGroup>().alpha > 0)
        {
            StartMenu.GetComponent<CanvasGroup>().alpha -= Time.unscaledDeltaTime * fadespeed;
        }
        else if (StartMenu.GetComponent<CanvasGroup>().alpha <= 0)
        {
            fading = false;
            StartMenu.SetActive(false);
            GamePanel.SetActive(true);
        }
    }

    private void HandleMinimapVisibility()
    {
        if (showMinimap && !StartMenu.activeSelf && !Settings.settingsMenu.activeSelf && !GameOverMenu.activeSelf && !VictoryMenu.activeSelf)
        {
            Minimap.SetActive(true);
        }
        else
        {
            Minimap.SetActive(false);
        }
    }

    private void UpdateUnitCounters()
    {
        int playerUnits = GameObject.FindGameObjectsWithTag("Knight").Length;
        int enemyUnits = GameObject.FindGameObjectsWithTag("Enemy").Length;
        int selectedUnits = 0;

        foreach (GameObject unit in GameObject.FindGameObjectsWithTag("Knight"))
        {
            if (unit.GetComponent<Character>().selected)
            {
                selectedUnits++;
            }
        }

        if (unitsLabel != null)
            unitsLabel.GetComponentInChildren<Text>().text = "Player Units: " + playerUnits + "  |  Enemy Units: " + enemyUnits + "  |  Selected Units: " + selectedUnits;
        else
            Debug.LogError("unitsLabel is null");
    }

    private void CheckWinLoseConditions()
    {
        if (playerCastleStrength <= 0)
        {
            gameOver = true;
        }

        if (enemyCastleStrength <= 0 && destroyCastle)
        {
            victory = true;
        }
        else if (killEnemies > 0 && enemiesKilled >= killEnemies && timeSeconds > 0 && time >= timeSeconds)
        {
            victory = true;
        }
        else if (killEnemies > 0 && enemiesKilled >= killEnemies && timeSeconds <= 0)
        {
            victory = true;
        }
        else if (killEnemies <= 0 && timeSeconds > 0 && time >= timeSeconds)
        {
            victory = true;
        }
    }

    private void GetCastleStrength()
    {
        foreach (var networkObject in FindObjectsOfType<NetworkObject>())
        {
            if (networkObject.TryGetComponent<Castle>(out var castle))
            {
                if (castle.Owner == Object.InputAuthority)
                {
                    playerCastleStrength = castle.lives;
                }
                else
                {
                    enemyCastleStrength = castle.lives;
                }
            }
        }
    }

	[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PauseGame()
    {
        Time.timeScale = 0;
    }
    public void startGame()
    {
        if (Object.HasStateAuthority)
        {
            RPC_StartGame();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_StartGame()
    {
        missionPanel.SetActive(false);
        Time.timeScale = 1;
        fading = true;

        GetComponent<CharacterManager>().addCharacterButtons();
    }

    public void openMissionPanel()
    {
        StartMenu.SetActive(false);
        missionPanel.SetActive(true);
    }

    public void endGame()
    {
        Application.Quit();
    }

    public void surrender()
    {
        if (Object.HasStateAuthority)
        {
            gameOver = true;
            RPC_PauseGame();
        }
    }

    public void showHideUnits()
    {
        characterButtons.SetActive(!characterButtons.activeSelf);
        showHideUnitsButton.GetComponentInChildren<Text>().text = characterButtons.activeSelf ? "-" : "+";
    }

    public void showHideMinimap()
    {
        showMinimap = !showMinimap;
        showHideMinimapButton.GetComponentInChildren<Text>().text = showMinimap ? "-" : "+";
    }

    public void setMinimapSize()
    {
        miniMapCamera.orthographicSize = minimapZoomSlider.value;
    }

    public void restart()
    {
        // You might want to use Fusion's way of restarting the game here
        // For now, we'll use SceneManager, but this might need to be adjusted
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void Retreat()
    {
        RetreatTroops?.Invoke();
        if (retreatAudio != null)
        {
            retreatAudio.Play();
        }
    }

    public void Fight()
    {
        FightTroops?.Invoke();
        if (fightAudio != null)
        {
            Debug.Log("Fight");
            fightAudio.Play();
        }
    }
}