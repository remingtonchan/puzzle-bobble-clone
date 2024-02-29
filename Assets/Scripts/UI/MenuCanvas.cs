using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class MenuCanvas : MonoBehaviour
    {
        [SerializeField] private Button mainPanelButton;
        [SerializeField] private TextMeshProUGUI instructionLabel;
        [SerializeField] private TextMeshProUGUI headerLabel;
        [SerializeField] private TextMeshProUGUI subtitleLabel;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private CanvasGroup mainPanelCanvasGroup;
        [SerializeField] private CanvasGroup inGameCanvasGroup;

        private void OnEnable()
        {
            mainPanelButton.onClick.AddListener(() =>
            {
                if (GameManager.Instance.CurrentGameState != GameManager.GameState.Title &&
                    GameManager.Instance.CurrentGameState != GameManager.GameState.GameOver)
                {
                    return;
                }
                
                GameManager.Instance.StartGame();
                StartCoroutine(FadePanel(false));
            });
        }

        private void OnDisable()
        {
            mainPanelButton.onClick.RemoveAllListeners();
        }

        private IEnumerator FadePanel(bool fadeIn)
        {
            var elapsedTime = 0f;
            var targetAlpha = fadeIn ? 1f : 0f;

            const float fadeInDuration = 0.5f;

            while (elapsedTime < fadeInDuration)
            {
                var alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeInDuration);

                mainPanelCanvasGroup.alpha = alpha;
                inGameCanvasGroup.alpha = 1f - alpha;

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            mainPanelCanvasGroup.alpha = targetAlpha;
            inGameCanvasGroup.alpha = 1f - targetAlpha;
        }

        public void SetScore(int score)
        {
            scoreText.text = score.ToString();
        }

        public void SetTimer(int value)
        {
            timerText.text = value.ToString();
        }

        public void SetVictory(int score)
        {
            headerLabel.text = "Congratulations!";
            subtitleLabel.text = $"Your final score is {score}";
            instructionLabel.text = "Click anywhere to play again.";

            StartCoroutine(FadePanel(true));
        }

        public void SetGameOver(int score)
        {
            headerLabel.text = "Game Over";
            subtitleLabel.text = $"Your final score is {score}";
            instructionLabel.text = "Click anywhere to play again.";

            StartCoroutine(FadePanel(true));
        }    
    }
}
