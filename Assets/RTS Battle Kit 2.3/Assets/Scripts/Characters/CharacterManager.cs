using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.EventSystems;
using UnityEngine.UI;

//troop/unit settings
[System.Serializable]
public class Troop
{
	public GameObject deployableTroops;
	public int troopCosts;
	public int foodCosts;
	public Sprite buttonImage;
	[HideInInspector]
	public GameObject button;
}

public class CharacterManager : MonoBehaviour
{

	//variables visible in the inspector	
	public int startGold;
	public int startFood;
	public GUIStyle rectangleStyle;
	public ParticleSystem newUnitEffect;
	public Texture2D cursorTexture;
	public Texture2D cursorTexture1;
	public bool highlightSelectedButton;
	public Color buttonHighlight;
	public GameObject button;
	public float bombLoadingSpeed;
	public float BombRange;
	public GameObject bombExplosion;
	public KeyCode deselectKey;

	[Space(5)]
	public bool mobileDragInput;
	public bool originalPreviewSize;
	public int dragPreviewSize;

	[Space(10)]
	public List<Troop> troops;

	//variables not visible in the inspector
	public static Vector3 clickedPos;
	public static int gold;
	public static int food;
	public static GameObject target;

	private Vector2 mouseDownPos;
	private Vector2 mouseLastPos;
	private bool visible;
	private bool isDown;
	private GameObject[] knights;
	private int selectedUnit;

	//Gold
	private GameObject goldText;
	private GameObject goldWarning;
	private GameObject addedGoldText;

	//Food
	private GameObject foodText;
	private GameObject foodWarning;
	private GameObject addedfoodText;

	//Tree
	private int maxTrees = 3;
	public static int treesCount = 0;
	private GameObject treeWarning;

	private GameObject characterList;
	private GameObject characterParent;
	private GameObject selectButton;

	private GameObject bombLoadingBar;
	private GameObject bombButton;
	private float bombProgress;
	private bool isPlacingBomb;
	private GameObject bombRange;
	private bool canDrag;
	private GameObject dragPreview;

	public static bool selectionMode;

	void Awake()
	{
		characterParent = new GameObject("Characters");
		selectButton = GameObject.Find("Character selection button");
		target = GameObject.Find("target");

		if (!GameObject.Find("Mobile"))
			mobileDragInput = false;

		if (mobileDragInput)
		{
			highlightSelectedButton = false;
			GameObject.FindGameObjectWithTag("Mobile units button").SetActive(false);
		}

		characterList = GameObject.Find("Character buttons");

		goldText = GameObject.Find("gold text");
		foodText = GameObject.Find("food text");
		addedGoldText = GameObject.Find("added gold text");
		addedfoodText = GameObject.Find("added food text");

		goldWarning = GameObject.Find("gold warning");
		foodWarning = GameObject.Find("food warning");
		treeWarning = GameObject.Find("tree warning");

		bombLoadingBar = GameObject.Find("Loading bar");
		bombButton = GameObject.Find("Bomb button");
		bombRange = GameObject.Find("Bomb range");
	}


	private void OnEnable()
	{
		FoodProvider.AddFoodCount += AddFood;
	}

	void Start()
	{
		if (target == null)
		{
			Debug.LogError("Target not found");
			return;
		}
		target.SetActive(false);
		Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.Auto);

		gold = startGold;
		if (addedGoldText != null) addedGoldText.SetActive(false);
		if (goldWarning != null) goldWarning.SetActive(false);

		food = startFood;
		if (addedfoodText != null) addedfoodText.SetActive(false);
		if (foodWarning != null) foodWarning.SetActive(false);

		if (treeWarning != null) treeWarning.SetActive(false);

		InvokeRepeating("AddGold", 1.0f, 5.0f);

		if (bombRange != null) bombRange.SetActive(false);
		isPlacingBomb = false;
	}


	void Update()
	{
		if (bombLoadingBar == null || bombButton == null) return;

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

		if (goldText != null)
			goldText.GetComponent<UnityEngine.UI.Text>().text = "" + gold;
		if (foodText != null)
			foodText.GetComponent<UnityEngine.UI.Text>().text = "" + food;

		RaycastHit hit;
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		Physics.Raycast(ray, out hit);

		if (Input.GetMouseButtonDown(0))
		{
			if (gold < troops[selectedUnit].troopCosts && !EventSystem.current.IsPointerOverGameObject())
			{
				StartCoroutine(GoldWarning());
			}
			if (food < troops[selectedUnit].foodCosts && !EventSystem.current.IsPointerOverGameObject())
			{
				StartCoroutine(FoodWarning());
			}
			if (hit.collider != null)
			{
				if (hit.collider.gameObject.CompareTag("Battle ground") && selectionMode && !isPlacingBomb && !EventSystem.current.IsPointerOverGameObject()
					&& gold >= troops[selectedUnit].troopCosts && food >= troops[selectedUnit].foodCosts && (!GameObject.Find("Mobile") || (GameObject.Find("Mobile") && Mobile.deployMode)))
				{

					if (troops[selectedUnit].deployableTroops.CompareTag("Tree") && treesCount < maxTrees)
					{
						CreateUnit(hit);
					}
					else if (troops[selectedUnit].deployableTroops.CompareTag("Tree") && treesCount > maxTrees)
					{
						if (treeWarning != null) treeWarning.SetActive(true);
						DisappearTreeWarning();
					}
					else if (!troops[selectedUnit].deployableTroops.CompareTag("Tree"))
					{
						CreateUnit(hit);
					}

				}

				if (isPlacingBomb && !EventSystem.current.IsPointerOverGameObject())
				{
					Instantiate(bombExplosion, hit.point, Quaternion.identity);
					bombProgress = 0;
					isPlacingBomb = false;
					if (bombRange != null) bombRange.SetActive(false);

					GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
					foreach (GameObject enemy in enemies)
					{
						if (enemy != null && Vector3.Distance(enemy.transform.position, hit.point) <= BombRange / 2)
						{
							enemy.GetComponent<Character>().lives = 0;
						}
					}
				}
				else if (hit.collider.gameObject.CompareTag("Battle ground") && isPlacingBomb && EventSystem.current.IsPointerOverGameObject())
				{
					isPlacingBomb = false;
					if (bombRange != null) bombRange.SetActive(false);
				}
			}
		}
		else if (Input.GetMouseButtonDown(1) && hit.collider != null)
		{
			if (hit.collider.gameObject.CompareTag("Character") && !selectionMode && !isPlacingBomb && target != null && hit.collider.gameObject.GetComponent<Character>().lives > 0)
			{
				if (target != null)
				{
					target.transform.position = hit.collider.transform.position;
					target.SetActive(true);
					knights = GameObject.FindGameObjectsWithTag("Knight");
					foreach (GameObject knight in knights)
					{
						knight.SendMessage("SetTarget", target);
					}
				}
			}
		}

		if (Input.GetKeyDown(deselectKey))
		{
			selectionMode = false;
			selectButton.GetComponent<UnityEngine.UI.Text>().text = "Select Character";
			Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.Auto);
			if (target != null) target.SetActive(false);
		}

		//start selection mode when player presses spacebar
		if (Input.GetKeyDown("space"))
		{
			selectCharacters();
		}

		//for each button, check if we have enough gold to deploy the unit and color the button grey if it can not be deployed yet
		for (int i = 0; i < troops.Count; i++)
		{
			if (troops[i].button != null)
			{
				if (troops[i].troopCosts <= gold && troops[i].foodCosts <= food)
				{
					troops[i].button.gameObject.GetComponent<Image>().color = Color.white;
				}
				else
				{
					troops[i].button.gameObject.GetComponent<Image>().color = new Color(0.7f, 0.7f, 0.7f, 1);
				}
			}
		}

		if (Input.GetMouseButtonUp(0) && mobileDragInput && canDrag)
		{
			canDrag = false;
			Destroy(dragPreview);

			if (hit.collider != null && Input.touchCount > 0 && !EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
				CreateUnit(hit);
		}

		if (Input.GetMouseButton(0) && canDrag && dragPreview != null)
		{
			dragPreview.transform.position = Input.mousePosition;

			if (Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
			{
				dragPreview.GetComponent<Image>().enabled = false;
			}
			else
			{
				dragPreview.GetComponent<Image>().enabled = true;
			}
		}
	}

	private void CreateUnit(RaycastHit hit)
	{
		GameObject unit = Instantiate(troops[selectedUnit].deployableTroops, hit.point, Quaternion.identity);
		gold -= troops[selectedUnit].troopCosts;
		food -= troops[selectedUnit].foodCosts;
		if (unit.CompareTag("Tree")) treesCount++;
		unit.transform.parent = characterParent.transform;
		Instantiate(newUnitEffect, hit.point, Quaternion.identity);
	}

	void OnGUI()
	{
		//check if rectangle should be visible
		if (visible)
		{
			// Find the corner of the box
			Vector2 origin;
			origin.x = Mathf.Min(mouseDownPos.x, mouseLastPos.x);

			// GUI and mouse coordinates are the opposite way around.
			origin.y = Mathf.Max(mouseDownPos.y, mouseLastPos.y);
			origin.y = Screen.height - origin.y;

			//Compute size of box
			Vector2 size = mouseDownPos - mouseLastPos;
			size.x = Mathf.Abs(size.x);
			size.y = Mathf.Abs(size.y);

			// Draw the GUI box
			Rect rect = new Rect(origin.x, origin.y, size.x, size.y);
			GUI.Box(rect, "", rectangleStyle);

			foreach (GameObject knight in knights)
			{
				if (knight != null)
				{
					Vector3 pos = Camera.main.WorldToScreenPoint(knight.transform.position);
					pos.y = Screen.height - pos.y;
					//foreach selectable character check its position and if it is within GUI rectangle, set selected to true
					if (rect.Contains(pos))
					{
						Character character = knight.GetComponent<Character>();
						character.selected = true;
						character.selectedObject.SetActive(true);
					}
				}
			}
		}
	}

	//function to select another unit
	public void selectUnit(int unit)
	{
		if (highlightSelectedButton)
		{
			//remove all outlines and set the current button outline visible
			for (int i = 0; i < troops.Count; i++)
			{
				if (troops[i].button != null)
					troops[i].button.GetComponent<Outline>().enabled = false;
			}

			troops[unit].button.GetComponent<Outline>().enabled = true;
		}

		//selected unit is the pressed button
		selectedUnit = unit;

		if (mobileDragInput && !isPlacingBomb && gold >= troops[selectedUnit].troopCosts && food >= troops[selectedUnit].foodCosts)
		{
			dragPreview = Instantiate(troops[unit].button);
			dragPreview.transform.SetParent(GameObject.FindObjectOfType<Settings>().gameObject.transform);

			if (!originalPreviewSize)
			{
				dragPreview.GetComponent<RectTransform>().sizeDelta = new Vector2(dragPreviewSize, dragPreviewSize);
			}
			else
			{
				dragPreview.GetComponent<RectTransform>().sizeDelta = troops[unit].button.GetComponent<RectTransform>().sizeDelta;
			}

			Destroy(dragPreview.GetComponent<buttonStats>());
			canDrag = true;
		}
	}

	public void selectCharacters()
	{
		Debug.Log("select" + selectionMode);
		//turn selection mode on/off
		selectionMode = !selectionMode;
		if (selectionMode)
		{
			//set cursor and button color to show the player selection mode is active
			selectButton.GetComponent<Image>().color = Color.red;
			Cursor.SetCursor(cursorTexture1, Vector2.zero, CursorMode.Auto);
			GameObject mobileObject = GameObject.Find("Mobile");
			if (mobileObject != null)
			{
				Debug.Log("deplymode" + Mobile.deployMode);
				if (!Mobile.deployMode)
				{
					Debug.Log("I am inside");
					mobileObject.GetComponent<Mobile>().toggleDeployMode();
				}
				Mobile.camEnabled = false;
			}
		}
		else
		{
			//show the player selection mode is not active
			selectButton.GetComponent<Image>().color = Color.white;
			Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.Auto);

			//set target object false and deselect all units
			foreach (GameObject knight in knights)
			{
				if (knight != null)
				{
					Character character = knight.GetComponent<Character>();
					character.selected = false;
					character.selectedObject.SetActive(false);
				}
			}
			target.SetActive(false);
			GameObject mobileObject = GameObject.Find("Mobile");
			if (mobileObject != null)
			{
				Mobile.camEnabled = true;
			}
		}
	}


	//warning if you need more gold
	private IEnumerator GoldWarning()
	{
		if (goldWarning != null) goldWarning.SetActive(true);
		yield return new WaitForSeconds(1.5f);
		if (goldWarning != null) goldWarning.SetActive(false);
	}
	//warning if you need more food
	private IEnumerator FoodWarning()
	{
		if (foodWarning != null) foodWarning.SetActive(true);
		yield return new WaitForSeconds(1.5f);
		if (foodWarning != null) foodWarning.SetActive(false);
	}

	private IEnumerator TreeWarning()
	{
		if (treeWarning != null) treeWarning.SetActive(true);
		yield return new WaitForSeconds(1.5f);
		if (treeWarning != null) treeWarning.SetActive(false);
	}
	public void addCharacterButtons()
	{
		bool unitShop = (GameObject.Find("Unit shop") != null);

		//for all troops...
		for (int i = 0; i < troops.Count; i++)
		{
			if (((i == 0 || PlayerPrefs.GetInt("unit" + i) == 1) && unitShop) || !unitShop)
			{
				//add a button to the list of buttons
				GameObject newButton = Instantiate(button);
				RectTransform rectTransform = newButton.GetComponent<RectTransform>();
				rectTransform.SetParent(characterList.transform, false);

				//set button outline
				newButton.GetComponent<Outline>().effectColor = buttonHighlight;

				//set the correct button sprite
				newButton.gameObject.GetComponent<Image>().sprite = troops[i].buttonImage;
				//only enable outline for the first button
				if (i == 0 && highlightSelectedButton)
				{
					newButton.GetComponent<Outline>().enabled = true;
				}
				else
				{
					newButton.GetComponent<Outline>().enabled = false;
				}

				//set button name to its position in the list(important for the button to work later on)
				newButton.transform.name = "" + i;

				//set the button stats
				newButton.GetComponentInChildren<Text>().text = "Price: " + troops[i].troopCosts +
				"\n Damage: " + troops[i].deployableTroops.GetComponentInChildren<Character>().damage +
				"\n Lives: " + troops[i].deployableTroops.GetComponentInChildren<Character>().lives;

				//this is the new button
				troops[i].button = newButton;
			}
		}
	}

	public void placeBomb()
	{
		//start placing a bomb
		isPlacingBomb = true;
	}

	//functions which adds 100 to your gold amount and shows text to let player know
	void AddGold()
	{
		gold += 100;
		StartCoroutine(AddedGoldText());
	}

	void AddFood()
	{
		food += 50;
		StartCoroutine(AddedFoodText());
	}

	IEnumerator AddedGoldText()
	{
		addedGoldText.SetActive(true);
		yield return new WaitForSeconds(0.7f);
		addedGoldText.SetActive(false);
	}
	IEnumerator AddedFoodText()
	{
		addedfoodText.SetActive(true);
		yield return new WaitForSeconds(0.7f);
		addedfoodText.SetActive(false);
	}

	private void DisappearTreeWarning()
	{
		StartCoroutine(TreeWarning());
	}

	private void OnDisable()
	{
		FoodProvider.AddFoodCount -= AddFood;
	}
}