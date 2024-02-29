using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Model
{
    /// <summary>
    /// Represents the full playable grid in a stage
    /// </summary>
    [Serializable]
    public class CellGrid
    {
        [SerializeField] private int columns= 10;
        [SerializeField] private int rows = 24;
        [SerializeField] private int initialRowCount = 5;
        public Cell[,] Matrix { get; private set; }

        private int _columnCount;
        private List<Cell.Type> _activeTypes = new();

        public bool IsAnchoredLeft { get; private set; }
        public Cell CurrentBullet { get; private set; }
        public Cell NextBullet { get; private set; }
        
        public void Initialize()
        {
            Matrix = new Cell[columns, rows];

            _columnCount = Matrix.GetLength(0);

            for (var column = 0; column < _columnCount; column++)
            {
                for (var row = 0; row < initialRowCount; row++)
                {
                    Matrix[column, row] = new Cell(new Vector2Int(column, row));
                }
            }

            CurrentBullet = new Cell((Cell.Type) Random.Range(0, (int) Cell.Type.Length));
            // CurrentBullet = new Cell(Cell.Type.Blue);
            NextBullet = new Cell((Cell.Type) Random.Range(0, (int) Cell.Type.Length));
        }

        public void TearDown()
        {
            Array.Clear(Matrix, 0, Matrix.Length);
            CurrentBullet = null;
            NextBullet = null;
        }

        public void AttachNewCell(int x, int y)
        {
            Matrix[x , y] = CurrentBullet;
            CurrentBullet.currentPosition = new Vector2Int(x, y);
            CurrentBullet = null; 
        }

        public void CreateNewBullet()
        {
            CurrentBullet = NextBullet;
            NextBullet = new Cell(GetValidCellType());
        }

        public int RemoveCells(List<Vector2Int> cellsToRemove)
        {
            var count = 0;
            foreach (var pair in cellsToRemove)
            {
                if (pair.x < Matrix.GetLength(0) && pair.y < Matrix.GetLength(1))
                {
                    count++;
                    Matrix[pair.x, pair.y] = null;
                }
            }

            return count;
        }

        public HashSet<Cell> FindFloatingCells()
        {
            var matrixSet = Matrix.Cast<Cell>().Where(cell => cell != null).ToHashSet();
            return matrixSet.Except(FindAttachedCells()).ToHashSet();
        }

        public int GetActiveCellsCount()
        {
            var attachedCells = FindAttachedCells();
            return attachedCells.Count;
        }
    
        private HashSet<Cell> FindAttachedCells()
        {
            _activeTypes.Clear();
            var cells = new HashSet<Cell>();

            for (var column = 0; column < _columnCount; column++)
            {
                var cell = Matrix[column, 0];
                if (cell != null)
                {
                    cells.UnionWith(GetConnectedCellsNonMatching(cell));
                }
            }
            
            _activeTypes = new HashSet<Cell.Type>(cells.Select(cell => cell.CellType)).ToList();

            return cells;
        }
    
        public void AddNewRow()
        {
            MoveCellsDown(Matrix);
            for (var column = 0; column < _columnCount; column++)
            {
                Matrix[column, 0] = new Cell(new Vector2Int(column, 0));
            }
        }

        private Cell.Type GetValidCellType()
        {
            return _activeTypes.Any() ? _activeTypes[Random.Range(0, _activeTypes.Count)] : Cell.Type.Blue;
        }

        private void MoveCellsDown(Cell[,] array)
        {
            var cols = array.GetLength(0);
            var ros = array.GetLength(1);
        
            for (var row = ros - 2; row >= 0; row--)
            {
                for (var column = 0; column < cols; column++)
                {
                    var cellToMove = array[column, row];
                    array[column, row + 1] = cellToMove;
                    if (cellToMove != null)
                    {
                        cellToMove.previousPosition = new Vector2Int(column, row);
                        cellToMove.currentPosition = new Vector2Int(column, row + 1);
                    }
                    array[column, row] = null;
                }
            }

            IsAnchoredLeft = !IsAnchoredLeft;
        }

        public HashSet<Cell> GetConnectedCells(Cell cell, int minimumCount = 3)
        {
            var matches = new HashSet<Cell>();
            GetConnectedCells(cell.currentPosition.x, cell.currentPosition.y, (toMatch) => cell.CellType == toMatch.CellType, ref matches);

            return matches.Count >= minimumCount ? matches : null;
        }

        private HashSet<Cell> GetConnectedCellsNonMatching(Cell cell)
        {
            var matches = new HashSet<Cell>();
            GetConnectedCells(cell.currentPosition.x, cell.currentPosition.y, (_) => true,  ref matches);

            return matches;
        }
    
        private void GetConnectedCells(int x, int y, Func<Cell, bool> comparisonFunction, ref HashSet<Cell> matches)
        {
            if (x < 0 || x >= Matrix.GetLength(0) || y < 0 || y >= Matrix.GetLength(1))
            {
                return;
            }
        
            var currentCell = Matrix[x, y];

            if (currentCell == null)
            {
                return;
            }
        
            if (matches.Contains(currentCell) || !comparisonFunction(currentCell))
            {
                return;
            }

            matches.Add(currentCell);

            GetConnectedCells(x + 1, y, comparisonFunction, ref matches);
            GetConnectedCells(x - 1, y, comparisonFunction, ref matches);
            GetConnectedCells(x, y + 1, comparisonFunction, ref matches);
            GetConnectedCells(x, y - 1, comparisonFunction, ref matches);

            if (IsAnchoredLeft && y % 2 == 0 || !IsAnchoredLeft && y % 2 != 0)
            {
                GetConnectedCells(x - 1, y - 1, comparisonFunction, ref matches);
                GetConnectedCells(x - 1, y + 1, comparisonFunction, ref matches);
            }
            else
            {
                GetConnectedCells(x + 1, y - 1, comparisonFunction, ref matches);
                GetConnectedCells(x + 1, y + 1, comparisonFunction, ref matches);
            }
        }
    }
}
