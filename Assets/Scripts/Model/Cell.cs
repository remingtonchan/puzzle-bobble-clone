using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Model
{
    [Serializable]
    public class Cell
    {
        public enum Type
        {
            Brown, 
            Violet, 
            Rust, 
            Red, 
            Blue, 
            Green,
            Length
        }

        public Vector2Int currentPosition;
        public Vector2Int previousPosition;
    
        public Type CellType { get; private set; }

        public Cell(Type type)
        {
            CellType = type;
        }

        public Cell(Vector2Int position)
        {
            CellType = (Type) Random.Range(0, (int) Type.Length);
            currentPosition = position;
        }

        public Cell(Type type, Vector2Int position)
        {
            CellType = type;
            currentPosition = position;
        }
    }
}
