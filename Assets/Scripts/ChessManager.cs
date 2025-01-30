using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChessManager : MonoBehaviour
{
    public GameObject tilePrefab; // Chessboard tile prefab
    public int projection = 8;
    public GameObject[] whitePiecePrefabs; // Assign white piece prefabs in the Inspector
    public GameObject[] blackPiecePrefabs; // Assign black piece prefabs in the Inspector
    private GameObject[,] tiles = new GameObject[8, 8];

    private List<GameObject> whitePieces = new List<GameObject>();
    private List<GameObject> blackPieces = new List<GameObject>();

    private string turn = "white";

    // Tint colors for tiles
    public Color whiteTileTint = new Color(0.9f, 0.9f, 0.9f, 1f); // Light gray
    public Color blackTileTint = new Color(0.2f, 0.2f, 0.2f, 1f); // Dark gray

    private Vector2Int selectedPiece;
    private bool pieceSelected = false;
    public Color highlightColor = new Color(0, 1, 0, 0.5f); // Transparent green
    private List<GameObject> highlightedTiles = new List<GameObject>(); // Stores highlighted tiles
    public GameObject highlightPrefab;

    private readonly string[,] startBoard = {
        { "r", "n", "b", "q", "k", "b", "n", "r" },
        { "p", "p", "p", "p", "p", "p", "p", "p" },
        { "",  "",  "",  "",  "",  "",  "",  ""  },
        { "",  "",  "",  "",  "",  "",  "",  ""  },
        { "",  "",  "",  "",  "",  "",  "",  ""  },
        { "",  "",  "",  "",  "",  "",  "",  ""  },
        { "P", "P", "P", "P", "P", "P", "P", "P" },
        { "R", "N", "B", "Q", "K", "B", "N", "R" }
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
        Camera.main.transform.position = new Vector3(3.5f, 3.5f, -10);
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

                SpriteRenderer tileRenderer = tile.GetComponent<SpriteRenderer>();
                tileRenderer.color = (x + y) % 2 == 0 ? whiteTileTint : blackTileTint;

                // Set sorting order for board tiles (lowest layer)
                tileRenderer.sortingOrder = -2;

                // Add BoxCollider2D to the tile for click detection
                BoxCollider2D collider = tile.AddComponent<BoxCollider2D>();
                collider.isTrigger = true;

                tiles[x, y] = tile;
            }
        }
    }

    bool BoardIsReady()
    {
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                if (tiles[x, y] == null)
                    return false;
            }
        }
        return true;
    }

    void SpawnPieces()
    {
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                string pieceSymbol = startBoard[y, x];
                if (!string.IsNullOrEmpty(pieceSymbol))
                {
                    GameObject piece = CreatePiece(pieceSymbol, new Vector2(x, y));

                    if (piece != null)
                    {
                        if (char.IsUpper(pieceSymbol[0]))
                            whitePieces.Add(piece);
                        else
                            blackPieces.Add(piece);
                    }
                }
            }
        }
    }

    GameObject CreatePiece(string pieceSymbol, Vector2 position)
    {
        if (!piecePrefabIndex.ContainsKey(pieceSymbol))
        {
            Debug.LogError($"Unrecognized piece symbol: {pieceSymbol}");
            return null;
        }

        bool isWhite = char.IsUpper(pieceSymbol[0]);
        int prefabIndex = piecePrefabIndex[pieceSymbol];

        GameObject piecePrefab = isWhite ? whitePiecePrefabs[prefabIndex] : blackPiecePrefabs[prefabIndex];

        if (piecePrefab == null)
        {
            Debug.LogError($"Missing prefab for {pieceSymbol}");
            return null;
        }

        GameObject piece = Instantiate(piecePrefab, new Vector3(position.x, position.y, 0), Quaternion.identity);
        piece.name = isWhite ? $"White_{pieceSymbol}" : $"Black_{pieceSymbol}";

        // ✅ Fix: Ensure pieces are above highlights
        SpriteRenderer sr = piece.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingOrder = 0;
        }

        Debug.Log($"Spawned {piece.name} at {position}");

        return piece;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider != null)
            {
                string clickedObjectName = hit.collider.gameObject.name;
                Debug.Log($"🖱 Clicked on {clickedObjectName}");

                // ✅ Detect if the clicked object is a tile
                if (clickedObjectName.StartsWith("Tile"))
                {
                    Vector2Int boardPos = Vector2Int.RoundToInt(hit.collider.gameObject.transform.position);
                    HandleTileClick(boardPos);
                }
            }
        }
    }


    bool IsTileHighlighted(Vector2Int position)
    {
        foreach (GameObject highlight in highlightedTiles)
        {
            if (Vector2Int.RoundToInt(highlight.transform.position) == position)
            {
                return true;
            }
        }
        return false;
    }



    void HandleTileClick(Vector2Int boardPos)
    {
        Debug.Log($"📌 Clicked on tile at {boardPos}");

        // ✅ Check if a piece exists at the clicked tile
        GameObject clickedPiece = GetPieceAtPosition(boardPos);

        if (!pieceSelected)
        {
            // ✅ If there's a piece on the tile and it's the correct turn, select it
            if (clickedPiece != null && IsCorrectTurn(clickedPiece.name))
            {
                Debug.Log($"✔ Selecting piece: {clickedPiece.name}");
                selectedPiece = boardPos;
                pieceSelected = true;
                HighlightValidMoves(boardPos);
            }
            else
            {
                Debug.Log("❌ No valid piece selected.");
            }
        }
        else
        {
            // ✅ If a piece is selected, check if the clicked tile is a valid move
            if (IsTileHighlighted(boardPos))
            {
                MovePiece(boardPos);
            }
            else
            {
                Debug.Log("❌ Invalid move. Resetting selection.");
                ClearHighlights();
                pieceSelected = false;
            }
        }
    }

    void HighlightValidMoves(Vector2Int pos)
    {
        Debug.Log($"🟩 Highlighting valid moves for: {pos}");

        ClearHighlights(); // Remove old highlights
        List<Vector2Int> validMoves = GetValidMoves(pos);

        Debug.Log($"✅ Valid moves found: {validMoves.Count}");

        foreach (Vector2Int move in validMoves)
        {
            Debug.Log($"🟢 Highlighting move at: {move}");

            // ✅ Instantiate HighlightPrefab instead of creating from scratch
            GameObject highlight = Instantiate(highlightPrefab, new Vector2(move.x, move.y), Quaternion.identity);

            // ✅ Ensure it's stored in the list for proper clearing later
            highlightedTiles.Add(highlight);
        }
    }

    void ClearHighlights()
    {
        Debug.Log($"Clearing {highlightedTiles.Count} highlights");
        
        foreach (GameObject highlight in highlightedTiles)
        {
            if (highlight != null)
            {
                Destroy(highlight);
            }
        }

        highlightedTiles.Clear();
    }

    void MovePiece(Vector2Int target)
    {
        if (GetValidMoves(selectedPiece).Contains(target))
        {
            GameObject piece = GetPieceAtPosition(selectedPiece);

            if (piece != null)
            {
                Debug.Log($"🚀 Moving {piece.name} from {selectedPiece} to {target}");

                piece.transform.position = new Vector2(target.x, target.y);
                UpdateBoardTracking(selectedPiece, target);

                // ✅ Reset Selection and Switch Turn
                pieceSelected = false;
                ClearHighlights();
                turn = (turn == "white") ? "black" : "white";

                // ✅ **AI Move After Player**
                if (turn == "black") StartCoroutine(AIMove());
            }
        }
    }

    void UpdateBoardTracking(Vector2Int oldPos, Vector2Int newPos)
    {
        GameObject piece = GetPieceAtPosition(oldPos);
        if (piece != null)
        {
            // Remove from old position and add to new
            if (whitePieces.Contains(piece))
            {
                whitePieces.Remove(piece);
                whitePieces.Add(piece);
            }
            else if (blackPieces.Contains(piece))
            {
                blackPieces.Remove(piece);
                blackPieces.Add(piece);
            }
        }
    }


    bool IsCorrectTurn(string pieceName)
    {
        return (turn == "white" && pieceName.StartsWith("White")) || (turn == "black" && pieceName.StartsWith("Black"));
    }

    bool IsInsideBoard(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < 8 && pos.y >= 0 && pos.y < 8;
    }

    List<Vector2Int> GetValidMoves(Vector2Int tilePos)
    {
        List<Vector2Int> validMoves = new List<Vector2Int>();

        GameObject piece = GetPieceAtPosition(tilePos);
        if (piece == null)
        {
            Debug.LogError($"❌ No piece found at {tilePos}");
            return validMoves;
        }

        string pieceTag = piece.tag; // ✅ Use tags instead of parsing name
        bool isWhite = pieceTag.Contains("White");

        Debug.Log($"🔍 Checking valid moves for {pieceTag} at {tilePos}");

        switch (pieceTag)
        {
            case "WhitePawn":
            case "BlackPawn":
                return GetPawnMoves(tilePos, isWhite);

            case "WhiteRook":
            case "BlackRook":
                return GetSlidingMoves(tilePos, new Vector2Int[] { 
                    Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right 
                }, isWhite);

            case "WhiteBishop":
            case "BlackBishop":
                return GetSlidingMoves(tilePos, new Vector2Int[] { 
                    new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
                }, isWhite);

            case "WhiteQueen":
            case "BlackQueen":
                return GetSlidingMoves(tilePos, new Vector2Int[] {
                    Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
                    new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
                }, isWhite);

            case "WhiteKing":
            case "BlackKing":
                return GetKingMoves(tilePos, isWhite);

            case "WhiteKnight":
            case "BlackKnight":
                return GetKnightMoves(tilePos, isWhite);
        }

        return validMoves;
    }

    List<Vector2Int> GetSlidingMoves(Vector2Int pos, Vector2Int[] directions, bool isWhite)
    {
        List<Vector2Int> moves = new List<Vector2Int>();

        foreach (Vector2Int dir in directions)
        {
            Vector2Int currentPos = pos + dir;

            while (IsInsideBoard(currentPos))
            {
                GameObject target = GetPieceAtPosition(currentPos);
                if (target == null)
                {
                    moves.Add(currentPos);
                }
                else
                {
                    if (IsEnemyPiece(target, isWhite))
                        moves.Add(currentPos);
                    break;
                }
                currentPos += dir;
            }
        }

        return moves;
    }


    List<Vector2Int> GetPawnMoves(Vector2Int pos, bool isWhite)
    {
        List<Vector2Int> validMoves = new List<Vector2Int>();

        int direction = isWhite ? -1 : 1; 
        int startRow = isWhite ? 6 : 1;   

        Vector2Int forward = new Vector2Int(pos.x, pos.y + direction);

        if (IsInsideBoard(forward) && GetPieceAtPosition(forward) == null)
        {
            validMoves.Add(forward);

            Vector2Int doubleStep = new Vector2Int(pos.x, pos.y + (2 * direction));
            if (pos.y == startRow && GetPieceAtPosition(doubleStep) == null)
            {
                validMoves.Add(doubleStep);
            }
        }

        Vector2Int[] captures = {
            new Vector2Int(pos.x - 1, pos.y + direction),
            new Vector2Int(pos.x + 1, pos.y + direction)
        };

        foreach (Vector2Int capture in captures)
        {
            if (IsInsideBoard(capture))
            {
                GameObject target = GetPieceAtPosition(capture);
                if (target != null && IsEnemyPiece(target, isWhite))
                {
                    validMoves.Add(capture);
                }
            }
        }

        return validMoves;
    }

    List<Vector2Int> GetKingMoves(Vector2Int pos, bool isWhite)
    {
        List<Vector2Int> moves = new List<Vector2Int>();

        Vector2Int[] directions = {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
        };

        foreach (Vector2Int dir in directions)
        {
            Vector2Int newPos = pos + dir;
            if (IsInsideBoard(newPos))
            {
                GameObject target = GetPieceAtPosition(newPos);
                if (target == null || IsEnemyPiece(target, isWhite))
                    moves.Add(newPos);
            }
        }

        return moves;
    }

    List<Vector2Int> GetKnightMoves(Vector2Int pos, bool isWhite)
    {
        List<Vector2Int> moves = new List<Vector2Int>();

        Vector2Int[] jumps = {
            new Vector2Int(1, 2), new Vector2Int(1, -2), new Vector2Int(-1, 2), new Vector2Int(-1, -2),
            new Vector2Int(2, 1), new Vector2Int(2, -1), new Vector2Int(-2, 1), new Vector2Int(-2, -1)
        };

        foreach (Vector2Int jump in jumps)
        {
            Vector2Int newPos = pos + jump;
            if (IsInsideBoard(newPos))
            {
                GameObject target = GetPieceAtPosition(newPos);
                if (target == null || IsEnemyPiece(target, isWhite))
                    moves.Add(newPos);
            }
        }

        return moves;
    }


    // Get piece at a given position
    GameObject GetPieceAtPosition(Vector2Int pos)
    {
        foreach (GameObject piece in whitePieces)
        {
            if (Vector2Int.RoundToInt(piece.transform.position) == pos)
            {
                return piece;
            }
        }

        foreach (GameObject piece in blackPieces)
        {
            if (Vector2Int.RoundToInt(piece.transform.position) == pos)
            {
                return piece;
            }
        }

        return null;
    }


    // Check if the piece is an enemy piece
    bool IsEnemyPiece(GameObject piece, bool isWhite)
    {
        return (isWhite && piece.name.StartsWith("Black")) || (!isWhite && piece.name.StartsWith("White"));
    }

    IEnumerator AIMove()
    {
        yield return new WaitForSeconds(0.5f); 

        List<GameObject> aiPieces = blackPieces; 
        List<GameObject> movablePieces = new List<GameObject>();

        foreach (GameObject piece in aiPieces)
        {
            Vector2Int pos = Vector2Int.RoundToInt(piece.transform.position);
            List<Vector2Int> moves = GetValidMoves(pos);

            if (moves.Count > 0)
                movablePieces.Add(piece);
        }

        if (movablePieces.Count == 0)
        {
            Debug.Log("AI has no valid moves! Skipping turn.");
            turn = "white";
            yield break;
        }

        GameObject selectedPiece = movablePieces[Random.Range(0, movablePieces.Count)];
        Vector2Int piecePos = Vector2Int.RoundToInt(selectedPiece.transform.position);
        List<Vector2Int> validMoves = GetValidMoves(piecePos);

        Vector2Int chosenMove = validMoves[Random.Range(0, validMoves.Count)];
        selectedPiece.transform.position = new Vector2(chosenMove.x, chosenMove.y);
        
        UpdateBoardTracking(piecePos, chosenMove);
        turn = "white";
    }
}
