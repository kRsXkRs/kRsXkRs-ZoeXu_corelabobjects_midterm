using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralMaze
{
    public class MazeController : MonoBehaviour
    {
        [Range(5, 100)] public int mazeWidth = 5, mazeHeight = 5;
        public bool generateEntryExit = true;

        private MazeCell[,] maze;
        public Vector2Int entry, exit;

        public MazeCell[,] GetMaze()
        {
            maze = new MazeCell[mazeWidth, mazeHeight];

            for (int x = 0; x < mazeWidth; x++)
            {
                for (int y = 0; y < mazeHeight; y++)
                {
                    maze[x, y] = new MazeCell(x, y);
                }
            }

            if (generateEntryExit)
                SetEntryAndExit();

            CarvePath(entry.x, entry.y);

            return maze;
        }

        void CarvePath(int x, int y)
        {
            Stack<Vector2Int> path = new Stack<Vector2Int>();
            path.Push(new Vector2Int(x, y));
            maze[x, y].visited = true;

            while (path.Count > 0)
            {
                Vector2Int current = path.Peek();
                List<Vector2Int> neighbors = GetValidNeighbors(current);

                if (neighbors.Count == 0)
                {
                    path.Pop();
                }
                else
                {
                    Vector2Int next = neighbors[UnityEngine.Random.Range(0, neighbors.Count)];
                    BreakWalls(current, next);
                    maze[next.x, next.y].visited = true;
                    path.Push(next);
                }
            }

            EnsureExitPath();
        }

        List<Vector2Int> GetValidNeighbors(Vector2Int cell)
        {
            List<Vector2Int> neighbors = new List<Vector2Int>();

            if (cell.x > 0 && !maze[cell.x - 1, cell.y].visited)
                neighbors.Add(new Vector2Int(cell.x - 1, cell.y));

            if (cell.x < mazeWidth - 1 && !maze[cell.x + 1, cell.y].visited)
                neighbors.Add(new Vector2Int(cell.x + 1, cell.y));

            if (cell.y > 0 && !maze[cell.x, cell.y - 1].visited)
                neighbors.Add(new Vector2Int(cell.x, cell.y - 1));

            if (cell.y < mazeHeight - 1 && !maze[cell.x, cell.y + 1].visited)
                neighbors.Add(new Vector2Int(cell.x, cell.y + 1));

            return neighbors;
        }

        void BreakWalls(Vector2Int first, Vector2Int second)
        {
            if (first.x < second.x)
            {
                maze[first.x, first.y].rightWall = false;
                maze[second.x, second.y].leftWall = false;
            }
            else if (first.x > second.x)
            {
                maze[first.x, first.y].leftWall = false;
                maze[second.x, second.y].rightWall = false;
            }
            else if (first.y < second.y)
            {
                maze[first.x, first.y].topWall = false;
                maze[second.x, second.y].bottomWall = false;
            }
            else if (first.y > second.y)
            {
                maze[first.x, first.y].bottomWall = false;
                maze[second.x, second.y].topWall = false;
            }
        }

        void SetEntryAndExit()
        {
            int entryY = UnityEngine.Random.Range(0, mazeHeight);
            int exitY = UnityEngine.Random.Range(0, mazeHeight);

            entry = new Vector2Int(0, entryY);
            exit = new Vector2Int(mazeWidth - 1, exitY);

            maze[entry.x, entry.y].leftWall = false;  // Open Entry
            maze[exit.x, exit.y].rightWall = false;   // Open Exit

            Debug.Log($"Entry at {entry}, Exit at {exit}");
        }

        void EnsureExitPath()
        {
            if (maze[exit.x, exit.y].visited) return;

            List<Vector2Int> neighbors = GetValidNeighbors(exit);

            if (neighbors.Count > 0)
            {
                Vector2Int nearest = neighbors[0];

                foreach (var neighbor in neighbors)
                {
                    if (Vector2Int.Distance(exit, neighbor) < Vector2Int.Distance(exit, nearest))
                        nearest = neighbor;
                }

                BreakWalls(exit, new Vector2Int(0, 1));
                maze[exit.x, exit.y].visited = true;
            }
        }
    }

    public class MazeCell
    {
        public bool visited;
        public int x, y;
        public bool topWall = true, bottomWall = true, leftWall = true, rightWall = true;

        public MazeCell(int x, int y)
        {
            this.x = x;
            this.y = y;
            visited = false;
        }
    }
}
