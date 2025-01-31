using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ChessManager : MonoBehaviour
{
    public static ChessManager Instance { get; private set; } // Singleton instance

    public GameObject tilePrefab;
    public int projection = 8;
    public GameObject[] whitePiecePrefabs; 
    public GameObject[] blackPiecePrefabs;
    private GameObject[,] tiles = new GameObject[8, 8];

    private List<GameObject> whitePieces = new List<GameObject>();
    private List<GameObject> blackPieces = new List<GameObject>();

    public string turn = "white";
    public bool isPlayerWhite = true;

    public Vector2Int selectedPiece;
    public bool pieceSelected = false;
    public GameObject highlightPrefab;
    private List<GameObject> highlightedTiles = new List<GameObject>();
    public bool isWhiteTurn = true; // True if it's White's turn, false if Black
    private Dictionary<Vector2Int, GameObject> boardPieces = new Dictionary<Vector2Int, GameObject>();


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

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("⚠️ Another instance of ChessManager found! Destroying duplicate.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject); // Ensure the instance persists across scenes
        }

        // Example of a game state variable

    public void ToggleTurn()
    {
        isWhiteTurn = !isWhiteTurn;
        Debug.Log("🔄 Turn switched. Now it's " + (isWhiteTurn ? "White" : "Black") + "'s turn.");
    }

    void Start()
    {
        PositionCamera();
        StartCoroutine(InitializeBoard());
    }

    public void SelectPieceAt(Vector2Int position)
    {
        GameObject piece = GetPieceAtPosition(position);

        if (piece != null)
        {
            string pieceTag = piece.tag; // Get piece tag (White or Black)
            bool isWhitePiece = pieceTag.Contains("White");

            // 🔹 Get isWhiteTurn from ChessManager instance
            bool isWhiteTurn = FindFirstObjectByType<ChessManager>().isWhiteTurn;

            if (isWhitePiece != isWhiteTurn)
            {
                Debug.Log($"⛔ Cannot select {pieceTag} at {position}, it's not your turn!");
                return; // Prevent selecting opponent's piece
            }

            selectedPiece = position;
            pieceSelected = true;
            Debug.Log($"✅ Selected {piece.tag} at {position}");
        }
        else
        {
            Debug.Log("❌ No piece found at this tile");
        }
    }


    public void EndTurn()
    {
        ToggleTurn();
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

    public void HandleTileClick(Vector2Int gridPos)
    {
        Debug.Log($"🟢 Tile Clicked at {gridPos}");

        // Adjust position based on player's perspective
        Vector2Int adjustedPos = isPlayerWhite ? new Vector2Int(gridPos.x, 7 - gridPos.y) : gridPos;
        Debug.Log($"🔄 Adjusted for White's view: {adjustedPos}");

        if (!IsValidPosition(adjustedPos))
        {
            Debug.Log("❌ Clicked outside the board!");
            return;
        }

        GameObject clickedPiece = GetPieceAtPosition(adjustedPos);

        if (!pieceSelected)
        {
            if (clickedPiece != null) 
            {
                string pieceTag = clickedPiece.tag;

                // ✅ Use full piece name instead of "StartsWith"
                bool isWhitePiece = pieceTag == "WhitePawn" || pieceTag == "WhiteRook" || 
                                    pieceTag == "WhiteKnight" || pieceTag == "WhiteBishop" || 
                                    pieceTag == "WhiteQueen" || pieceTag == "WhiteKing";

                bool isBlackPiece = pieceTag == "BlackPawn" || pieceTag == "BlackRook" || 
                                    pieceTag == "BlackKnight" || pieceTag == "BlackBishop" || 
                                    pieceTag == "BlackQueen" || pieceTag == "BlackKing";

                bool isPlayerPiece = (isPlayerWhite && isWhitePiece) || (!isPlayerWhite && isBlackPiece);
                bool isCorrectTurn = (isWhiteTurn && isWhitePiece) || (!isWhiteTurn && isBlackPiece);

                Debug.Log($"🔎 Found piece: {pieceTag} at {adjustedPos} - Player's Piece? {isPlayerPiece} - Correct Turn? {isCorrectTurn}");

                if (isPlayerPiece && isCorrectTurn)
                {
                    selectedPiece = adjustedPos;
                    pieceSelected = true;
                    Debug.Log($"✅ Selected {pieceTag} at {adjustedPos}");

                    HighlightValidMoves(selectedPiece);
                }
                else
                {
                    Debug.Log("⛔ You cannot select this piece. It's either not yours or not your turn.");
                }
            }
            else
            {
                Debug.Log("❌ No piece found at this tile.");
            }
        }
        else
        {
            if (IsHighlightedTile(adjustedPos))
            {
                MovePiece(selectedPiece, adjustedPos);
                pieceSelected = false;
                ClearHighlights();
            }
            else
            {
                Debug.Log("⛔ Invalid move! The clicked tile is not a valid move.");
            }
        }
    }


    void MovePiece(Vector2Int from, Vector2Int to)
    {
        if (!boardPieces.ContainsKey(from))
        {
            Debug.LogError($"❌ MovePiece() failed: No piece found at {from}");
            return;
        }

        GameObject piece = boardPieces[from];

        // ✅ Check if the destination has an enemy piece (for capturing)
        if (boardPieces.ContainsKey(to))
        {
            GameObject capturedPiece = boardPieces[to];

            if (capturedPiece.tag.Contains("White"))
                whitePieces.Remove(capturedPiece);
            else
                blackPieces.Remove(capturedPiece);

            Destroy(capturedPiece);
            Debug.Log($"🔥 Captured {capturedPiece.tag} at {to}");
        }

        // ✅ Move the piece in Unity & update board state
        piece.transform.position = new Vector3(to.x, to.y, 0);
        boardPieces.Remove(from);
        boardPieces[to] = piece;

        Debug.Log($"✅ Moved {piece.tag} from {from} to {to}");

        // ✅ Switch turn after moving
        ToggleTurn();
    }
    private bool IsHighlightedTile(Vector2Int position)
    {
        foreach (GameObject highlight in highlightedTiles)
        {
            if (highlight.transform.position.x == position.x && highlight.transform.position.y == position.y)
            {
                return true;
            }
        }
        return false;
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
        // Determine board setup based on player's perspective
        string[,] boardToUse = isPlayerWhite ? startBoardWhite : startBoardBlack;

        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                // Reverse board layout when playing as Black
                int boardY = isPlayerWhite ? y : 7 - y;
                string pieceSymbol = boardToUse[boardY, x];

                if (!string.IsNullOrEmpty(pieceSymbol))
                {
                    // ✅ Adjust visual positioning to match player perspective
                    int adjustedX = isPlayerWhite ? x : 7 - x;
                    int adjustedY = isPlayerWhite ? y : 7 - y;
                    Vector2Int boardPos = new Vector2Int(adjustedX, adjustedY);

                    // ✅ Create piece at the correct board position
                    GameObject piece = CreatePiece(pieceSymbol, boardPos);

                    if (piece != null)
                    {
                        // Add piece to the correct list
                        if (char.IsUpper(pieceSymbol[0])) 
                            whitePieces.Add(piece);
                        else
                            blackPieces.Add(piece);

                        Debug.Log($"✅ Spawned {pieceSymbol} at {boardPos}, Assigned Tag: {piece.tag}, Player is White: {isPlayerWhite}");
                    }
                }
            }
        }
        // Ensure pieces are adjusted
        AdjustPiecePositions();
    }

    void AdjustPiecePositions()
    {
        Dictionary<Vector2Int, GameObject> updatedPositions = new Dictionary<Vector2Int, GameObject>();

        foreach (GameObject piece in whitePieces.Concat(blackPieces)) // Process all pieces together
        {
            Vector2Int oldPos = new Vector2Int(
                Mathf.RoundToInt(piece.transform.position.x),
                Mathf.RoundToInt(piece.transform.position.y)
            );

            // Flip Y-coordinate for correct player perspective
            Vector2Int newPos = isPlayerWhite 
                ? new Vector2Int(oldPos.x, 7 - oldPos.y) // Flip for White player
                : oldPos; // Keep unchanged for Black player

            // ✅ Update both the actual transform and `boardPieces` dictionary
            piece.transform.position = new Vector3(newPos.x, newPos.y, 0);
            updatedPositions[newPos] = piece;
        }

        // ✅ Update boardPieces AFTER transforming all pieces
        boardPieces = updatedPositions;

        Debug.Log($"✅ Adjusted piece positions & updated board. Player is White: {isPlayerWhite}");
    }


    GameObject CreatePiece(string pieceSymbol, Vector2 position)
    {
        bool isWhite = char.IsUpper(pieceSymbol[0]);
        int prefabIndex = piecePrefabIndex[pieceSymbol];
        GameObject piecePrefab = isWhite ? whitePiecePrefabs[prefabIndex] : blackPiecePrefabs[prefabIndex];

        if (piecePrefab == null) return null;

        Vector2Int gridPos = new Vector2Int(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.y));
        GameObject piece = Instantiate(piecePrefab, new Vector3(gridPos.x, gridPos.y, 0), Quaternion.identity);

        // ✅ Use full name for proper tag detection
        string tag = isWhite ? 
            (pieceSymbol == "P" ? "WhitePawn" : 
            pieceSymbol == "R" ? "WhiteRook" : 
            pieceSymbol == "N" ? "WhiteKnight" : 
            pieceSymbol == "B" ? "WhiteBishop" : 
            pieceSymbol == "Q" ? "WhiteQueen" : "WhiteKing") 
        :
            (pieceSymbol == "p" ? "BlackPawn" : 
            pieceSymbol == "r" ? "BlackRook" : 
            pieceSymbol == "n" ? "BlackKnight" : 
            pieceSymbol == "b" ? "BlackBishop" : 
            pieceSymbol == "q" ? "BlackQueen" : "BlackKing");

        piece.tag = tag;

        Debug.Log($"🛠 Created {pieceSymbol} at {gridPos}, Assigned Tag: {piece.tag}, Player is White: {isPlayerWhite}");

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
            Debug.Log($"❌ No piece found at {pos} in GetValidMoves()");
            return validMoves;
        }

        string pieceTag = piece.tag;
        
        Debug.Log($"🔎 Finding valid moves for {pieceTag} at {pos}");

        switch (pieceTag)
        {
            case "WhitePawn":
            case "BlackPawn":
                validMoves = GetPawnMoves(pos, pieceTag == "WhitePawn");
                break;
            case "WhiteRook":
            case "BlackRook":
                validMoves = GetRookMoves(pos, pieceTag == "WhiteRook");
                break;
            case "WhiteKnight":
            case "BlackKnight":
                validMoves = GetKnightMoves(pos, pieceTag == "WhiteKnight");
                break;
            case "WhiteBishop":
            case "BlackBishop":
                validMoves = GetBishopMoves(pos, pieceTag == "WhiteBishop");
                break;
            case "WhiteQueen":
            case "BlackQueen":
                validMoves = GetQueenMoves(pos, pieceTag == "WhiteQueen");
                break;
            case "WhiteKing":
            case "BlackKing":
                validMoves = GetKingMoves(pos, pieceTag == "WhiteKing");
                break;
            default:
                Debug.LogError($"❌ Unrecognized piece tag: {pieceTag} at {pos}");
                break;
        }

    Debug.Log($"✅ Found {validMoves.Count} valid moves for {pieceTag} at {pos}");
    return validMoves;
}


    GameObject GetPieceAtPosition(Vector2Int pos)
    {
        if (boardPieces.TryGetValue(pos, out GameObject piece))
        {
            Debug.Log($"✅ Found piece at {pos} | Name: {piece.name} | Tag: {piece.tag}");
            return piece;
        }

        Debug.Log($"❌ No piece found at {pos}");
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
