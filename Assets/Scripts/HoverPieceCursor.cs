using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HoverPieceCursor : MonoBehaviour
{
    public GameObject hoveredPiece;
    public Color highlightColor;
    public bool inDrag;


    private void LateUpdate()
    {
        Vector2 mouseKoordsInWorld = new Vector2(Camera.main.ScreenToWorldPoint(Input.mousePosition).x, Camera.main.ScreenToWorldPoint(Input.mousePosition).y);
        transform.position = mouseKoordsInWorld;
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!inDrag)
        {
            hoveredPiece = other.gameObject;
            hoveredPiece.GetComponent<SpriteRenderer>().color = highlightColor; 
        }
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (!inDrag && hoveredPiece != null)
        {
            hoveredPiece.GetComponent<SpriteRenderer>().color = Color.white;
            hoveredPiece = null;
        }
    }
}
