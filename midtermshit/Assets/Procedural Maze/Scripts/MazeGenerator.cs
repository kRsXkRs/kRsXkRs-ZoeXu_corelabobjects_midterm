using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ProceduralMaze
{
    public class MazeGenerator : MonoBehaviour
    {
        public static MazeGenerator Instance;

        [SerializeField] MazeController mazeController;
        [SerializeField] GameObject mazeCellPrefab;

        public float cellSize = 1.0f;
        public int numberOfProps = 10;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            GenerateMaze();
        }

        [ContextMenu("GENERATE MAZE")]
        public void GenerateMaze()
        {
            MazeCell[,] maze = mazeController.GetMaze();
            GenerateMaze(maze);
        }

        private void GenerateMaze(MazeCell[,] maze)
        {
            for (int x = 0; x < mazeController.mazeWidth; x++)
            {
                for (int y = 0; y < mazeController.mazeHeight; y++)
                {
                    GameObject newCell = Instantiate(mazeCellPrefab, new Vector3((float)x * cellSize, 0f, (float)y * cellSize), Quaternion.identity, transform);

                    MazeCellObject mazeCell = newCell.GetComponent<MazeCellObject>();

                    bool top = maze[x, y].topWall;
                    bool left = maze[x, y].leftWall;
                    bool right = x == mazeController.mazeWidth - 1;

                    if (mazeController.exit.x == x && mazeController.exit.y == y)
                        right = false;

                    bool bottom = y == 0;

                    mazeCell.Init(top, bottom, left, right);
                }
            }
        }
    }

}
