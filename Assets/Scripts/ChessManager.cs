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
                tile.AddComponent<BoxCollider2D>().isTrigger = true;
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
        bool isWhite = char.IsUpper(pieceSymbol[0]);
        int prefabIndex = piecePrefabIndex[pieceSymbol];
        GameObject piecePrefab = isWhite ? whitePiecePrefabs[prefabIndex] : blackPiecePrefabs[prefabIndex];
        if (piecePrefab == null) return null;
        
        GameObject piece = Instantiate(piecePrefab, new Vector3(position.x, position.y, 0), Quaternion.identity);
        piece.tag = isWhite ? "White" + pieceSymbol.ToUpper() : "Black" + pieceSymbol.ToLower();
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
        if (piece == null) return validMoves;

        string pieceTag = piece.tag;
        bool isWhite = pieceTag.StartsWith("White");

        switch (pieceTag)
        {
            case "WhiteP":
            case "BlackP":
                validMoves = GetPawnMoves(pos, isWhite);
                break;
            case "WhiteR":
            case "BlackR":
                validMoves = GetRookMoves(pos, isWhite);
                break;
            case "WhiteN":
            case "BlackN":
                validMoves = GetKnightMoves(pos, isWhite);
                break;
            case "WhiteB":
            case "BlackB":
                validMoves = GetBishopMoves(pos, isWhite);
                break;
            case "WhiteQ":
            case "BlackQ":
                validMoves = GetQueenMoves(pos, isWhite);
                break;
            case "WhiteK":
            case "BlackK":
                validMoves = GetKingMoves(pos, isWhite);
                break;
        }
        return validMoves;
    }
}
