using System;
using UnityEngine;

public class Door : Outlineable {
    public bool Opened { get; private set; }

    public void OpenSide1() {
        if (this.Opened)
            return;
        this.Disable();

        this.Opened = true;
        Vector3 angles = this.transform.eulerAngles;
        LeanTween
            .value(this.gameObject, angles.y, angles.y + 75, 0.75f)
            .setOnUpdate(angle => this.transform.eulerAngles = new(angles.x, angle, angles.z))
            .setEaseOutBounce();
    }

    public void OpenSide2() {
        if (this.Opened)
            return;
        this.Disable();

        this.Opened = true;
        Vector3 angles = this.transform.eulerAngles;
        LeanTween
            .value(this.gameObject, angles.y, angles.y - 75, 0.75f)
            .setOnUpdate(angle => this.transform.eulerAngles = new(angles.x, angle, angles.z))
            .setEaseOutBounce();
    }
}
