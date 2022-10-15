using Cinemachine;
using System.Collections;
using UnityEngine;

public class Chest : Outlineable {
    public bool Opened;
    [SerializeField] private GameObject Top;

    public void Open() {
        if (this.Opened)
            return;
        this.Disable();


        this.StartCoroutine(this.SetCameraTarget());

        this.Opened = true;
        Vector3 angles = this.Top.transform.eulerAngles;
        LeanTween
            .value(this.Top, angles.x, angles.x - 60, 0.75f)
            .setOnUpdate(angle => this.Top.transform.eulerAngles = new(angle, angles.y, angles.z))
            .setEaseOutBounce();
    }

    private IEnumerator SetCameraTarget() {
        PauseManager.Pause();
        CinemachineFreeLook cinemachine = GameObject.FindGameObjectWithTag("CinemachineCamera").GetComponent<CinemachineFreeLook>();
        Transform follow = cinemachine.Follow;
        Transform lookAt = cinemachine.LookAt;
        float xMaxSpeed = cinemachine.m_XAxis.m_MaxSpeed;
        float yMaxSpeed = cinemachine.m_YAxis.m_MaxSpeed;
        float fov = cinemachine.m_Lens.FieldOfView;
        float targetFov = 30;

        LeanTween.value(cinemachine.m_XAxis.Value, this.transform.eulerAngles.y - 180, 0.2f)
            .setOnUpdate(angle => cinemachine.m_XAxis.Value = angle);
        LeanTween.value(cinemachine.m_YAxis.Value, 0, 0.2f)
            .setOnUpdate(angle => cinemachine.m_YAxis.Value = angle);
        LeanTween.value(fov, targetFov, 0.2f)
            .setOnUpdate(fov => cinemachine.m_Lens.FieldOfView = fov)
            .setOnStart(() => {
                cinemachine.Follow = this.transform;
                cinemachine.LookAt = this.transform;
                cinemachine.m_XAxis.m_MaxSpeed = 0;
                cinemachine.m_YAxis.m_MaxSpeed = 0;
            });

        yield return new WaitForSeconds(2);

        LeanTween.value(targetFov, fov, 0.2f)
            .setOnUpdate(fov => cinemachine.m_Lens.FieldOfView = fov)
            .setOnStart(() => {
                cinemachine.m_XAxis.m_MaxSpeed = xMaxSpeed;
                cinemachine.m_YAxis.m_MaxSpeed = yMaxSpeed;
            })
            .setOnComplete(() => {
                cinemachine.Follow = follow;
                cinemachine.LookAt = lookAt;
                PauseManager.Unpause();
            });
    }
}
