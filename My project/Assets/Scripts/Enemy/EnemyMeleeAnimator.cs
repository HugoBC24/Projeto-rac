using UnityEngine;
using UnityEngine.AI;

namespace ProjectRac.Enemy
{
    // Syncs EnemyMeleeAI state to the Boneca AnimatorController (0 = Idle, 1 = Walk, 2 = Run).
    public class EnemyMeleeAnimator : MonoBehaviour
    {
        private static readonly int AnimStateHash = Animator.StringToHash("AnimState");

        public const int StateIdle = 0;
        public const int StateWalk = 1;
        public const int StateRun = 2;

        [SerializeField] private Animator animator;
        [SerializeField] private float walkVelocityThreshold = 0.15f;

        public int CurrentAnimState { get; private set; } = -1;
        public Animator AnimatorComponent => animator;

        private EnemyMeleeAI _ai;
        private NavMeshAgent _agent;
        private EnemyHealth _health;

        private void Awake()
        {
            _ai = GetComponent<EnemyMeleeAI>();
            _agent = GetComponent<NavMeshAgent>();
            _health = GetComponent<EnemyHealth>();
            ResolveAnimator();
        }

        private void Start()
        {
            ResolveAnimator();
            SyncAnimationState(force: true);
        }

        private void Update()
        {
            SyncAnimationState(force: false);
        }

        public void BindAnimator(Animator targetAnimator)
        {
            animator = targetAnimator;
            if (animator != null)
            {
                animator.applyRootMotion = false;
                animator.keepAnimatorStateOnDisable = true;
            }
            SyncAnimationState(force: true);
        }

        public void ResolveAnimator()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }

            if (animator != null)
            {
                animator.applyRootMotion = false;
                animator.keepAnimatorStateOnDisable = true;
            }
        }

        private void SyncAnimationState(bool force)
        {
            if (animator == null)
            {
                ResolveAnimator();
                if (animator == null) return;
            }

            if (animator.runtimeAnimatorController == null) return;

            int desired = ComputeDesiredState();
            if (force || desired != CurrentAnimState)
            {
                CurrentAnimState = desired;
                animator.SetInteger(AnimStateHash, desired);
            }
        }

        private int ComputeDesiredState()
        {
            if (_health != null && _health.IsDead) return StateIdle;
            if (_ai == null) return StateIdle;

            switch (_ai.CurrentState)
            {
                case EnemyMeleeAI.AIState.Idle:
                    return StateIdle;

                case EnemyMeleeAI.AIState.Patrol:
                    if (_agent == null || _agent.isStopped) return StateIdle;
                    bool moving = _agent.velocity.sqrMagnitude >= walkVelocityThreshold * walkVelocityThreshold
                               || (_agent.hasPath && _agent.remainingDistance > _agent.stoppingDistance + 0.35f);
                    return moving ? StateWalk : StateIdle;

                case EnemyMeleeAI.AIState.Chase:
                case EnemyMeleeAI.AIState.Attack:
                    return StateRun;

                default:
                    return StateIdle;
            }
        }
    }
}
