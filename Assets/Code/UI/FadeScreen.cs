using UnityEngine;
using UnityEngine.UI;

public class FadeScreen : MonoBehaviour {
    private Image Image;

    private void Awake() {
        this.Image = this.GetComponentInChildren<Image>();
    }

    public LTDescr Fade(float duration) {
        Color color = this.Image.color;
        return LeanTween.value(0, 1, duration)
            .setOnUpdate(alpha => this.Image.color = new(color.r, color.g, color.b, alpha));
    }

    public LTDescr Unfade(float duration) {
        Color color = this.Image.color;
        return LeanTween.value(1, 0, duration)
            .setOnUpdate(alpha => this.Image.color = new(color.r, color.g, color.b, alpha));
    }
}
