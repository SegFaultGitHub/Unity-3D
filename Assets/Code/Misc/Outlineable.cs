using UnityEngine;

[RequireComponent(typeof(Outline))]
public abstract class Outlineable : MonoBehaviour {
    private Outline Outline;
    private LTDescr CurrentEnableDisableTween;
    private LTDescr CurrentColorTween;

    private void Awake() {
        this.Outline = this.GetComponent<Outline>();
        this.Outline.OutlineWidth = 5;
        this.Outline.OutlineMode = Outline.Mode.OutlineVisible;
        this.Outline.OutlineColor = new(1, 1, 1, 0);
    }

    public void Enable() {
        if (this.CurrentEnableDisableTween != null)
            LeanTween.cancel(this.CurrentEnableDisableTween.id);
        this.CurrentEnableDisableTween = LeanTween.value(this.Outline.OutlineColor.a, 1f, 0.3f)
            .setOnUpdate(alpha => this.Outline.OutlineColor = new(this.Outline.OutlineColor.r, this.Outline.OutlineColor.g, this.Outline.OutlineColor.b, alpha))
            .setOnComplete(() => this.CurrentEnableDisableTween = null);
    }

    public void Disable() {
        if (this.CurrentEnableDisableTween != null)
            LeanTween.cancel(this.CurrentEnableDisableTween.id);
        this.CurrentEnableDisableTween = LeanTween.value(this.Outline.OutlineColor.a, 0f, 0.3f)
            .setOnUpdate(alpha => this.Outline.OutlineColor = new(this.Outline.OutlineColor.r, this.Outline.OutlineColor.g, this.Outline.OutlineColor.b, alpha))
            .setOnComplete(() => this.CurrentEnableDisableTween = null);
    }

    public void ChangeColor(Color to) {
        if (this.CurrentColorTween != null)
            LeanTween.cancel(this.CurrentColorTween.id);
        Color from = this.Outline.OutlineColor;
        this.CurrentColorTween = LeanTween.value(this.gameObject, from, to, 0.3f)
            .setOnUpdate(color => this.Outline.OutlineColor = new(color.r, color.g, color.b, this.Outline.OutlineColor.a))
            .setOnComplete(() => this.CurrentColorTween = null);
    }
}
