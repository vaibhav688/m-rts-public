using UnityEngine;
using UnityEngine.UI;
using Fusion;
using Fusion.Sockets;
using UnityEngine.SceneManagement;

public class GameManagerMultiplayer: NetworkBehaviour {

    public float fadespeed;
    public GameObject cam;

    private GameObject leftCastleStrengthText;
    private GameObject rightCastleStrengthText;
    private GameObject leftCastleStrengthBar;
    private GameObject rightCastleStrengthBar;

    private float player1CastleStrengthStart;
    private float player2CastleStrengthStart;

	public NetworkRunner networkRunner;

    private float player1CastleStrength;
    private float player2CastleStrength;

    public static GameObject connectionPanel;

    private bool fading;
    private float alpha;
    public static bool battleEnded;

    private GameObject battleResultPanel;
    private GameObject battleResultTitle;
    private Text battleResultDuration;
    private Text battleResultKilledEnemies;
    private Text battleResultKilledAllies;

    private float battleDuration;
    private bool gameStarted;

    void Start() {
        if (transform.position.x < 0) {
            transform.localEulerAngles = new Vector3(0, 180, 0);
        } else {
            transform.localEulerAngles = new Vector3(0, 0, 0);
        }

        GameObject[] flags = GameObject.FindGameObjectsWithTag("Flag");

        if (!Object.HasInputAuthority) {
            if (GameObject.FindWithTag("Player 1 castle")) {
                foreach (Transform castlePart in transform) {
                    if (!castlePart.gameObject.GetComponent<Camera>()) {
                        castlePart.gameObject.tag = "Player 2 castle";
                    }
                }
                foreach (GameObject flag in flags) {
                    if ((flag.transform.position.x < 0 && transform.position.x < 0) || (flag.transform.position.x > 0 && transform.position.x > 0)) {
                        flag.GetComponent<SkinnedMeshRenderer>().material.color = Color.blue;
                    }
                }
            } else {
                foreach (Transform castlePart in transform) {
                    if (!castlePart.gameObject.GetComponent<Camera>()) {
                        castlePart.gameObject.tag = "Player 1 castle";
                    }
                }
                foreach (GameObject flag in flags) {
                    if ((flag.transform.position.x < 0 && transform.position.x < 0) || (flag.transform.position.x > 0 && transform.position.x > 0)) {
                        flag.GetComponent<SkinnedMeshRenderer>().material.color = Color.red;
                    }
                }
            }
        }

        if (!Object.HasInputAuthority)
            return;

        leftCastleStrengthText = GameObject.Find("Left castle strength text");
        rightCastleStrengthText = GameObject.Find("Right castle strength text");
        leftCastleStrengthBar = GameObject.Find("Left castle strength bar");
        rightCastleStrengthBar = GameObject.Find("Right castle strength bar");

        connectionPanel = GameObject.Find("Connection");

        battleResultPanel = GameObject.Find("Battle result");
        battleResultTitle = GameObject.Find("result title");
        battleResultDuration = GameObject.Find("duration").GetComponent<Text>();
        battleResultKilledEnemies = GameObject.Find("killed enemies").GetComponent<Text>();
        battleResultKilledAllies = GameObject.Find("killed allies").GetComponent<Text>();

        battleResultPanel.SetActive(false);

        alpha = connectionPanel.GetComponent<CanvasGroup>().alpha;
        battleEnded = false;

        GetCastleStrength();

        player1CastleStrengthStart = player1CastleStrength;
        player2CastleStrengthStart = player1CastleStrength;    
    }

    void Update() {
        if (!Object.HasInputAuthority)
            return;

        if (FindObjectsOfType<GameManagerMultiplayer>().Length != 2) {
            if (gameStarted) {
                endBattle(player1CastleStrength, player2CastleStrength, battleDuration, GetComponent<CharacterManagerMultiplayer>().player1kills, GetComponent<CharacterManagerMultiplayer>().player2kills);
            } else {
                Time.timeScale = 0;
            }
        } else {
            if (!gameStarted)
                gameStarted = true;

            if (!battleEnded)
                battleDuration += Time.deltaTime;

            if (connectionPanel.activeSelf && !fading) {
                Time.timeScale = 1;
                fading = true;
            }

            if (fading && alpha > 0) {
                alpha -= Time.deltaTime * fadespeed;
                connectionPanel.GetComponent<CanvasGroup>().alpha = alpha;
            } else if (alpha <= 0) {
                fading = false;
                connectionPanel.SetActive(false);        
            }

            GetCastleStrength();

            if (GetComponent<CharacterManagerMultiplayer>().host) {
                leftCastleStrengthText.GetComponent<Text>().text = "" + (int)player1CastleStrength;
                rightCastleStrengthText.GetComponent<Text>().text = "" + (int)player2CastleStrength;

                rightCastleStrengthBar.GetComponent<Image>().fillAmount = player2CastleStrength/player2CastleStrengthStart;
                leftCastleStrengthBar.GetComponent<Image>().fillAmount = player1CastleStrength/player1CastleStrengthStart;

                rightCastleStrengthBar.GetComponent<Image>().color = Color.blue;
                leftCastleStrengthBar.GetComponent<Image>().color = Color.red;

                if (player1CastleStrength <= 0 || player2CastleStrength <= 0) {
                    RpcEndBattle(player1CastleStrength, player2CastleStrength, battleDuration, 
                    GetComponent<CharacterManagerMultiplayer>().player1kills, GetComponent<CharacterManagerMultiplayer>().player2kills);
                }
            } else {
                leftCastleStrengthText.GetComponent<Text>().text = "" + (int)player2CastleStrength;
                rightCastleStrengthText.GetComponent<Text>().text = "" + (int)player1CastleStrength;

                rightCastleStrengthBar.GetComponent<Image>().fillAmount = player1CastleStrength/player1CastleStrengthStart;
                leftCastleStrengthBar.GetComponent<Image>().fillAmount = player2CastleStrength/player2CastleStrengthStart;

                rightCastleStrengthBar.GetComponent<Image>().color = Color.red;
                leftCastleStrengthBar.GetComponent<Image>().color = Color.blue;
            }
        }

        if (battleEnded && battleResultPanel.GetComponent<CanvasGroup>().alpha < 1) {
            battleResultPanel.GetComponent<CanvasGroup>().alpha += Time.deltaTime * fadespeed;
        } else if (battleEnded && Time.timeScale != 0) {
            Time.timeScale = 0;
            // Stop the host if this instance is the host
            if (Runner.IsServer) {
                Runner.Shutdown();
            }
        }
    }

    void GetCastleStrength() {
        player1CastleStrength = 0;
        player2CastleStrength = 0;

        foreach (GameObject player2Castle in GameObject.FindGameObjectsWithTag("Player 2 castle")) {
            if (player2Castle.GetComponent<Castle>().lives > 0) {
                player2CastleStrength += player2Castle.GetComponent<Castle>().lives;
            }
        }
        foreach (GameObject player1Castle in GameObject.FindGameObjectsWithTag("Player 1 castle")) {
            if (player1Castle.GetComponent<Castle>().lives > 0) {
                player1CastleStrength += player1Castle.GetComponent<Castle>().lives;
            }
        }    
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RpcEndBattle(float player1Castle, float player2Castle, float duration, int player1kills, int player2kills) {
        Camera.main.gameObject.transform.root.gameObject.GetComponent<GameManagerMultiplayer>().endBattle(player1Castle, player2Castle, duration, player1kills, player2kills);
    }

    public void endBattle(float player1Castle, float player2Castle, float duration, int player1kills, int player2kills) {
        bool host = GetComponent<CharacterManagerMultiplayer>().host;

        if (!battleEnded && Object.HasInputAuthority) {
            battleResultPanel.GetComponent<CanvasGroup>().alpha = 0;

            if ((player1Castle <= 0 && host) || (player2Castle <= 0 && !host)) {
                battleResultPanel.GetComponent<Image>().color = new Color(0.9f, 0.2f, 0.2f, 0.95f);
                battleResultTitle.GetComponent<Text>().text = "DEFEAT";
            } else if ((player1Castle <= 0 && !host) || (player2Castle <= 0 && host)) {
                battleResultPanel.GetComponent<Image>().color = new Color(0, 0.7f, 0.8f, 0.95f);
                battleResultTitle.GetComponent<Text>().text = "VICTORY";
            }

            if (host) {
                battleResultKilledEnemies.text = "" + player1kills;
                battleResultKilledAllies.text = "" + player2kills;
            } else {
                battleResultKilledEnemies.text = "" + player2kills;
                battleResultKilledAllies.text = "" + player1kills;
            }

            battleResultDuration.text = "" + duration.ToString("f1") + "s";

            battleResultPanel.SetActive(true);
            battleEnded = true;
        }
    }

	
	// //go back to the start menu (show the network HUD & reload scene)
	public void backToMenu(){
	 	if (networkRunner != null) {
            // Perform any necessary clean-up here
            networkRunner.Shutdown(); // Example shutdown method
        }
		SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
	 }
}
