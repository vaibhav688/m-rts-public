using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Fusion;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameObject unitPrefab;
    public GameObject monkPrefab;
    public GameObject playerCastlePrefab;
    public GameObject enemyCastlePrefab;
    public Transform playerCastleSpawnPoint;
    public Transform enemyCastleSpawnPoint;
    public Transform playerSpawnPoint;
    public Transform enemySpawnPoint;

    public List<Card> cardAssets;

    public TMP_Text playerScoreText;
    public TMP_Text enemyScoreText;

    private NetworkRunner runner;

    private List<Unit> playerUnits = new List<Unit>();
    private List<Unit> enemyUnits = new List<Unit>();
    private Castle playerCastle;
    private Castle enemyCastle;

    private int playerScore;
    private int enemyScore;

    public delegate void CastlesInitialized();
    public event CastlesInitialized OnCastlesInitialized;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        runner = FindObjectOfType<NetworkRunner>();
        if (runner == null)
        {
            Debug.LogError("No NetworkRunner found in the scene.");
        }
    }

    public void StartBattle()
    {
        OnCastlesInitialized += () =>
        {
            ApplyCardEffects();
            StartCoroutine(DelayedSpawnUnits());
            StartCoroutine(CheckCastleHealth());
        };
        SpawnCastles();
    }

    private void ApplyCardEffects()
    {
        foreach (var card in cardAssets)
        {
            foreach (var unit in playerUnits)
            {
                card.ApplyEffect(unit);
            }
        }
    }

    private void SpawnCastles()
    {
        var playerCastleInstance = runner.Spawn(playerCastlePrefab, playerCastleSpawnPoint.position, Quaternion.identity)?.GetComponent<Castle>();
        if (playerCastleInstance == null)
        {
            Debug.LogError("Player castle instantiation failed!");
            return;
        }
        playerCastleInstance.Initialize(runner.LocalPlayer);
        playerCastle = playerCastleInstance;
        Debug.Log($"Player castle spawned and initialized with owner: {runner.LocalPlayer}");

        var enemyCastleInstance = runner.Spawn(enemyCastlePrefab, enemyCastleSpawnPoint.position, Quaternion.identity)?.GetComponent<Castle>();
        if (enemyCastleInstance == null)
        {
            Debug.LogError("Enemy castle instantiation failed!");
            return;
        }
        enemyCastleInstance.Initialize(default); // Assuming default for the enemy player
        enemyCastleInstance.gameObject.tag = "Enemy Castle"; // Ensure enemy castle is tagged
        enemyCastle = enemyCastleInstance;
        Debug.Log($"Enemy castle spawned and initialized with owner: {default}");

        OnCastlesInitialized?.Invoke();
    }

    private IEnumerator DelayedSpawnUnits()
    {
        yield return new WaitForSeconds(1f); // Ensure castles are fully initialized
        SpawnUnits();
    }

    public void SpawnUnits()
    {
        PlayerRef playerRef = runner.LocalPlayer;
        PlayerRef enemyRef = default;

        for (int i = 0; i < 5; i++)
        {
            var unit = runner.Spawn(unitPrefab, playerSpawnPoint.position + Vector3.right * i, Quaternion.identity)?.GetComponent<Unit>();
            if (unit == null)
            {
                Debug.LogError("Player unit instantiation failed!");
                continue;
            }
            unit.Initialize(playerRef);
            playerUnits.Add(unit);
        }

        for (int i = 0; i < 5; i++)
        {
            var unit = runner.Spawn(unitPrefab, enemySpawnPoint.position + Vector3.right * i, Quaternion.identity)?.GetComponent<Unit>();
            if (unit == null)
            {
                Debug.LogError("Enemy unit instantiation failed!");
                continue;
            }
            unit.Initialize(enemyRef);
            enemyUnits.Add(unit);
        }
    }

    private IEnumerator CheckCastleHealth()
    {
        while (true)
        {
            if (playerCastle != null && enemyCastle != null)
            {
                if (playerCastle.lives <= 0)
                {
                    Debug.Log("Player has lost the game!");
                    OnCastleDestroyed(playerCastle);
                    break;
                }

                if (enemyCastle.lives <= 0)
                {
                    Debug.Log("Player has won the game!");
                    OnCastleDestroyed(enemyCastle);
                    break;
                }
            }

            yield return new WaitForSeconds(1f);
        }
    }

    public void OnCastleDestroyed(Castle destroyedCastle)
    {
        if (destroyedCastle == playerCastle)
        {
            Debug.Log("Game Over: Player Lost");
            enemyScore++;
        }
        else if (destroyedCastle == enemyCastle)
        {
            Debug.Log("Game Over: Player Won");
            playerScore++;
        }

        UpdateScoreUI();
    }

    private void UpdateScoreUI()
    {
        if (playerScoreText != null && enemyScoreText != null)
        {
            playerScoreText.text = $"Player Score: {playerScore}";
            enemyScoreText.text = $"Enemy Score: {enemyScore}";
        }
    }

    public void SpawnMonk(Vector3 position)
    {
        var monk = runner.Spawn(monkPrefab, position, Quaternion.identity)?.GetComponent<Monk>();
        if (monk == null)
        {
            Debug.LogError("Monk instantiation failed!");
            return;
        }
        monk.Initialize(runner.LocalPlayer);
        playerUnits.Add(monk);
    }

    public void UpdateScores(bool isPlayerUnit)
    {
        if (isPlayerUnit)
        {
            enemyScore += 10;
        }
        else
        {
            playerScore += 10;
        }
        UpdateScoreUI();
    }
}
