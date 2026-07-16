using System.Collections;
using UnityEngine;

public class LaserObstacle : Obstacle
{
    [SerializeField, Min(0.01f)] private float activeDuration = 2f;
    [SerializeField, Min(0.01f)] private float inactiveDuration = 2f;
    [SerializeField] private bool startsActive = true;
    [SerializeField] private Renderer visualRenderer;
    [SerializeField] private GameObject visualChild;
    [SerializeField] private Collider2D damageCollider;

    private float baseActiveDuration;
    private float baseInactiveDuration;
    private Coroutine toggleRoutine;

    protected override void OnEnable()
    {
        base.OnEnable();
        baseActiveDuration = Mathf.Max(0.01f, activeDuration);
        baseInactiveDuration = Mathf.Max(0.01f, inactiveDuration);
        SetLaserActive(startsActive);
        toggleRoutine = StartCoroutine(ToggleLaser());
    }

    protected override void OnDisable()
    {
        if (toggleRoutine != null)
        {
            StopCoroutine(toggleRoutine);
            toggleRoutine = null;
        }

        base.OnDisable();
    }

    public override void ApplyDifficulty(DifficultyTuning tuning)
    {
        base.ApplyDifficulty(tuning);
        float difficulty = affectedByDifficulty && tuning != null ? tuning.difficulty : 0.5f;
        activeDuration = Mathf.Clamp(
            baseActiveDuration * Mathf.Lerp(0.85f, 1.3f, difficulty), 0.05f, 60f);
        inactiveDuration = Mathf.Clamp(
            baseInactiveDuration * Mathf.Lerp(1.2f, 0.65f, difficulty), 0.05f, 60f);
    }

    private IEnumerator ToggleLaser()
    {
        bool isActive = startsActive;

        while (true)
        {
            yield return new WaitForSeconds(isActive ? activeDuration : inactiveDuration);
            isActive = !isActive;
            SetLaserActive(isActive);
        }
    }

    private void SetLaserActive(bool isActive)
    {
        if (visualRenderer != null)
            visualRenderer.enabled = isActive;
        if (visualChild != null)
            visualChild.SetActive(isActive);
        if (damageCollider != null)
            damageCollider.enabled = isActive;
    }
}
