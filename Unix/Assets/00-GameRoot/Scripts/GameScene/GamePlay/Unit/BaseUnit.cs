using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseUnit : MonoBehaviour
{
    public int maxHealth;
    int _health;
    public char characterCode;

    #region Unit setup
    public Color teamColor = Color.clear;

    Tile _originalTile = null;

    public Vector3 rotation;


    public virtual void Setup(Color TeamColor, Color32 unitColor, char CharacterCode)
    {
        characterCode = CharacterCode;
        teamColor = TeamColor;

        int layer = teamColor == Color.red ? 3:6;
        gameObject.layer = layer;
        GetComponent<Renderer>().material.color = unitColor;

        _health = maxHealth;

        rotation = transform.rotation.eulerAngles;

    }

  
    #endregion

    #region Unit Movement
    //Movement Variables
    public Vector3Int movement = Vector3Int.one;
    public List<Tile> highlightedTiles = new List<Tile>();
    public Tile currentTile = null;

    Tile _targetTile = null;
    public bool selected;
    void CreateTilePath(int xDirection, int yDirection, int movement)
    {
        //TargetPosition
        int currentX = currentTile.boardPosition.x;
        int currentY = currentTile.boardPosition.z; // z represents the world point, but it also represents the y point in the 2D array. 

        //Check each tile
        for (int i = 1; i <= movement; i++)
        {
            currentX += xDirection;
            currentY += yDirection;

            TileState tileState = TileState.None;
            tileState = currentTile.board.ValidateTile(currentX, currentY, this);

            if (tileState != TileState.Free) //if the tile is out of bounds or has a friendly on it, then break the look and don't add anything to the available target tiles.
                break;

            highlightedTiles.Add(currentTile.board.allTiles[currentX, currentY]); //add the tile if it is free

        }
    }

    protected virtual void CheckPath()
    {
        //horizantal 
        CreateTilePath(1, 0, movement.x);
        CreateTilePath(-1, 0, movement.x);

        //horizantal 
        CreateTilePath(0, 1, movement.z);
        CreateTilePath(0, -1, movement.z);

        //horizantal 
        CreateTilePath(1, 1, movement.y);
        CreateTilePath(-1, 1, movement.y);

        //horizantal 
        CreateTilePath(-1, -1, movement.y);
        CreateTilePath(1, -1, movement.y);
    }

    public void ShowHighlightedTiles()
    {
        foreach (Tile tile in highlightedTiles)
            tile.HighLight();
            
    }

    public void HideHighlightedTiles()
    {
        foreach (Tile tile in highlightedTiles)
            tile.StopHighLight();

        highlightedTiles.Clear();
    }
    public void Place(Tile newTile)
    {
        //tile
        currentTile = newTile;
        _originalTile = newTile;

        currentTile.currentUnit = this;

        //object
        transform.position = newTile.transform.position;
        gameObject.SetActive(true);
    }
    public void Reset() //Resets peices, keeps board the same
    {
        gameObject.SetActive(true);
        Place(_originalTile);
    }

    protected virtual void Move()
    {
        //clear the current tile
        currentTile.currentUnit = null;

        //change current tile
        currentTile = _targetTile;
        currentTile.currentUnit = this;

        //Move on board
        transform.position = currentTile.transform.position;
        _targetTile = null;
    }

    #region Mouse events
    public void OnMouseOver()
    {
        if (gameObject.CompareTag("Interactive"))
        {
            // Test for cells
            CheckPath();
            ShowHighlightedTiles(); //show highlighted tiles
        }   
    }

    private void OnMouseExit()
    {
       
        if (_currentState != hoverState) //don't clear the highlighted tiles while the player is draging a unit
        {
            HideHighlightedTiles();
        }
    }

    public void Clicked()
    {       
        TransitionToState(hoverState);
    }

    public void Drag()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit)) // did the ray hit anything?
        {
            transform.position = new Vector3(hit.point.x, transform.position.y, hit.point.z);// follow the ray(the cursor)

            foreach (Tile tile in highlightedTiles)
            {
                if (hit.transform.gameObject.GetComponent<Tile>() == tile) // is the cursor over one of he highlighted tiles?
                {
                    _targetTile = tile;//make that tile the target tile 
                    break;// only one can be the target 
                }

                _targetTile = null;
            }

            if (Input.GetMouseButtonDown(0) && _targetTile != null) // right button clicked
            {

                Move(); // go to target location;
                TransitionToState(idleState);
                UnitManager.Static_SwitchSides(teamColor);
            }

            if (Input.GetMouseButtonDown(1)) // right button clicked
            {
                TransitionToState(idleState);
            }
        }
    }

    #endregion

    #endregion

    #region States machine

    UnitBaseState _currentState;
    public UnitBaseState currentState { get { return _currentState; } }


    public readonly UnitAttackState attackState = new UnitAttackState(); // attack enemies
    public readonly UnitHoverState hoverState = new UnitHoverState();  // hover over enemies
    public readonly UnitIdleState idleState = new UnitIdleState(); // scan for enemies

    void Start()
    {
        TransitionToState(idleState);
    }
    private void Update()
    {
        currentState.Update(this);
    }
    public void TransitionToState(UnitBaseState state)
    {
        _currentState = state;
        _currentState.EnterState(this);
    }


    #endregion

    #region Attack
    public BaseUnit target;
    public Vector3 targetPos;
    public float coolDown;
    public virtual void CheckForEnemies() { }
    public virtual void Attack() { }
    public virtual bool CheckAttackValidity() {
        if (!target.gameObject.activeSelf) //if gameObject has been set to deactive
            return false;

        if (target.transform.position == targetPos)
            return true;

        return false;
    }

    #endregion
    public IEnumerator TakeDamage(int damage)
    {
        _health -= damage;
        gameObject.GetComponent<Renderer>().material.EnableKeyword("_EMISSION");
        if (_health <= 0)
        {
            gameObject.SetActive(false);
            UnitManager.Static_UnitDeath(teamColor);
        }
        yield return new WaitForSeconds(.5f);
        gameObject.GetComponent<Renderer>().material.DisableKeyword("_EMISSION");

    }
   
    
}
