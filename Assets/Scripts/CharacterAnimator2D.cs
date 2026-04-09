using UnityEngine;
using System.Collections;

public class CharacterAnimator2D : MonoBehaviour
{
    [Header("Idle Animation")]
    public float floatSpeed = 2f;
    public float floatHeight = 20f; // 20 pixels up and down
    private Vector3 startPos;

    [Header("Attack Animation")]
    public float attackForwardDistance = 150f; // 150 pixels forward
    public float attackSpeed = 0.2f;
    
    [Header("Event Listening")]
    [Tooltip("If checked, this character will lunge forward whenever any card is played.")]
    public bool attackOnCardPlay = false;
    
    [Header("Projectile Attack")]
    [Tooltip("The object/prefab to throw at the enemy (e.g. laser, satellite, spark)")]
    public GameObject projectilePrefab;
    [Tooltip("The character that should be hit by the projectile")]
    public Transform attackTarget;
    [Tooltip("How fast the projectile flies across the screen (in seconds)")]
    public float projectileSpeed = 0.3f;
    
    private bool isAttacking = false;
    private DeckManager deckManager;

    void Start()
    {
        startPos = transform.localPosition;
        
        // Find the deck manager so we can listen to cards being played!
        deckManager = FindAnyObjectByType<DeckManager>();
        if (deckManager != null)
        {
            deckManager.OnCardPlayed += HandleCardPlayed;
        }
    }

    private void HandleCardPlayed(CardData card)
    {
        if (attackOnCardPlay)
        {
            PlayAttackAnimation();
        }
    }

    void OnDestroy()
    {
        // Always clean up event listeners to prevent memory leaks!
        if (deckManager != null)
        {
            deckManager.OnCardPlayed -= HandleCardPlayed;
        }
    }

    void Update()
    {
        if (!isAttacking)
        {
            // Idle floating animation
            float newY = startPos.y + (Mathf.Sin(Time.time * floatSpeed) * floatHeight);
            transform.localPosition = new Vector3(transform.localPosition.x, newY, transform.localPosition.z);
        }
    }

    // Call this method when a card is played
    public void PlayAttackAnimation()
    {
        if (!isAttacking)
        {
            StartCoroutine(AttackRoutine());
        }
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        
        // Decide which direction to lunge based on what side of the screen we are on
        Vector3 direction = (transform.localPosition.x < 0) ? Vector3.right : Vector3.left;
        Vector3 peakPos = startPos + direction * attackForwardDistance; 
        Vector3 currentPos = transform.localPosition;

        // 1. Lunge forward
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime / (attackSpeed / 2f);
            transform.localPosition = Vector3.Lerp(currentPos, peakPos, t);
            yield return null;
        }

        // 2. Fire the projectile exactly from our peak position!
        if (projectilePrefab != null && attackTarget != null)
        {
            try
            {
                // This will safely try to start the routine, but if the projectile was a deleted scene object, it catches the error!
                StartCoroutine(ThrowProjectileRoutine(peakPos, attackTarget.localPosition));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Project Prefab is missing or was deleted! Make sure you assign the Prefab from the Project folder, not the Hierarchy!");
            }
        }

        // 3. Return to start position
        t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime / (attackSpeed / 2f);
            transform.localPosition = Vector3.Lerp(peakPos, startPos, t);
            yield return null;
        }

        transform.localPosition = startPos;
        isAttacking = false;
    }

    private IEnumerator ThrowProjectileRoutine(Vector3 startP, Vector3 endP)
    {
        // Spawn the projectile in the same canvas as this character
        GameObject proj = Instantiate(projectilePrefab, transform.parent);
        proj.transform.localPosition = startP;
        
        // Ensure the projectile flies smoothly across the screen
        float t = 0;
        while(t < 1f)
        {
            t += Time.deltaTime / projectileSpeed;
            proj.transform.localPosition = Vector3.Lerp(startP, endP, t);
            yield return null;
        }
        
        // The projectile hit the target! Delete it.
        Destroy(proj);
    }
}
