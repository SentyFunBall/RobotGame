using System.Collections.Generic;
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

    private readonly Dictionary<string, AnimatorControllerParameterType> parameterTypes = new();
    private readonly HashSet<string> warnedMissingParams = new();

    public void FindAndSetAnimator()
    {
        if (transform.childCount > 0)
            animator = transform.GetChild(0).GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator not found on character.");
            return;
        }

        RefreshParameterCache();
    }

    private void RefreshParameterCache()
    {
        if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null)
            return;

        AnimatorControllerParameter[] parameters = animator.parameters;
        if (parameters == null || parameters.Length == 0)
            return;

        parameterTypes.Clear();
        foreach (AnimatorControllerParameter param in parameters)
        {
            parameterTypes[param.name] = param.type;
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

    public bool HasParameter(string paramName, AnimatorControllerParameterType type)
    {
        if (animator == null || string.IsNullOrEmpty(paramName))
            return false;

        if (parameterTypes.Count == 0)
            RefreshParameterCache();

        if (
            parameterTypes.TryGetValue(paramName, out AnimatorControllerParameterType foundType)
            && foundType == type
        )
        {
            return true;
        }

        if (parameterTypes.Count > 0 && warnedMissingParams.Add(paramName))
        {
            Debug.LogWarning(
                $"Animator on '{gameObject.name}' does not have a {type} parameter named '{paramName}'. Available: {string.Join(", ", parameterTypes.Keys)}",
                this
            );
        }

        return false;
    }

    public void SetBool(string paramName, bool value)
    {
        if (HasParameter(paramName, AnimatorControllerParameterType.Bool))
        {
            animator.SetBool(paramName, value);
        }
    }

    public void SetTrigger(string paramName)
    {
        if (HasParameter(paramName, AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger(paramName);
        }
    }
}