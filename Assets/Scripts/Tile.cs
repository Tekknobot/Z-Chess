using UnityEngine;

public class Tile : MonoBehaviour
{
    public Vector2Int position;
    private ChessManager chessManager;

    void Start()
    {
        // Find ChessManager in the scene dynamically
        chessManager = FindFirstObjectByType<ChessManager>();

        if (chessManager == null)
        {
            Debug.LogError("ChessManager not found in the scene!");
        }
    }

    void OnMouseDown()
    {
        Debug.Log($"Tile clicked at {position}"); // Debug log to verify clicks
        if (chessManager != null)
        {
            chessManager.HandleTileClick(position);
        }
    }
}
