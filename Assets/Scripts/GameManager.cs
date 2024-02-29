using UI;
using UnityEngine;
using View;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private GridManager gridManager;
    [SerializeField] private MenuCanvas menuCanvas;
    [SerializeField] private float loopInterval = 10f;
    [SerializeField] private AudioSource threeSecondsTimerAudioSource;
    [SerializeField] private AudioSource victoryAudioSource;
    [SerializeField] private AudioSource defeatAudioSource;
    private float _timeLeft;
    private bool _hasPlayedThisLoop;

    private const int POINTS = 100;

    public GameState CurrentGameState { get; private set; } = GameState.Title;

    private int _score;

    private void Awake()
    {
        Instance = this;
        CurrentGameState = GameState.Title;
    }
    
    private void StartTimer()
    {
        CancelInvoke(nameof(TimerFunction));
        InvokeRepeating(nameof(TimerFunction), 0f, 1f); 
    }

    private void StopTimer()
    {
        CancelInvoke(nameof(TimerFunction));
    }
    
    private void TimerFunction()
    {
        _timeLeft -= 1f;

        if (_timeLeft <= 3f)
        {
            if (!_hasPlayedThisLoop)
            {
                threeSecondsTimerAudioSource.time = 0f;
                threeSecondsTimerAudioSource.Play();
                _hasPlayedThisLoop = true;
            }
        }
        
        if (_timeLeft <= 0f)
        {
            gridManager.DropDownGrid();
            _timeLeft = loopInterval;
            _hasPlayedThisLoop = false;
        }
        
        menuCanvas.SetTimer((int) _timeLeft);
    }

    public void StartGame()
    {
        _score = 0;
        gridManager.Initialize();
        menuCanvas.SetScore(0);
        CurrentGameState = GameState.GamePlay;
        _timeLeft = loopInterval;
        StartTimer();
        StartCoroutine(gridManager.Fade(true));
    }
    
    public void AddBubblePopped(int bubblesPopped)
    {
        _score += bubblesPopped * POINTS;
        menuCanvas.SetScore(_score);
    }

    public void SetVictory()
    {
        victoryAudioSource.time = 0f;
        victoryAudioSource.Play();
        StopTimer();
        menuCanvas.SetScore(0);
        CurrentGameState = GameState.GameOver;
        menuCanvas.SetVictory(_score);
        gridManager.TearDown();
    }
    
    public void SetGameOver()
    {
        defeatAudioSource.time = 0f;
        defeatAudioSource.Play();
        StopTimer();
        menuCanvas.SetScore(0);
        CurrentGameState = GameState.GameOver;
        menuCanvas.SetGameOver(_score);
        gridManager.TearDown();
    }
    
    public enum GameState
    {
        Title,
        GamePlay,
        GameOver,
    }
}
