using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private GridManager gridManager = null;

    private Vector3 playerOffset;
    private const int MAX_MOVES = 6;

    public int movesLeft { get; private set; }
    public Vector3 currentPosition { get; private set; }
    public Vector3Int currentGridPosition { get; private set; }
    public bool isMoving { get; private set; }

    public UnityAction<Vector3Int> PlayerPositionUpdated;

    public void Awake()
    {
        playerOffset = new Vector3(0.5f, 0.5f, 0);
        movesLeft = MAX_MOVES;
        currentPosition = transform.position;
    }

    private void Start()
    {
        // get the current grid position for the players
        currentGridPosition = gridManager.ClickToGridPosition(new Vector2(currentPosition.x, currentPosition.y));
    }

    public void Update()
    {
        currentPosition = transform.position;
        currentGridPosition = gridManager.ClickToGridPosition(new Vector2(currentPosition.x, currentPosition.y));
    }

    public void Move(Vector3 newLocation, List<Vector3Int> path)
    {
        StartCoroutine(MoveCoroutine(this.transform, path, EndMove));
    }

    private void EndMove()
    {
        if(movesLeft <= 0)
        {
            Debug.LogError("Setting moves to max");
            movesLeft = MAX_MOVES;
        }
        isMoving = false;
        StopCoroutine("MoveCoroutine");
    }

    private IEnumerator MoveCoroutine(Transform playerTransform, List<Vector3Int> path, UnityAction callBack)
    {
        isMoving = true;
        foreach (var node in path)
        {
            if(movesLeft > 0)
            {
                playerTransform.position = node + playerOffset;
                movesLeft--;
                var gridPosition = gridManager.ClickToGridPosition(new Vector2(currentPosition.x, currentPosition.y));
                PlayerPositionUpdated?.Invoke(node);
            }
            yield return new WaitForSeconds(0.2f);
        }

        callBack.Invoke();
    }
}
