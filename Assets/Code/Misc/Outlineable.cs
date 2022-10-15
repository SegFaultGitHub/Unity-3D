using UnityEngine;

[RequireComponent(typeof(Outline))]
public class Outlineable : MonoBehaviour {
    private Outline Outline;
    private LTDescr CurrentTween;

    private void Awake() {
        this.Outline = this.GetComponent<Outline>();
        this.Outline.OutlineWidth = 5;
        this.Outline.OutlineMode = Outline.Mode.OutlineVisible;
        this.Outline.OutlineColor = new(1, 1, 1, 0);
    }

    public void Enable() {
        if (this.CurrentTween != null)
            LeanTween.cancel(this.CurrentTween.id);
        Color color = this.Outline.OutlineColor;
        this.CurrentTween = LeanTween.value(color.a, 1f, 0.3f)
            .setOnUpdate(alpha => this.Outline.OutlineColor = new(color.r, color.g, color.b, alpha));
    }

    public void Disable() {
        if (this.CurrentTween != null)
            LeanTween.cancel(this.CurrentTween.id);
        Color color = this.Outline.OutlineColor;
        this.CurrentTween = LeanTween.value(color.a, 0f, 0.3f)
            .setOnUpdate(alpha => this.Outline.OutlineColor = new(color.r, color.g, color.b, alpha));
    }
}
