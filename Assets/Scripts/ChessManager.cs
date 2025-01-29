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
                Debug.Log($"Clicked on {hit.collider.gameObject.name}");

                if (hit.collider.gameObject.name.StartsWith("Tile"))
                {
                    Vector2Int boardPos = Vector2Int.RoundToInt(hit.collider.gameObject.transform.position);
                    Debug.Log($"Rounded Board Position: {boardPos}");
                    HandleTileClick(boardPos);
                }
                else if (hit.collider.gameObject.name.StartsWith("White") || hit.collider.gameObject.name.StartsWith("Black"))
                {
                    HandlePieceClick(hit.collider.gameObject);
                }
            }
        }
    }

    void HandlePieceClick(GameObject piece)
    {
        Debug.Log($"Selected piece: {piece.name}");

        if (IsCorrectTurn(piece.name))
        {
            selectedPiece = Vector2Int.RoundToInt(piece.transform.position);
            pieceSelected = true;
            HighlightValidMoves(selectedPiece);
        }
        else
        {
            Debug.Log("Not your turn!");
        }
    }

    void HandleTileClick(Vector2Int boardPos)
    {
        if (pieceSelected)
        {
            MovePiece(boardPos);
        }
    }


    void SelectPiece(Vector2Int pos)
    {
        GameObject selected = GetPieceAtPosition(pos);
        if (selected != null && IsCorrectTurn(selected.name))
        {
            selectedPiece = pos;
            pieceSelected = true;
            HighlightValidMoves(pos);
        }
    }

    void HighlightValidMoves(Vector2Int pos)
    {
        ClearHighlights(); // Remove old highlights
        List<Vector2Int> validMoves = GetValidMoves(pos);

        Debug.Log($"Valid moves for {pos}: {validMoves.Count}");

        foreach (Vector2Int move in validMoves)
        {
            Debug.Log($"Highlighting move at: {move}");
            GameObject highlight = new GameObject("Highlight");
            SpriteRenderer sr = highlight.AddComponent<SpriteRenderer>();
            sr.color = highlightColor;
            sr.sortingOrder = 0;
            highlight.transform.position = new Vector2(move.x, move.y);
            highlightedTiles.Add(highlight);
        }
    }

    void ClearHighlights()
    {
        Debug.Log($"Clearing {highlightedTiles.Count} highlights");
        foreach (GameObject tile in highlightedTiles)
        {
            Destroy(tile);
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
                Debug.Log($"Moving {piece.name} from {selectedPiece} to {target}");

                piece.transform.position = new Vector2(target.x, target.y);

                // Remove old position from tracking
                UpdateBoardTracking(selectedPiece, target);

                pieceSelected = false;
                turn = (turn == "white") ? "black" : "white";
                ClearHighlights();

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

    List<Vector2Int> GetValidMoves(Vector2Int pos)
    {
        List<Vector2Int> validMoves = new List<Vector2Int>();
        GameObject piece = GetPieceAtPosition(pos);

        if (piece == null)
        {
            Debug.Log($"No piece found at {pos}");
            return validMoves;
        }

        string pieceName = piece.name;
        bool isWhite = pieceName.StartsWith("White");

        // ✅ **Check if the piece is a Pawn using EndsWith**
        if (pieceName.EndsWith("Pawn"))
        {
            int direction = isWhite ? -1 : 1; // ✅ White moves UP (-1), Black moves DOWN (+1)
            int startRow = isWhite ? 6 : 1;   // ✅ **White pawns start at row 6, Black pawns start at row 1**

            Vector2Int forward = new Vector2Int(pos.x, pos.y + direction);

            // ✅ **Move forward if empty**
            if (IsInsideBoard(forward) && GetPieceAtPosition(forward) == null)
            {
                validMoves.Add(forward);

                // ✅ **Double-step only if in starting position & both spaces are empty**
                Vector2Int doubleStep = new Vector2Int(pos.x, pos.y + (2 * direction));
                if (pos.y == startRow && GetPieceAtPosition(doubleStep) == null && GetPieceAtPosition(forward) == null)
                {
                    validMoves.Add(doubleStep);
                }
            }

            // ✅ **Diagonal captures (check if an enemy piece is present)**
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
        }

        Debug.Log($"Valid moves for {pieceName} at {pos}: {validMoves.Count}");
        return validMoves;
    }

    // Get piece at a given position
    GameObject GetPieceAtPosition(Vector2Int pos)
    {
        foreach (GameObject piece in whitePieces)
        {
            if (Vector2Int.RoundToInt(piece.transform.position) == pos)
            {
                Debug.Log($"Found {piece.name} at {pos}");
                return piece;
            }
        }

        foreach (GameObject piece in blackPieces)
        {
            if (Vector2Int.RoundToInt(piece.transform.position) == pos)
            {
                Debug.Log($"Found {piece.name} at {pos}");
                return piece;
            }
        }

        Debug.Log($"No piece found at {pos}");
        return null;
    }



    // Check if the piece is an enemy piece
    bool IsEnemyPiece(GameObject piece, bool isWhite)
    {
        return (isWhite && piece.name.StartsWith("Black")) || (!isWhite && piece.name.StartsWith("White"));
    }

    IEnumerator AIMove()
    {
        yield return new WaitForSeconds(0.5f); // Small delay for AI move simulation

        List<GameObject> aiPieces = blackPieces; // AI moves black pieces
        List<GameObject> movablePieces = new List<GameObject>();

        // Find all black pieces that have valid moves
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
            yield break; // **Prevent freeze**
        }

        // Choose a random piece from those that have valid moves
        GameObject selectedPiece = movablePieces[Random.Range(0, movablePieces.Count)];
        Vector2Int piecePos = Vector2Int.RoundToInt(selectedPiece.transform.position);
        List<Vector2Int> validMoves = GetValidMoves(piecePos);

        if (validMoves.Count == 0) // **Extra safeguard**
        {
            Debug.Log("AI piece has no valid moves. Skipping.");
            turn = "white";
            yield break;
        }

        // Pick a random move from the selected piece's valid moves
        Vector2Int chosenMove = validMoves[Random.Range(0, validMoves.Count)];

        // Move AI piece
        selectedPiece.transform.position = new Vector2(chosenMove.x, chosenMove.y);
        
        // ✅ Ensure board updates tracking
        UpdateBoardTracking(piecePos, chosenMove);

        // Switch turn back to white
        turn = "white";
    }


}
