using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    private int playerScore;
    private int enemyScore;

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
    }

    public void AddPlayerScore(int score)
    {
        playerScore += score;
        UpdatePlayerScoreUI();
    }

    public void AddEnemyScore(int score)
    {
        enemyScore += score;
        UpdateEnemyScoreUI();
    }

    private void UpdatePlayerScoreUI()
    {
        // Implement your UI update logic here
        Debug.Log("Player Score: " + playerScore);
    }

    private void UpdateEnemyScoreUI()
    {
        // Implement your UI update logic here
        Debug.Log("Enemy Score: " + enemyScore);
    }
}
