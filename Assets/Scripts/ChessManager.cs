using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChessManager : MonoBehaviour
{
    public GameObject tilePrefab;
    public int projection = 8;
    public GameObject[] whitePiecePrefabs; 
    public GameObject[] blackPiecePrefabs;
    private GameObject[,] tiles = new GameObject[8, 8];

    private List<GameObject> whitePieces = new List<GameObject>();
    private List<GameObject> blackPieces = new List<GameObject>();

    private string turn = "white";
    public bool isPlayerWhite = true;

    private Vector2Int selectedPiece;
    private bool pieceSelected = false;
    public GameObject highlightPrefab;
    private List<GameObject> highlightedTiles = new List<GameObject>();

    private readonly string[,] startBoardWhite = {
        { "r", "n", "b", "q", "k", "b", "n", "r" },
        { "p", "p", "p", "p", "p", "p", "p", "p" },
        { "",  "",  "",  "",  "",  "",  "",  ""  },
        { "",  "",  "",  "",  "",  "",  "",  ""  },
        { "",  "",  "",  "",  "",  "",  "",  ""  },
        { "",  "",  "",  "",  "",  "",  "",  ""  },
        { "P", "P", "P", "P", "P", "P", "P", "P" },
        { "R", "N", "B", "Q", "K", "B", "N", "R" }
    };

    private readonly string[,] startBoardBlack = {
        { "R", "N", "B", "K", "Q", "B", "N", "R" },
        { "P", "P", "P", "P", "P", "P", "P", "P" },
        { "",  "",  "",  "",  "",  "",  "",  ""  },
        { "",  "",  "",  "",  "",  "",  "",  ""  },
        { "",  "",  "",  "",  "",  "",  "",  ""  },
        { "",  "",  "",  "",  "",  "",  "",  ""  },
        { "p", "p", "p", "p", "p", "p", "p", "p" },
        { "r", "n", "b", "k", "q", "b", "n", "r" }
    };


    private Dictionary<string, int> piecePrefabIndex = new Dictionary<string, int>()
    {
        { "P", 0 }, { "R", 1 }, { "N", 2 }, { "B", 3 }, { "Q", 4 }, { "K", 5 }, 
        { "p", 0 }, { "r", 1 }, { "n", 2 }, { "b", 3 }, { "q", 4 }, { "k", 5 }  
    };

    void Start()
    {
        PositionCamera();
        StartCoroutine(InitializeBoard());
    }

    void PositionCamera()
    {
        Camera.main.orthographic = true;
        Camera.main.orthographicSize = projection;

        // Keep the camera in a fixed position (no flipping)
        Camera.main.transform.position = new Vector3(3.5f, 3.5f, -10);
        Camera.main.transform.rotation = Quaternion.identity;

        Debug.Log($"Camera set. Player is White: {isPlayerWhite}");
    }



    void HandlePlayerInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2Int gridPos = new Vector2Int(Mathf.FloorToInt(mousePos.x), Mathf.FloorToInt(mousePos.y));

            Debug.Log($"Mouse Position: {mousePos}, Grid Position: {gridPos}");

            if (!IsValidPosition(gridPos)) 
            {
                Debug.Log("Invalid Position Clicked!");
                return;
            }

            if (!pieceSelected)
            {
                GameObject piece = GetPieceAtPosition(gridPos);
                if (piece != null && piece.tag.StartsWith(turn == "white" ? "White" : "Black"))
                {
                    selectedPiece = gridPos;
                    pieceSelected = true;
                    HighlightValidMoves(selectedPiece);
                    Debug.Log($"Piece Selected at {gridPos}");
                }
            }
            else
            {
                List<Vector2Int> validMoves = GetValidMoves(selectedPiece);
                if (validMoves.Contains(gridPos))
                {
                    MovePiece(selectedPiece, gridPos);
                    turn = turn == "white" ? "black" : "white";
                    Debug.Log($"Piece Moved to {gridPos}");
                }
                else
                {
                    Debug.Log("Invalid Move");
                }

                pieceSelected = false;
                ClearHighlights();
            }
        }
    }

    public void HandleTileClick(Vector2Int gridPos)
    {
        if (isPlayerWhite)
        {
            gridPos = new Vector2Int(gridPos.x, 7 - gridPos.y); // Flip for white player
        }

        if (!IsValidPosition(gridPos)) return;

        if (!pieceSelected)
        {
            GameObject piece = GetPieceAtPosition(gridPos);
            if (piece != null)
            {
                bool isWhitePiece = piece.tag.StartsWith("White");
                bool isPlayerPiece = (isPlayerWhite && isWhitePiece) || (!isPlayerWhite && !isWhitePiece);
                bool isCorrectTurn = (turn == "white" && isWhitePiece) || (turn == "black" && !isWhitePiece);

                // ✅ Ensure the player can only select their own pieces
                if (isPlayerPiece && isCorrectTurn)
                {
                    selectedPiece = gridPos;
                    pieceSelected = true;
                    HighlightValidMoves(selectedPiece);
                }
            }
        }
        else
        {
            List<Vector2Int> validMoves = GetValidMoves(selectedPiece);
            if (validMoves.Contains(gridPos))
            {
                MovePiece(selectedPiece, gridPos);
                turn = turn == "white" ? "black" : "white";
            }

            pieceSelected = false;
            ClearHighlights();
        }
    }

    void MovePiece(Vector2Int from, Vector2Int to)
    {
        Vector2Int realFrom = isPlayerWhite ? new Vector2Int(from.x, 7 - from.y) : from;
        Vector2Int realTo = isPlayerWhite ? new Vector2Int(to.x, 7 - to.y) : to;

        GameObject piece = GetPieceAtPosition(realFrom);
        if (piece == null)
        {
            Debug.LogError($"MovePiece() failed: No piece found at {realFrom}");
            return;
        }

        GameObject targetPiece = GetPieceAtPosition(realTo);
        if (targetPiece != null)
        {
            if (targetPiece.tag.StartsWith(turn == "white" ? "Black" : "White"))
            {
                Destroy(targetPiece);
            }
            else
            {
                Debug.Log("Move blocked by same team piece.");
                return;
            }
        }

        piece.transform.position = new Vector3(realTo.x, realTo.y, 0);

        Debug.Log($"Moved {piece.tag} from {realFrom} to {realTo}");

        turn = turn == "white" ? "black" : "white";
    }

    void HighlightValidMoves(Vector2Int pos)
    {
        List<Vector2Int> validMoves = GetValidMoves(pos);

        if (validMoves.Count == 0)
        {
            Debug.Log($"No valid moves to highlight for {pos}");
            return;
        }

        Debug.Log($"HighlightValidMoves called for {pos}. Valid Moves: {validMoves.Count}");

        foreach (Vector2Int move in validMoves)
        {
            if (!IsValidPosition(move))
            {
                Debug.LogError($"Invalid position detected in validMoves: {move}");
                continue;
            }

            GameObject targetPiece = GetPieceAtPosition(move);
            bool isCapture = targetPiece != null; // Capture if a piece exists

            Debug.Log($"Highlighting tile at {move} - Capture: {isCapture}");

            // Instantiate highlight tile
            GameObject highlight = Instantiate(highlightPrefab, new Vector3(move.x, move.y, -0.5f), Quaternion.identity);
            if (highlight == null)
            {
                Debug.LogError("Highlight Prefab is NULL! Check if it's assigned in the Inspector.");
                return;
            }

            // Adjust highlight color
            SpriteRenderer sr = highlight.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingOrder = 2; // Ensure highlights are above tiles but below pieces
                sr.color = isCapture ? new Color(1f, 0f, 0f, 0.5f) : new Color(0f, 1f, 0f, 0.5f);
            }
            else
            {
                Debug.LogError("Highlight Prefab is missing a SpriteRenderer!");
            }

            highlightedTiles.Add(highlight);
        }
    }



    IEnumerator InitializeBoard()
    {
        CreateBoard();
        yield return new WaitUntil(() => BoardIsReady());
        SpawnPieces();
    }

    void CreateBoard()
    {
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                GameObject tile = Instantiate(tilePrefab, new Vector2(x, y), Quaternion.identity);
                tile.name = $"Tile {x}, {y}";

                // Attach Tile script for input handling
                Tile tileScript = tile.AddComponent<Tile>();
                tileScript.position = new Vector2Int(x, y);

                // Set tile color
                SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = (x + y) % 2 == 0 ? new Color(0.85f, 0.85f, 0.85f) : new Color(0.25f, 0.25f, 0.25f); // Darker white & lighter black
                    sr.sortingOrder = 0; // Ensure tiles are behind pieces and highlights
                }

                tiles[x, y] = tile;
            }
        }
    }

    bool BoardIsReady()
    {
        for (int y = 0; y < 8; y++)
            for (int x = 0; x < 8; x++)
                if (tiles[x, y] == null)
                    return false;
        return true;
    }

    void SpawnPieces()
    {
        // Use correct board setup based on the player color
        string[,] boardToUse = isPlayerWhite ? startBoardWhite : startBoardBlack;

        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                string pieceSymbol = boardToUse[y, x];
                if (!string.IsNullOrEmpty(pieceSymbol))
                {
                    // Adjust piece position so the player's pieces are always at the bottom
                    int adjustedY = isPlayerWhite ? y : 7 - y;
                    int adjustedX = isPlayerWhite ? x : 7 - x;

                    Vector2Int boardPos = new Vector2Int(adjustedX, adjustedY);

                    GameObject piece = CreatePiece(pieceSymbol, boardPos);
                    if (piece != null)
                    {
                        if (char.IsUpper(pieceSymbol[0]))
                            whitePieces.Add(piece);
                        else
                            blackPieces.Add(piece);

                        Debug.Log($"Spawned {pieceSymbol} at {boardPos}, Adjusted for Player Side: {!isPlayerWhite}");
                    }
                }
            }
        }
        
        // ✅ Move White pieces to the bottom and Black pieces to the top if needed
        AdjustPiecePositions();        
    }

    void AdjustPiecePositions()
    {
        if (isPlayerWhite)
        {
            foreach (GameObject piece in whitePieces)
            {
                Vector2Int pos = new Vector2Int(
                    Mathf.RoundToInt(piece.transform.position.x),
                    Mathf.RoundToInt(piece.transform.position.y)
                );

                // ✅ Move White pieces to the bottom
                piece.transform.position = new Vector3(pos.x, 7 - pos.y, 0);
            }

            foreach (GameObject piece in blackPieces)
            {
                Vector2Int pos = new Vector2Int(
                    Mathf.RoundToInt(piece.transform.position.x),
                    Mathf.RoundToInt(piece.transform.position.y)
                );

                // ✅ Move Black pieces to the top
                piece.transform.position = new Vector3(pos.x, 7 - pos.y, 0);
            }
        }

        Debug.Log($"Adjusted piece positions. Player is White: {isPlayerWhite}");
    }


    GameObject CreatePiece(string pieceSymbol, Vector2 position)
    {
        bool isWhite = char.IsUpper(pieceSymbol[0]);
        int prefabIndex = piecePrefabIndex[pieceSymbol];
        GameObject piecePrefab = isWhite ? whitePiecePrefabs[prefabIndex] : blackPiecePrefabs[prefabIndex];

        if (piecePrefab == null) return null;

        Vector2Int gridPos = new Vector2Int(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.y));
        GameObject piece = Instantiate(piecePrefab, new Vector3(gridPos.x, gridPos.y, 0), Quaternion.identity);

        // Rotate the pieces **ONLY when the player is Black**
        if (!isPlayerWhite)
        {
            piece.transform.rotation = Quaternion.Euler(0, 0, 0);
        }

        // Ensure pieces render above tiles
        SpriteRenderer sr = piece.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingOrder = 1;
        }

        Debug.Log($"Created {pieceSymbol} at {gridPos}, Player is White: {isPlayerWhite}");
        return piece;
    }



    void ClearHighlights()
    {
        foreach (GameObject highlight in highlightedTiles)
        {
            Destroy(highlight);
        }
        highlightedTiles.Clear();
    }

    List<Vector2Int> GetValidMoves(Vector2Int pos)
    {
        List<Vector2Int> validMoves = new List<Vector2Int>();
        GameObject piece = GetPieceAtPosition(pos);
        
        if (piece == null)
        {
            Debug.Log($"No piece found at {pos} in GetValidMoves()");
            return validMoves;
        }

        string pieceTag = piece.tag; // ✅ Ensure tag is used
        bool isWhite = pieceTag.StartsWith("White");

        // ✅ Ensure only the player's pieces are selected
        bool isPlayerPiece = (isPlayerWhite && isWhite) || (!isPlayerWhite && !isWhite);
        if (!isPlayerPiece)
        {
            Debug.Log($"Ignoring opponent piece at {pos} with tag {pieceTag}");
            return validMoves;
        }

        Debug.Log($"Finding valid moves for {pieceTag} at {pos}");

        switch (pieceTag)
        {
            case "WhitePawn":
            case "BlackPawn":
                validMoves = GetPawnMoves(pos, isWhite);
                break;
            case "WhiteRook":
            case "BlackRook":
                validMoves = GetRookMoves(pos, isWhite);
                break;
            case "WhiteKnight":
            case "BlackKnight":
                validMoves = GetKnightMoves(pos, isWhite);
                break;
            case "WhiteBishop":
            case "BlackBishop":
                validMoves = GetBishopMoves(pos, isWhite);
                break;
            case "WhiteQueen":
            case "BlackQueen":
                validMoves = GetQueenMoves(pos, isWhite);
                break;
            case "WhiteKing":
            case "BlackKing":
                validMoves = GetKingMoves(pos, isWhite);
                break;
            default:
                Debug.LogError($"Unrecognized piece tag: {pieceTag} at {pos}");
                break;
        }

        if (validMoves.Count == 0)
        {
            Debug.Log($"No valid moves found for {pieceTag} at {pos}");
        }

        return validMoves;
    }

    GameObject GetPieceAtPosition(Vector2Int pos)
    {
        Vector2Int boardPos = isPlayerWhite ? new Vector2Int(pos.x, 7 - pos.y) : pos; // Flip for White player

        foreach (GameObject piece in whitePieces)
        {
            Vector2Int piecePos = new Vector2Int(
                Mathf.RoundToInt(piece.transform.position.x),
                Mathf.RoundToInt(piece.transform.position.y)
            );

            if (piecePos == boardPos)
            {
                Debug.Log($"Found White Piece at {pos} ({piece.tag})");
                return piece;
            }
        }

        foreach (GameObject piece in blackPieces)
        {
            Vector2Int piecePos = new Vector2Int(
                Mathf.RoundToInt(piece.transform.position.x),
                Mathf.RoundToInt(piece.transform.position.y)
            );

            if (piecePos == boardPos)
            {
                Debug.Log($"Found Black Piece at {pos} ({piece.tag})");
                return piece;
            }
        }

        Debug.Log($"No piece found at {pos}");
        return null;
    }


    List<Vector2Int> GetPawnMoves(Vector2Int pos, bool isWhite)
    {
        List<Vector2Int> moves = new List<Vector2Int>();

        int direction = isWhite ? 1 : -1;
        if (isPlayerWhite) direction *= -1; // ✅ Flip for white player

        Vector2Int forward = new Vector2Int(pos.x, pos.y + direction);

        // ✅ Forward move if no piece is blocking
        if (IsValidPosition(forward) && GetPieceAtPosition(forward) == null)
        {
            moves.Add(forward);

            // ✅ Double step only if it's the pawn's first move
            int startingRow = isWhite ? 1 : 6;
            if (pos.y == startingRow)
            {
                Vector2Int doubleStep = new Vector2Int(pos.x, pos.y + (2 * direction));
                if (IsValidPosition(doubleStep) && GetPieceAtPosition(doubleStep) == null)
                {
                    moves.Add(doubleStep);
                }
            }
        }

        // ✅ Capturing diagonally
        Vector2Int[] attackOffsets = { new Vector2Int(1, direction), new Vector2Int(-1, direction) };
        foreach (Vector2Int offset in attackOffsets)
        {
            Vector2Int attackPos = pos + offset;
            GameObject targetPiece = GetPieceAtPosition(attackPos);

            if (IsValidPosition(attackPos) && targetPiece != null)
            {
                bool isEnemy = isWhite ? targetPiece.tag.StartsWith("Black") : targetPiece.tag.StartsWith("White");
                if (isEnemy)
                {
                    moves.Add(attackPos);
                }
            }
        }

        return moves;
    }

    List<Vector2Int> GetRookMoves(Vector2Int pos, bool isWhite)
    {
        return GetSlidingMoves(pos, isWhite, new Vector2Int[] { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) });
    }

    List<Vector2Int> GetBishopMoves(Vector2Int pos, bool isWhite)
    {
        return GetSlidingMoves(pos, isWhite, new Vector2Int[] { new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1) });
    }

    List<Vector2Int> GetQueenMoves(Vector2Int pos, bool isWhite)
    {
        List<Vector2Int> moves = GetRookMoves(pos, isWhite);
        moves.AddRange(GetBishopMoves(pos, isWhite));
        return moves;
    }

    List<Vector2Int> GetKingMoves(Vector2Int pos, bool isWhite)
    {
        List<Vector2Int> moves = new List<Vector2Int>();
        Vector2Int[] directions = {
            new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
            new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1)
        };
        
        foreach (Vector2Int dir in directions)
        {
            Vector2Int newPos = pos + dir;
            if (IsValidPosition(newPos))
                moves.Add(newPos);
        }
        return moves;
    }

    List<Vector2Int> GetKnightMoves(Vector2Int pos, bool isWhite)
    {
        List<Vector2Int> moves = new List<Vector2Int>();
        Vector2Int[] knightMoves = {
            new Vector2Int(2, 1), new Vector2Int(2, -1), new Vector2Int(-2, 1), new Vector2Int(-2, -1),
            new Vector2Int(1, 2), new Vector2Int(1, -2), new Vector2Int(-1, 2), new Vector2Int(-1, -2)
        };

        foreach (Vector2Int move in knightMoves)
        {
            Vector2Int newPos = pos + move;
            if (IsValidPosition(newPos))
                moves.Add(newPos);
        }
        return moves;
    }

    List<Vector2Int> GetSlidingMoves(Vector2Int pos, bool isWhite, Vector2Int[] directions)
    {
        List<Vector2Int> moves = new List<Vector2Int>();
        
        foreach (Vector2Int dir in directions)
        {
            Vector2Int newPos = pos + dir;
            while (IsValidPosition(newPos) && GetPieceAtPosition(newPos) == null)
            {
                moves.Add(newPos);
                newPos += dir;
            }
        }
        return moves;
    }

    bool IsValidPosition(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < 8 && pos.y >= 0 && pos.y < 8;
    }
}
