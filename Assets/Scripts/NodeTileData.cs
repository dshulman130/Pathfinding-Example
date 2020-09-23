using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// This class is used to store data about the current node for use in the A* algorithm
/// </summary>
[CreateAssetMenu]
public class NodeTileData
{
    // Since this is a UnityEngine Object we can't have a custom constructor as ScriptableObjects aren't
    // created with "new", they're instantiated by the UnityEngine with CreateInstance()
    public void Init(Vector3Int gridPosition = default, int gCost = 0, int hCost = 0, bool isWalkable = false)
    {
        cumulativeCost = gCost;
        estimatedCostToTarget = hCost;
        this.Position = gridPosition;
        this.IsWalkable = isWalkable;
        ParentNode = this;
    }

    #region Public Members
    public TileBase[] tiles;                    // List of tiles that represent the number of moves to get to this node (1 to 6)

    // I've chosen to make these public as they need to be changed often externally
    public int cumulativeCost = 0;              // This is the "G-Cost"
    public int estimatedCostToTarget = 0;       // This is the "H-Cost"
    #endregion

    #region Properties
    public int Score { get => cumulativeCost + estimatedCostToTarget; } // This is the "F-Cost"

    public Vector3Int Position { get; private set; }                    // Public accessor for gridPosition since we only want it set during construction

    public bool IsWalkable { get; private set; }                        // Represents whether there is an obstacle at this location in the Obstacle Tilemap

    public NodeTileData ParentNode { get; set; }                        // the parent node of this node in the path
    #endregion
}
