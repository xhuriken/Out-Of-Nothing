using UnityEngine;
using Shapes;
using DG.Tweening;

/// <summary>
/// Dynamically draws a yellow outline ring around a selected object using Shapes.Disc.
/// Includes a pulsing yoyo scale animation.
/// </summary>
public class SelectionHighlight : MonoBehaviour
{
    private Disc _highlightDisc;
    private Tween _pulseTween;

    private void Start()
    {
        // Create a child GameObject to hold the Shapes Disc so that scaling/rotation is isolated
        GameObject child = new GameObject("SelectionHighlightRing");
        child.transform.SetParent(transform, false);

        _highlightDisc = child.AddComponent<Disc>();
        _highlightDisc.Color = new Color(1f, 0.92f, 0.016f, 0.85f); // Bright yellow
        _highlightDisc.Type = DiscType.Ring;
        
        // Find radius from collider bounds or default to 1.0f
        float radius = 1.0f;
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            // Compute a radius that bounds the shape nicely
            radius = Mathf.Max(col.bounds.extents.x, col.bounds.extents.y) * 1.25f;
            
            // Adjust radius for parent's lossyScale to keep visual thickness/radius uniform
            Vector3 lossyScale = transform.lossyScale;
            float maxScale = Mathf.Max(lossyScale.x, lossyScale.y);
            if (maxScale > 0.001f)
            {
                radius /= maxScale;
            }
        }
        
        _highlightDisc.Radius = radius;
        _highlightDisc.Thickness = 0.06f; // Standard outline thickness

        // Pulse Animation using DOTween
        _highlightDisc.transform.localScale = Vector3.zero;
        _highlightDisc.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
        
        _pulseTween = _highlightDisc.transform.DOScale(Vector3.one * 1.08f, 0.8f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    private void OnDestroy()
    {
        if (_pulseTween != null)
        {
            _pulseTween.Kill();
        }

        if (_highlightDisc != null)
        {
            Destroy(_highlightDisc.gameObject);
        }
    }
}
