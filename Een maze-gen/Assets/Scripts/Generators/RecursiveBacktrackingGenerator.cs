using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Recursive Backtracking Algorithm (Depth-First Search). Inheritance from MazeGenerator.
/// </summary>
public sealed class RecursiveBacktrackingGenerator : MazeGenerator
{
    #region protected functions
    
    protected override void GenerateMaze() => StartCoroutine(GenerateMazeCoroutine(false));

    protected override IEnumerator GenerateMazeCoroutine(bool isSlowly = true)
    {
        VisitedCells = new bool[width, height]; // Initialize the visitedCells array
        CellStack = new Stack<Vector3Int>(); // Initialize the cell stack

        SetupMaze();

        var startPosition = FindAndSetStartPosition();
        CellStack.Push(startPosition);
        // UpdateTileColor(startPosition);
        
        // Update the starting tile color before entering the main loop
        yield return new WaitForSeconds(waitTime);
        UpdateTileColor(startPosition);

        // Loop until all cells have been in the CellStack
        while (CellStack.Count > 0)
        {
            if (isSlowly) 
            {
                yield return new WaitForSeconds(waitTime); // Pause execution and resume
                tileChangeSound.Play();
            }

            var currentCell = CellStack.Peek();
            VisitedCells[currentCell.x, currentCell.y] = true;

            var unvisitedNeighbors = GetUnvisitedNeighbors(currentCell);

            if (unvisitedNeighbors.Count > 0)
            {
                var randomNeighbor = unvisitedNeighbors[Random.Range(0, unvisitedNeighbors.Count)];
                UpdateTileColor(currentCell, randomNeighbor);
                CellStack.Push(randomNeighbor);
            }
            else
            {
                // Mark the current cell as visited after backtracking
                UpdateTileColor(currentCell);
                CellStack.Pop();
            }

            // Stop the maze generation process
            if (AllTilesVisited()) break;
        }

        CreateEntranceAndExit();
    }
    
    #endregion
}
