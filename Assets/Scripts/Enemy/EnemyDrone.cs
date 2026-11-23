using UnityEngine;

public class EnemyDrone : AerialEnemyBase
{
    public GameObject cannonballPrefab;
    public Transform firePoint;
    public float fireInterval = 3f;
    public float detectionRange = 10f;

    private float timer = 0f;

    protected override void OnResetState()
    {
        base.OnResetState();
        timer = 0f;
    }

    protected override void OnAerialUpdate()
    {
        if (player == null)
        {
            return;
        }

        timer += Time.deltaTime;
        if (timer >= fireInterval)
        {
            Fire();
            timer = 0f;
        }
    }

    private void Fire()
    {
        GameObject cannonball = Instantiate(cannonballPrefab, firePoint.position, Quaternion.identity);
        EnemyCannonball cannonballScript = cannonball.GetComponent<EnemyCannonball>();
        cannonballScript.Initialize(facingDirection, null);
    }
}
