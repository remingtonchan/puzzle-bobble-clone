using System;
using UnityEngine;

namespace View
{
    public class CellView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        private Action _clickCallback;
        private Action _onHit;
        private Action _onBounce;
        private Action _onReachLine;

        private bool _isBullet;
        private bool _isFired;
        private Vector2 _direction;
        private bool _hasProcessedHit;
        private Rigidbody2D _rb;
        private const float SPEED = 20f;

        public bool IsFired => _isFired;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }
    
        private void Update()
        {
            if (_isFired)
            {
                _rb.position += _direction * (Time.deltaTime * SPEED);
            }
        }

        private void OnCollisionEnter2D(Collision2D other)
        {
            if (_isBullet && !_hasProcessedHit)
            {
                if (other.gameObject.CompareTag("Wall"))
                {
                    _direction = new Vector2(_direction.x * -1f, _direction.y);
                    _onBounce?.Invoke();
                }
        
                var cellViewHit = other.gameObject.GetComponent<CellView>();
                if (cellViewHit == null && !other.gameObject.CompareTag("Ceiling"))
                {
                    return;
                }
        
                _hasProcessedHit = true;
                _isFired = false;
                _onHit?.Invoke();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_isBullet)
            {
                if (other.gameObject.CompareTag("GameOverLine"))
                {
                    _onReachLine?.Invoke();
                }
            }
        }

        public void SetIsBullet(bool isBullet)
        {
            _isBullet = isBullet;
        }

        public void Fire(Vector2 direction)
        {
            _hasProcessedHit = false;
            _direction = direction;
            _isFired = true;
        }

        public void SetOnHitCallback(Action action)
        {
            _onHit = action;
        }
        
        public void SetOnBounceCallback(Action action)
        {
            _onBounce = action;
        }

        public void SetOnReachLine(Action action)
        {
            _onReachLine = action;
        }

        public void SetSprite(Sprite sprite)
        {
            spriteRenderer.sprite = sprite;
        }
    }
}
