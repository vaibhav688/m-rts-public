using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Networking;
using Fusion;


//troop settings
[System.Serializable]
public class troop
{
	public GameObject deployableTroops;
	public int troopCosts;
	public Sprite buttonImage;
	[HideInInspector]
	public Button button;
}

public class CharacterManagerMultiplayer : NetworkBehaviour
{

	// //variables visible in the inspector	
	public GUIStyle rectangleStyle;

	public NetworkRunner networkRunner;
	public ParticleSystem newUnitEffect;
	public Texture2D cursorTexture;
	public Texture2D cursorTexture1;
	public bool highlightSelectedButton;
	public Color buttonHighlight;
	public Button button;
	public float bombLoadingSpeed;
	public float BombRange;
	public GameObject bombExplosion;
	public GameObject killLabel;
	public bool ownHalfOnly;
	public float maxGold;
	public float addGoldTime;
	public int addGold;

	[Space(10)]

	// //not visible in the inspector
	public List<troop> troops;

	[HideInInspector]
	public float gold;
	public static GameObject target;

	private Vector2 mouseDownPos;
	private Vector2 mouseLastPos;
	private bool visible;
	private bool isDown;

	private GameObject[] allies;
	private int selectedUnit;
	private GameObject goldWarning;
	private GameObject characterList;
	private GameObject characterParent;
	private GameObject selectButton;
	private Image goldBar;
	private Text goldText;

	private GameObject bombLoadingBar;
	private GameObject bombButton;
	private float bombProgress;
	private bool isPlacingBomb;
	private GameObject bombRange;

	private GameObject deployWarning1;
	private GameObject deployWarning2;

	public static bool selectionMode;

	[HideInInspector]
	public bool host;

	private GameObject killList;

	[HideInInspector]
	public int player1kills;
	[HideInInspector]
	public int player2kills;

	void Start()
	{
		if (!Object.HasInputAuthority)
			return;

		selectButton = GameObject.Find("Character selection button");
		target = GameObject.Find("target");
		target.SetActive(false);

		Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.Auto);
		characterList = GameObject.Find("Character buttons");
		addCharacterButtons();
		goldWarning = GameObject.Find("gold warning");
		goldWarning.SetActive(false);
		InvokeRepeating("AddGold", 1.0f, addGoldTime);

		bombLoadingBar = GameObject.Find("Loading bar");
		bombButton = GameObject.Find("Bomb button");
		bombRange = GameObject.Find("Bomb range");
		bombRange.SetActive(false);
		isPlacingBomb = false;

		goldBar = GameObject.Find("Gold bar").GetComponent<Image>();
		goldText = GameObject.Find("Gold text").GetComponent<Text>();

		deployWarning1 = GameObject.Find("Unit deploy warning 1");
		deployWarning2 = GameObject.Find("Unit deploy warning 2");

		deployWarning1.SetActive(false);
		deployWarning2.SetActive(false);

		bombButton.GetComponent<Button>().onClick.AddListener(() => { placeBomb(); });
		selectButton.GetComponent<Button>().onClick.AddListener(() => { selectCharacters(); });
	}

	void Update()
	{
		if (!Object.HasInputAuthority || FindObjectsOfType<GameManagerMultiplayer>().Length != 2)
			return;

		if (bombProgress < 1)
		{
			bombProgress += Time.deltaTime * bombLoadingSpeed;
			bombLoadingBar.GetComponent<Image>().color = Color.red;
			bombButton.GetComponent<Button>().enabled = false;
		}
		else
		{
			bombProgress = 1;
			bombLoadingBar.GetComponent<Image>().color = new Color(0, 1, 1, 1);
			bombButton.GetComponent<Button>().enabled = true;
		}

		bombLoadingBar.GetComponent<Image>().fillAmount = bombProgress;

		RaycastHit hit;
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		if (Physics.Raycast(ray, out hit))

			if (Input.GetMouseButtonDown(0))
			{

				if (gold < troops[selectedUnit].troopCosts && !EventSystem.current.IsPointerOverGameObject())
				{
					StartCoroutine(GoldWarning());
				}
				if (hit.collider != null)
				{
					if (hit.collider.gameObject.CompareTag("Battle ground") && !selectionMode && !isPlacingBomb && !EventSystem.current.IsPointerOverGameObject()
					&& gold >= troops[selectedUnit].troopCosts && (!GameObject.Find("Mobile multiplayer") || (GameObject.Find("Mobile multiplayer") && MobileMultiplayer.deployMode))
					&& (!ownHalfOnly || (ownHalfOnly && ((transform.position.x < 0 && hit.point.x < -2) || (transform.position.x > 0 && hit.point.x > 2)))))
					{
						SpawnUnit(hit.point, null, false, false);
						gold -= troops[selectedUnit].troopCosts;
					}
					else if (hit.collider.gameObject.CompareTag("Battle ground") && !selectionMode && !isPlacingBomb && !EventSystem.current.IsPointerOverGameObject()
					&& gold >= troops[selectedUnit].troopCosts && (!GameObject.Find("Mobile multiplayer") || (GameObject.Find("Mobile multiplayer") && MobileMultiplayer.deployMode))
					&& ownHalfOnly && transform.position.x < 0 && hit.point.x > -2)
					{
						StartCoroutine(deployWrongSideWarning(2));
					}
					else if (hit.collider.gameObject.CompareTag("Battle ground") && !selectionMode && !isPlacingBomb && !EventSystem.current.IsPointerOverGameObject()
					&& gold >= troops[selectedUnit].troopCosts && (!GameObject.Find("Mobile multiplayer") || (GameObject.Find("Mobile multiplayer") && MobileMultiplayer.deployMode))
					&& ownHalfOnly && transform.position.x > 0 && hit.point.x < 2)
					{
						StartCoroutine(deployWrongSideWarning(1));
					}

					if (isPlacingBomb && !EventSystem.current.IsPointerOverGameObject())
					{

						if (host)
						{
							GameObject explosion = Instantiate(bombExplosion, hit.point, Quaternion.identity);
							Runner.Spawn(explosion);

							foreach (GameObject enemy in GameObject.FindGameObjectsWithTag("Player 2 unit"))
							{
								if (enemy != null && Vector3.Distance(enemy.transform.position, hit.point) <= BombRange / 2)
								{
									enemy.GetComponent<CharacterMultiplayer>().lives = 0;
								}
							}
						}
						else
						{
							CmdSpawnExplosion(hit.point);
						}

						bombProgress = 0;
						isPlacingBomb = false;
						bombRange.SetActive(false);
					}
					else if (hit.collider.gameObject.CompareTag("Battle ground") && isPlacingBomb && EventSystem.current.IsPointerOverGameObject())
					{
						isPlacingBomb = false;
						bombRange.SetActive(false);
					}
				}
			}

		if (hit.collider != null && isPlacingBomb && !EventSystem.current.IsPointerOverGameObject())
		{
			bombRange.transform.position = new Vector3(hit.point.x, 75, hit.point.z);
			bombRange.GetComponent<Light>().spotAngle = BombRange;
			bombRange.SetActive(true);
		}

		if (Input.GetMouseButtonDown(0) && selectionMode && !isPlacingBomb
		&& (!GameObject.Find("Mobile multiplayer") || (GameObject.Find("Mobile multiplayer") && !MobileMultiplayer.selectionModeMove)))
		{
			mouseDownPos = Input.mousePosition;
			isDown = true;
			visible = true;
		}

		if (isDown)
		{
			mouseLastPos = Input.mousePosition;
			if (Input.GetMouseButtonUp(0))
			{
				isDown = false;
				visible = false;
			}
		}

		if (host)
		{
			allies = GameObject.FindGameObjectsWithTag("Player 1 unit");
		}
		else
		{
			allies = GameObject.FindGameObjectsWithTag("Player 2 unit");
		}

		if (Input.GetKey("x"))
		{
			foreach (GameObject Ally in allies)
			{
				if (Ally != null)
				{
					Ally.GetComponent<CharacterMultiplayer>().selected = false;
				}
			}
		}

		if (Input.GetKeyDown("space"))
		{
			selectCharacters();
		}

		goldBar.fillAmount = (gold / maxGold);
		goldText.text = "" + gold;

		for (int i = 0; i < troops.Count; i++)
		{
			if (troops[i].troopCosts <= gold)
			{
				troops[i].button.gameObject.GetComponent<Image>().color = Color.white;
			}
			else
			{
				troops[i].button.gameObject.GetComponent<Image>().color = new Color(0.7f, 0.7f, 0.7f, 1);
			}
		}
	}

	void OnGUI()
	{
		if (visible)
		{
			Vector2 origin;
			origin.x = Mathf.Min(mouseDownPos.x, mouseLastPos.x);

			origin.y = Mathf.Max(mouseDownPos.y, mouseLastPos.y);
			origin.y = Screen.height - origin.y;

			Vector2 size = mouseDownPos - mouseLastPos;
			size.x = Mathf.Abs(size.x);
			size.y = Mathf.Abs(size.y);

			Rect rect = new Rect(origin.x, origin.y, size.x, size.y);
			GUI.Box(rect, "", rectangleStyle);

			foreach (GameObject Ally in allies)
			{
				if (Ally != null)
				{
					Vector3 pos = Camera.main.WorldToScreenPoint(Ally.transform.position);
					pos.y = Screen.height - pos.y;
					if (rect.Contains(pos))
					{
						Ally.GetComponent<CharacterMultiplayer>().selected = true;
					}
				}
			}
		}
	}

	public void SpawnUnit(Vector3 position, GameObject unit, bool skeleton, bool blue)
	{
		if (!host)
		{
			CmdSpawnUnit(position, selectedUnit, unit);
		}
		else
		{
			GameObject newTroop = null;

			if (unit == null)
			{
				newTroop = Instantiate(troops[selectedUnit].deployableTroops, position, troops[selectedUnit].deployableTroops.transform.rotation) as GameObject;
			}
			else
			{
				newTroop = Instantiate(unit, position, unit.transform.rotation) as GameObject;
			}

			Runner.Spawn(newTroop);

			if (!skeleton || !blue)
			{
				RpcAddTags(newTroop, "Player 1 unit", "Player 2 unit", "Player 2 castle", Color.red);
			}
			else
			{
				RpcAddTags(newTroop, "Player 2 unit", "Player 1 unit", "Player 1 castle", Color.blue);
			}

			ParticleSystem effect = Instantiate(newUnitEffect, position, Quaternion.identity) as ParticleSystem;
			Runner.Spawn(effect.gameObject);
		}
	}
	public void selectUnit(int unit)
	{
		if (highlightSelectedButton)
		{
			for (int i = 0; i < troops.Count; i++)
			{
				troops[i].button.GetComponent<Outline>().enabled = false;
			}
			EventSystem.current.currentSelectedGameObject.GetComponent<Outline>().enabled = true;
		}
		selectedUnit = unit;
	}

	public void selectCharacters()
	{
		selectionMode = !selectionMode;
		if (selectionMode)
		{
			selectButton.GetComponent<Image>().color = Color.red;
			Cursor.SetCursor(cursorTexture1, Vector2.zero, CursorMode.Auto);
			if (GameObject.Find("Mobile multiplayer"))
			{
				if (MobileMultiplayer.deployMode)
				{
					GameObject.Find("Mobile multiplayer").GetComponent<MobileMultiplayer>().toggleDeployMode();
				}
				MobileMultiplayer.camEnabled = false;
			}
		}
		else
		{
			selectButton.GetComponent<Image>().color = Color.white;
			Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.Auto);

			foreach (GameObject Ally in allies)
			{
				if (Ally != null)
				{
					Ally.GetComponent<CharacterMultiplayer>().selected = false;
				}
			}
			target.SetActive(false);
			if (GameObject.Find("Mobile multiplayer"))
			{
				MobileMultiplayer.camEnabled = true;
			}
		}
	}

	IEnumerator GoldWarning()
	{
		if (!goldWarning.activeSelf)
		{
			goldWarning.SetActive(true);

			yield return new WaitForSeconds(2);
			goldWarning.SetActive(false);
		}
	}

	void addCharacterButtons()
	{
		for (int i = 0; i < troops.Count; i++)
		{
			Button newButton = Instantiate(button);
			RectTransform rectTransform = newButton.GetComponent<RectTransform>();
			rectTransform.SetParent(characterList.transform, false);

			newButton.GetComponent<Outline>().effectColor = buttonHighlight;

			newButton.gameObject.GetComponent<Image>().sprite = troops[i].buttonImage;
			if (i == 0 && highlightSelectedButton)
			{
				newButton.GetComponent<Outline>().enabled = true;
			}
			else
			{
				newButton.GetComponent<Outline>().enabled = false;
			}

			newButton.transform.name = "" + i;

			newButton.GetComponent<Button>().onClick.AddListener(
			() =>
			{
				selectUnit(int.Parse(newButton.transform.name));
			}
			);

			newButton.GetComponentInChildren<Text>().text = "Price: " + troops[i].troopCosts +
			"\n Damage: " + troops[i].deployableTroops.GetComponentInChildren<CharacterMultiplayer>().damage +
			"\n Lives: " + troops[i].deployableTroops.GetComponentInChildren<CharacterMultiplayer>().lives;

			troops[i].button = newButton;
		}
	}

	public void placeBomb()
	{
		if (Object.HasInputAuthority)
			isPlacingBomb = true;
	}

	void AddGold()
	{
		if (Object.HasInputAuthority && FindObjectsOfType<GameManagerMultiplayer>().Length == 2 && gold < maxGold)
		{
			if (gold + addGold > maxGold)
			{
				gold += maxGold - gold;
			}
			else
			{
				gold += addGold;
			}
		}
	}


	// //tell server to spawn the correct unit and add an effect
	void CmdSpawnUnit(Vector3 position, int unit, GameObject unitPrefab)
	{
		GameObject newTroop = null;

		if (unitPrefab == null)
		{
			newTroop = Instantiate(troops[unit].deployableTroops, position, troops[unit].deployableTroops.transform.rotation) as GameObject;
		}
		else
		{
			newTroop = Instantiate(unitPrefab, position, unitPrefab.transform.rotation) as GameObject;
		}

		Runner.Spawn(newTroop);
		RpcAddTags(newTroop, "Player 2 unit", "Player 1 unit", "Player 1 castle", Color.blue);
		ParticleSystem effect = Instantiate(newUnitEffect, position, Quaternion.identity) as ParticleSystem;
		Runner.Spawn(effect.gameObject);
	}


	// //add tags on the client

	void RpcAddTags(GameObject newTroop, string tag, string attackTag, string attackCastleTag, Color color)
	{
		newTroop.tag = tag;
		newTroop.GetComponent<CharacterMultiplayer>().attackTag = attackTag;
		newTroop.GetComponent<CharacterMultiplayer>().attackCastleTag = attackCastleTag;
		newTroop.GetComponent<CharacterMultiplayer>().healthBarFill.GetComponent<Image>().color = color;
	}

	// //an enemy unit has been killed, show a label
	public void enemyUnitKilled(string tag, string unitType)
	{
		// 	//check if this is the host. If not, send a command to the server
		if (!host)
		{
			CmdEnemyUnitKilled(tag, unitType);
		}
		else
		{
			//if this is the host, directly send an rpc
			RpcEnemyUnitKilled(tag, unitType);
		}
	}

	// //server tells client to show the kill label

	void CmdEnemyUnitKilled(string tag, string unitType)
	{
		RpcEnemyUnitKilled(tag, unitType);
	}

	// //client checks if it should show a label

	void RpcEnemyUnitKilled(string tag, string unitType)
	{
		// 		//if this is player 2 and a unit of player 1 has been killed, show the kill label
		if (tag == "Player 1 unit")
		{
			if (!host)
			{
				enemyKilled(unitType);
			}

			// 			//player 2 has one extra kill
			player2kills++;
		}
		// 		//if this is player 1 and a unit of player 2 has been killed, show the kill label
		else if (tag == "Player 2 unit")
		{
			if (host)
			{
				enemyKilled(unitType);
			}

			// 			//player 1 has one extra kill
			player1kills++;
		}
	}

	// //actually show the kill label locally (it gets destroyed automatically)
	void enemyKilled(string unitType)
	{
		GameObject newLabel = Instantiate(killLabel);
		RectTransform rectTransform = newLabel.GetComponent<RectTransform>();
		rectTransform.SetParent(killList.transform, false);
		newLabel.GetComponentInChildren<Text>().text = "ENEMY " + unitType.ToUpper() + " KILLED";
	}

	// //shows a warning when you're trying to deploy a unit on the wrong half of the battle ground
	IEnumerator deployWrongSideWarning(int half)
	{
		if (half == 1)
		{
			// 		//show the warning, wait, hide the warning
			deployWarning1.SetActive(true);
			yield return new WaitForSeconds(1);
			deployWarning1.SetActive(false);
		}
		else if (half == 2)
		{
			// 		//show the warning, wait, hide the warning
			deployWarning2.SetActive(true);
			yield return new WaitForSeconds(1);
			deployWarning2.SetActive(false);
		}
	}

	// //tell the server to spawn an explosion

	void CmdSpawnExplosion(Vector3 position)
	{
		// 	//add the explosion and spawn it on the server
		GameObject explosion = Instantiate(bombExplosion, position, Quaternion.identity);
		Runner.Spawn(explosion);

		// 	//this is a command, so it came from the client, which means the bomb should find and kill all units of player 1 in range (player1 = host)
		foreach (GameObject enemy in GameObject.FindGameObjectsWithTag("Player 1 unit"))
		{
			if (enemy != null && Vector3.Distance(enemy.transform.position, position) <= BombRange / 2)
			{
				// 		//kill enemy if its within the bombrange
				enemy.GetComponent<CharacterMultiplayer>().lives = 0;
			}
		}
	}
}
