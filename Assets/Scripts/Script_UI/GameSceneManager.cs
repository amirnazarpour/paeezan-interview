using Enums;
using GameCore;
using ScriptableObjects.GameEvents;
using ScriptableObjects.Services;
using TMPro;
using UnityEngine;

namespace UI
{
    public class GameSceneManager : MonoBehaviour
    {
        [SerializeField] private IntEvent OnScoreChanged;
        [SerializeField] private NullEvent OnGameEnded;
        [SerializeField] private NullEvent OnGameStarted;

        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private TextMeshProUGUI playerScoreText;
        [SerializeField] private TextMeshProUGUI playerHighScoreText;
        [SerializeField] private TextMeshProUGUI playerCoinsText;
        [SerializeField] private TextMeshProUGUI resultCoinsText;
        [SerializeField] private BallController ball;
       
        
        private int playerScore;
        private int playerHighScore;
        private int runCoins;
        private bool highScoreSoundPlayed;

        private void Start()
        {
            playerNameText.text = PlayerPrefsSaveService.Main.LoadString("PlayerName", "Honey Drops");
            
            playerScore = 0;
            playerScoreText.text = "Score : 0";

            playerHighScore = PlayerPrefsSaveService.Main.LoadInt("PlayerScore", 5000);
            playerHighScoreText.text = "HighScore : " + playerHighScore;

            if (!ball || !playerCoinsText || !resultCoinsText)
                Debug.LogError("GameSceneManager requires the scene BallController, GameUi/PlayerCoins text, and GameOverUI/PlayerCoins result text assigned in Scene_Game.", this);

            runCoins = 0;
            RefreshCoins();
        }

        private void OnEnable()
        {
            if (ball)
                ball.CoinsEarned += OnCoinsEarned;
     
            OnScoreChanged.OnEventRaised += OnScoreChange;
            OnGameStarted.OnEventRaised += OnGameStart;
       
        }

       

        private void OnDisable()
        {
            if (ball)
                ball.CoinsEarned -= OnCoinsEarned;

            OnScoreChanged.OnEventRaised -= OnScoreChange;
            OnGameStarted.OnEventRaised -= OnGameStart;
         
        }

        private void OnGameStart()
        {
            highScoreSoundPlayed = false;

            playerScore = 0;
            playerScoreText.text = "Score : 0";
            runCoins = 0;
            RefreshCoins();
        }

        private void OnScoreChange(int score)
        {
            playerScore = score;
            playerScoreText.text = "Score : " + score;
            
            if (playerScore > playerHighScore)
            {
                playerHighScore = playerScore;
                PlayerPrefsSaveService.Main.SaveInt("PlayerScore", playerHighScore);
                playerHighScoreText.text = "HighScore : " + playerHighScore;
                
                if (!highScoreSoundPlayed)
                {
                    highScoreSoundPlayed = true;
                    AudioManger.AudioManager.Instance.PlaySFX(SoundType.HighScore);
                }
            }
        }

        private void OnCoinsEarned(int amount)
        {
            if (amount <= 0)
                return;

            runCoins += amount;
            RefreshCoins();
        }

        private void RefreshCoins()
        {
            if (playerCoinsText)
                playerCoinsText.text = "Coins : " + runCoins;
            if (resultCoinsText)
                resultCoinsText.text = "Coins : " + runCoins;
        }

    }
}
