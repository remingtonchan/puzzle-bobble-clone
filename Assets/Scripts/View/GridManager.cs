using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Model;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Serialization;

namespace View
{
    public class GridManager : MonoBehaviour
    {
        [SerializeField] private CellGrid grid;
        [SerializeField] private CellView cellViewPrefab;
        [SerializeField] private Sprite brownSprite;
        [SerializeField] private Sprite violetSprite;
        [SerializeField] private Sprite rustSprite;
        [SerializeField] private Sprite redSprite;
        [SerializeField] private Sprite blueSprite;
        [SerializeField] private Sprite greenSprite;
        [SerializeField] private SpriteRenderer nextBulletDisplay;
        [SerializeField] private Transform bulletOrigin;
        [SerializeField] private AudioSource shrinkAudioSource;
        [SerializeField] private AudioSource dropAudioSource;
        [SerializeField] private AudioSource launchAudioSource;
        [SerializeField] private AudioSource bounceAudioSource;
        [SerializeField] private AudioSource bonusAudioSource;
        [SerializeField] private List<Transform> toggleableObjects;
        private List<Renderer> _renderers;
        private Dictionary<Cell.Type, Sprite> _spriteMap = new();
        private readonly Dictionary<Cell, CellView> _cellMap = new();
        private ObjectPool<CellView> _cellViewPool;
        private CellView _currentBulletView;
        private CellView _nextBullet;
        private Vector2 _bulletOriginScreenPoint;
        private bool _fadeComplete;
        private bool _processingOnHit;
        private bool _gameOverTriggered;
    
        private void Awake()
        {
            _spriteMap = new Dictionary<Cell.Type, Sprite>
            {
                {Cell.Type.Brown, brownSprite},
                {Cell.Type.Violet, violetSprite},
                {Cell.Type.Rust, rustSprite},
                {Cell.Type.Red, redSprite},
                {Cell.Type.Blue, blueSprite},
                {Cell.Type.Green, greenSprite},
            };

            _cellViewPool = new ObjectPool<CellView>(() => Instantiate(cellViewPrefab, transform, true),
                view =>
                {
                    view.gameObject.SetActive(true);
                    view.transform.localScale = Vector3.one;
                },
                view =>
                {
                    view.SetIsBullet(false);
                    view.SetOnHitCallback(null);
                    view.SetOnBounceCallback(null);
                    view.SetOnReachLine(null);
                    view.gameObject.SetActive(false);
                },
                view => Destroy(view.gameObject), true, 50, 250);
        
            _bulletOriginScreenPoint = Camera.main.WorldToScreenPoint(bulletOrigin.transform.position);
        }

        private void Update()
        {
            HandleInput();
        }

        public void Initialize()
        {
            _gameOverTriggered = false;
            grid.Initialize();
            UpdateGrid();
            UpdateBullet();
            
            _renderers = GetComponentsInChildren<Renderer>().ToList();
            foreach (var rd in _renderers)
            {
                if (rd.gameObject.GetComponent<TextMeshPro>())
                {
                    rd.gameObject.GetComponent<TextMeshPro>().alpha = 0f;
                    continue;
                }
                if (rd != null)
                {
                    var currentColor = rd.material.color;
                    rd.material.color = new Color(currentColor.r, currentColor.g, currentColor.b, 0f);
                }
            }
            
            toggleableObjects.ForEach(t => t.gameObject.SetActive(true));
        }

        public void TearDown()
        {
            grid.TearDown();
            foreach (var cell in _cellMap.Values)
            {
                _cellViewPool.Release(cell);
            }
            
            _cellViewPool.Release(_currentBulletView);

            _cellMap.Clear();
        }

        public IEnumerator Fade(bool fadeIn)
        {
            var elapsedTime = 0f;
            var targetAlpha = fadeIn ? 1f : 0f;

            const float fadeInDuration = 0.5f;

            while (elapsedTime < fadeInDuration)
            {
                var alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeInDuration);

                foreach (var rd in _renderers)
                {
                    if (rd.gameObject.GetComponent<TextMeshPro>())
                    {
                        rd.gameObject.GetComponent<TextMeshPro>().alpha = alpha;
                        continue;
                    }
                    if (rd != null)
                    {
                        var currentColor = rd.material.color;
                        rd.material.color = new Color(currentColor.r, currentColor.g, currentColor.b, alpha);
                    }
                }

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            foreach (var rd in _renderers)
            {
                if (rd.gameObject.GetComponent<TextMeshPro>())
                {
                    rd.gameObject.GetComponent<TextMeshPro>().alpha = targetAlpha;
                    continue;
                }
                if (rd != null)
                {
                    var currentColor = rd.material.color;
                    rd.material.color = new Color(currentColor.r, currentColor.g, currentColor.b, targetAlpha);
                }
            }

            _fadeComplete = true;
        }

        public void DropDownGrid()
        {
            dropAudioSource.time = 0.5f;
            dropAudioSource.Play();
            grid.AddNewRow();
            UpdateGrid();
        }
        
        private void UpdateBullet()
        {
            _currentBulletView = _cellViewPool.Get();
            _currentBulletView.SetSprite(_spriteMap[grid.CurrentBullet.CellType]);
            _currentBulletView.SetIsBullet(true);
            _currentBulletView.SetOnHitCallback(() =>
            {
                StartCoroutine(OnHit());
            });
            
            _currentBulletView.SetOnBounceCallback(() =>
            {
                bounceAudioSource.time = 0f;
                bounceAudioSource.Play();
            });
        
            _currentBulletView.transform.position = bulletOrigin.transform.position;

            nextBulletDisplay.sprite = _spriteMap[grid.NextBullet.CellType];
            nextBulletDisplay.size = Vector2.one;
        }

        private IEnumerator OnHit()
        {
            _processingOnHit = true;

            var targetPosition = ConvertToGrid(_currentBulletView.transform.position);
            var bulletCell = grid.CurrentBullet;
            grid.AttachNewCell(targetPosition.x, targetPosition.y);
            _cellMap[grid.Matrix[targetPosition.x, targetPosition.y]] = _currentBulletView;
            _currentBulletView = null;
            UpdateGrid();

            var matches = grid.GetConnectedCells(bulletCell);
            
            if (matches != null && matches.Any()) 
            {
                // VISUAL change
                shrinkAudioSource.time = 0f;
                shrinkAudioSource.Play();
                yield return ScaleDownCoroutine(matches.Select(t => _cellMap[t].transform).ToList());
                foreach (var match in matches)
                {
                    var view = _cellMap[match];
                    _cellViewPool.Release(view);
                    _cellMap.Remove(match);
                }
                
                // DATA change
                var removed = grid.RemoveCells(matches.Select(t=> t.currentPosition).ToList());
                GameManager.Instance.AddBubblePopped(removed);
            }

            var floaters = grid.FindFloatingCells();
            if (floaters != null && floaters.Any())
            {
                // VISUAL change
                bonusAudioSource.time = 0f;
                bonusAudioSource.Play();
                yield return ScaleDownCoroutine(floaters.Select(t => _cellMap[t].transform).ToList());
                foreach (var floater in floaters)
                {
                    var view = _cellMap[floater];
                    _cellViewPool.Release(view);
                    _cellMap.Remove(floater);
                }
                
                // DATA change
                var removed = grid.RemoveCells(floaters.Select(t=> t.currentPosition).ToList());
                GameManager.Instance.AddBubblePopped(removed);
            }

            // DATA change
            grid.CreateNewBullet();
            // VISUAL change
            UpdateBullet();
                
            if (grid.GetActiveCellsCount() == 0)
            {
                GameManager.Instance.SetVictory();
            }

            _processingOnHit = false;
            yield return null;
        }

        private IEnumerator ScaleDownCoroutine(List<Transform> objectsToScale)
        {
            const float scaleDuration = 0.15f;
            var elapsedTime = 0f;
            var initialScale = objectsToScale[0].localScale;

            while (elapsedTime < scaleDuration)
            {
                var t = elapsedTime / scaleDuration;
                var newScale = Vector3.Lerp(initialScale, Vector3.zero, t);

                foreach (var objTransform in objectsToScale)
                {
                    objTransform.localScale = newScale;
                }

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            foreach (var objTransform in objectsToScale)
            {
                objTransform.localScale = Vector3.zero;
            }
        }

        private void HandleInput()
        {
            if (_processingOnHit)
            {
                return;
            }
            
            if (!_fadeComplete)
            {
                return;
            }

            if (_gameOverTriggered)
            {
                return;
            }
            
            if (GameManager.Instance.CurrentGameState != GameManager.GameState.GamePlay)
            {
                return;
            }
            
            if (_currentBulletView != null && _currentBulletView.IsFired)
            {
                return;
            }
        
            if (Input.GetMouseButtonDown(0))
            {
                var direction = ((Vector2) Input.mousePosition - _bulletOriginScreenPoint).normalized;
                launchAudioSource.time = 0f;
                launchAudioSource.Play();
                _currentBulletView.Fire(direction);
            }
        }

        private void UpdateGrid()
        {
            foreach (var cell in _cellMap.Values)
            {
                _cellViewPool.Release(cell);
            }
            
            _cellMap.Clear();
            
            var totalColumns = grid.Matrix.GetLength(0);
            var totalRows = grid.Matrix.GetLength(1);
            for (var row = 0; row < totalRows; row++)
            {
                for (var column = 0; column < totalColumns; column++)
                {
                    if (grid.Matrix[column, row] != null)
                    {
                        var cell = grid.Matrix[column, row];
                        var cellView = _cellViewPool.Get();
                        cellView.SetSprite(_spriteMap[cell.CellType]);
                        cellView.SetOnReachLine(() =>
                        {
                            if (!_gameOverTriggered)
                            {
                                _gameOverTriggered = true;
                                StartCoroutine(GameOverPhase());
                            }
                        });
            
                        float xPos;
                        if (grid.IsAnchoredLeft)
                        {
                            xPos = row % 2 == 0 ? cell.currentPosition.x : cell.currentPosition.x + 0.5f;
                        }
                        else
                        {
                            xPos = row % 2 != 0 ? cell.currentPosition.x : cell.currentPosition.x + 0.5f;
                        }
                    
                        var yPos = cell.currentPosition.y * -1f;
                        cellView.transform.position = new Vector2(xPos, yPos);
                        _cellMap[cell] = cellView;
                    }
                }
            }
        }

        private IEnumerator GameOverPhase()
        {
            yield return ScaleDownCoroutine(_cellMap.Values.Select(t => t.transform).ToList());
            toggleableObjects.ForEach(t => t.gameObject.SetActive(false));
            GameManager.Instance.SetGameOver();
        }

        /// <summary>
        /// Find the effective position on our grid given the world position of the view
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private Vector2Int ConvertToGrid(Vector2 position)
        {
            var isAnchoredLeft = grid.IsAnchoredLeft;

            var column = Mathf.RoundToInt(position.x);

            var row = Mathf.RoundToInt(position.y / -1.0f);
            
            if (isAnchoredLeft)
            {
                if (row % 2 != 0)
                {
                    column =  Mathf.RoundToInt(position.x - 0.5f);
                }
            }
            else
            {
                if (row % 2 == 0)
                {
                    column = Mathf.RoundToInt(position.x - 0.5f);
                }
            }

            return new Vector2Int(column, row);
        }
    }
}
