using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;
using TMPro;

/// <summary>
/// An abstract maze generator class. Inherit this class and make a own GenerateMaze() & GenerateMazeCoroutine().
/// </summary>
public abstract class MazeGenerator : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] protected int width;
    [SerializeField] protected int height;
    [SerializeField] private bool generateInstantly = true;
    [SerializeField] protected float waitTime = 0.1f;
    
    [Header("Tilemap")]
    [SerializeField] protected Tilemap tilemap;
    [SerializeField] protected TileBase whiteTile;
    [SerializeField] protected TileBase redTile;
    [SerializeField] protected TileBase greenTile;

    [Header("UI")]
    [SerializeField] private TMP_InputField inputWidth;
    [SerializeField] private TMP_InputField inputHeight;
    [SerializeField] private TMP_InputField inputWaitTime;

    [Header("Sound's")]
    [SerializeField] protected AudioSource tileChangeSound;
    [SerializeField] protected AudioSource mazeDoneSound;

    [Header("Other generators")]
    [SerializeField] private CurrentGenerator currentGenerator;
    [SerializeField] private MazeGenerator otherGenerator;
    
    [Space]
    [SerializeField] private UnityEvent onGenerate = new UnityEvent();
    
    protected bool[,] VisitedCells; // To track visited cells
    protected Stack<Vector3Int> CellStack; // Stack to track visited cells

    private readonly Vector3Int[] _directions = 
    {
        new (0, 2, 0), // Up
        new (2, 0, 0), // Right
        new (0, -2, 0), // Down
        new (-2, 0, 0) // Left
    };

    #region Lifetime functions

    private void Start()
    {
        inputWidth.onValueChanged.AddListener(SetWidth);
        inputHeight.onValueChanged.AddListener(SetHeight);
        inputWaitTime.onValueChanged.AddListener(SetWaitTime);
    }

    #endregion

    #region public functions

    /// <summary>
    /// Start generating or regenerate the maze.
    /// </summary>
    public void StartGenerating()
    {
        // When regenerating, it clears the previous maze
        tilemap.ClearAllTiles();

        // Stop the current maze generation if it is in progress
        if (currentGenerator.CurrentGeneration != null)
        {
            otherGenerator.StopAllCoroutines();
            StopAllCoroutines();
            currentGenerator.CurrentGeneration = null;
        }

        if (generateInstantly) GenerateMaze();
        else currentGenerator.CurrentGeneration = StartCoroutine(GenerateMazeCoroutine());

        onGenerate?.Invoke();
    }

    #endregion

    #region protected functions

    /// <summary>
    /// Generates the maze.
    /// </summary>
    protected abstract void GenerateMaze();

    /// <summary>
    /// Generates the maze with some delay in between visiting the tiles.
    /// </summary>
    protected abstract IEnumerator GenerateMazeCoroutine(bool isSlowly = true);
    
    /// <summary>
    /// Creates a blank maze for the generation.
    /// </summary>
    protected void SetupMaze()
    {
        width = width % 2 == 0 ? width + 1 : width;
        height = height % 2 == 0 ? height + 1 : height;

        VisitedCells = new bool[width, height]; // Initialize the VisitedCells array
        
        for (int x = -1; x < width + 1; x++)
        {
            for (int y = -1; y < height + 1; y++)
            {
                var position = new Vector3Int(x, y, 0);

                if (x == -1 || y == -1 || x == width || y == height)
                {
                    // Set walls for the border cells
                    tilemap.SetTile(position, whiteTile);
                }
                else if (x % 2 == 0 || y % 2 == 0)
                {
                    // Set walls for even-indexed cells
                    tilemap.SetTile(position, whiteTile);
                }
                else
                {
                    // Set empty space for odd-indexed cells
                    tilemap.SetTile(position, redTile);
                    VisitedCells[x, y] = false;
                }
            }
        }
    }

    /// <summary>
    /// Creates the entrance and exit for the maze.
    /// </summary>
    protected void CreateEntranceAndExit()
    {
        var entrancePosition = FindRandomEmptyTile();
        var exitPosition = FindRandomEmptyTile();

        // Ensure entrance and exit positions are valid
        while (!IsValidEntranceExitPosition(entrancePosition)) entrancePosition = FindRandomEmptyTile();
        while (!IsValidEntranceExitPosition(exitPosition)) exitPosition = FindRandomEmptyTile();
        
        // Adjust entrance position to be at the left edge and exit position to be at the right edge
        entrancePosition.x = -1;
        exitPosition.x = width;

        // Set tiles for entrance and exit
        tilemap.SetTile(entrancePosition, greenTile);
        tilemap.SetTile(exitPosition, greenTile);

        // Make the second tile for entrance and exit, because outer wall is 2 tiles thick
        var entrancePosition2 = new Vector3Int(0, entrancePosition.y, 0);
        var exitPosition2 = new Vector3Int(width - 1, exitPosition.y, 0);
        tilemap.SetTile(entrancePosition2, greenTile);
        tilemap.SetTile(exitPosition2, greenTile);
        
        mazeDoneSound.Play();
    }

    /// <summary>
    /// Finds a starting position for the generation. And sets its self.
    /// </summary>
    /// <returns>The chosen starting position.</returns>
    protected Vector3Int FindAndSetStartPosition()
    {
        var availableCells = new List<Vector3Int>();

        // Adds every red tile
        for (int x = 1; x < width; x += 2)
        {
            for (int y = 1; y < height; y += 2)
            {
                availableCells.Add(new Vector3Int(x, y, 0));
            }
        }

        if (availableCells.Count != 0) return GetRandomTile(availableCells);
        
        Debug.LogError("No available cells for starting position.");
        return Vector3Int.zero;
    }
    
    /// <summary>
    /// Checks if every tile is visited with the generation.
    /// </summary>
    /// <returns>If every tile is visited.</returns>
    protected bool AllTilesVisited()
    {
        for (int x = 1; x < width; x += 2)
        {
            for (int y = 1; y < height; y += 2)
            {
                if (!VisitedCells[x, y]) return false; // At least one tile is unvisited
            }
        }

        return true; // All tiles are visited
    }
    
    /// <summary>
    /// Changes the tile in between currentTile and neighborTile to a color. And the currentTile gets changed.
    /// </summary>
    /// <param name="currentTile">The current tile, that gets also changed</param>
    /// <param name="neighborTile">The neighbor to look for the tile in between.</param>
    /// <param name="isPrims">Idk</param>
    protected void UpdateTileColor(Vector3Int currentTile, Vector3Int neighborTile = default)
    {
        // Making the wallTile green
        if (neighborTile != default)
        {
            var wallTilePosition = currentTile + (neighborTile - currentTile) / 2;
            tilemap.SetTile(wallTilePosition, greenTile);
        }

        // Update the tiles for the path
        tilemap.SetTile(currentTile, greenTile);
        tilemap.SetTile(neighborTile, greenTile);
    }
    
    protected List<Vector3Int> GetUnvisitedNeighbors(Vector3Int cell) => _directions.Select(direction => cell + direction).Where(neighbor => IsInsideMaze(neighbor) && !VisitedCells[neighbor.x, neighbor.y]).ToList();
    
    #endregion

    #region private functions
    
    /// <summary>
    /// Checks if the tile is inside the maze.
    /// </summary>
    /// <param name="tile">The tile to check for.</param>
    /// <returns>If it's in the maze or not.</returns>
    private bool IsInsideMaze(Vector3Int tile) => tile.x >= 0 && tile.x < width && tile.y >= 0 && tile.y < height;
    
    /// <summary>
    /// Looking at the neighbors of the entrance and exit if it's a valid position. Prevents spawning next to a wall.
    /// </summary>
    /// <param name="position">The preferred position.</param>
    /// <returns>If it's valid position to spawn.</returns>
    private bool IsValidEntranceExitPosition(Vector3Int position) => _directions.Select(direction => position + direction).Any(adjacentPosition => IsInsideMaze(adjacentPosition) && tilemap.GetTile<TileBase>(adjacentPosition) == greenTile);
    
    /// <summary>
    /// Find a random tile that is empty in the tile map.
    /// </summary>
    /// <returns>The chosen random tile.</returns>
    private Vector3Int FindRandomEmptyTile()
    {
        var emptyCells = new List<Vector3Int>();

        // Adds every green tile (empty tile)
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (tilemap.GetTile<TileBase>(new Vector3Int(x, y, 0)) == greenTile) emptyCells.Add(new Vector3Int(x, y, 0));
            }
        }

        if (emptyCells.Count != 0) return GetRandomTile(emptyCells);
        
        Debug.LogError("No empty cells found.");
        return Vector3Int.zero;
    }

    /// <summary>
    /// Get a random tile in the tilemap.
    /// </summary>
    /// <param name="tiles">The list of given tile to select a random tile form.</param>
    /// <returns>The chosen tile.</returns>
    private static Vector3Int GetRandomTile(IReadOnlyList<Vector3Int> tiles)
    {
        var randomIndex = Random.Range(0, tiles.Count);
        var startPosition = tiles[randomIndex];

        return startPosition;
    }

    #endregion
    
    #region UI functions

    public void ToggleInstantlyGeneration() => generateInstantly = !generateInstantly;
    
    public void ToggleMute(bool value)
    {
        tileChangeSound.mute = value;
        mazeDoneSound.mute = value;
    }
    
    private void SetHeight(string input)
    {
        if(currentGenerator.CurrentGeneration != null) StopCoroutine(currentGenerator.CurrentGeneration);
        int.TryParse(input, out height);
    }

    private void SetWidth(string input)
    {
        if(currentGenerator.CurrentGeneration != null) StopCoroutine(currentGenerator.CurrentGeneration);
        int.TryParse(input, out width);
    }

    private void SetWaitTime(string input) => float.TryParse(input, out waitTime);

    #endregion
}
