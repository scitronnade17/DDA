using UnityEngine;

[RequireComponent(typeof(PlayerSkillManager))]
public class CollisionHandler : MonoBehaviour
{
    PlayerSkillManager skillManager;

    private void Start()
    {
        skillManager = GetComponent<PlayerSkillManager>();
    }
    void OnCollisionEnter(Collision collision)
    {
        if (skillManager == null || collision.gameObject == null)
            return;

        skillManager.RegisterCollision(collision.gameObject);
    }
}