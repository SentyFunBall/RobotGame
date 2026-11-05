using UnityEngine;

public class CharacterAnimation : MonoBehaviour
{
    //[SerializeField]
    Animator animator;

    private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int LookAroundHash = Animator.StringToHash("LookAround");
    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");
    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int SprintHash = Animator.StringToHash("Sprint");

    public void FindAndSetAnimator()
    {
        animator = transform.GetChild(0).GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator not found on character.");
        }
    }

    public void SetMovementSpeed(float speed)
    {
        animator.SetFloat(MoveSpeedHash, speed);
    }

    public void Attack()
    {
        animator.SetTrigger(AttackHash);
    }

    public void LookAround()
    {
        animator.SetTrigger(LookAroundHash);
    }

    public void SetDead(bool dead)
    {
        animator.SetBool(IsDeadHash, dead);
    }

    public void Hit()
    {
        animator.SetTrigger(HitHash);
    }

    public void Sprint(float speed)
    {
        animator.SetFloat(SprintHash, speed);
    }
}
