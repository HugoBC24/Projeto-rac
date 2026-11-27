using UnityEngine;
using UnityEngine.AI;

namespace ProjectRac.Enemy
{
    // Syncs EnemyRangedAI state to the Mordomo AnimatorController (0 = Idle, 1 = Walk, 2 = Attack).
    // Scales Attack clip speed 1:1 with EnemyRangedAI.AttackCycleDuration so every shot plays one full animation.
    public class EnemyRangedAnimator : MonoBehaviour
    {
        private static readonly int AnimStateHash = Animator.StringToHash("AnimState");

        public const int StateIdle = 0;
        public const int StateWalk = 1;
        public const int StateAttack = 2;

        [SerializeField] private Animator animator;
        [SerializeField] private float walkVelocityThreshold = 0.12f;
        [SerializeField] private float attackClipLength = 1.25f; // Frames 54..84 at 24 fps

        public int CurrentAnimState { get; private set; } = -1;
        public Animator AnimatorComponent => animator;

        private EnemyRangedAI _ai;
        private NavMeshAgent _agent;
        private EnemyHealth _health;

        private void Awake()
        {
            _ai = GetComponent<EnemyRangedAI>();
            _agent = GetComponent<NavMeshAgent>();
            _health = GetComponent<EnemyHealth>();
            ResolveAnimator();
        }

        private void OnEnable()
        {
            if (_ai != null) _ai.OnAttackStarted += HandleAttackStarted;
        }

        private void OnDisable()
        {
            if (_ai != null) _ai.OnAttackStarted -= HandleAttackStarted;
        }

        private void Start()
        {
            ResolveAnimator();
            CacheAttackClipLength();
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
                CacheAttackClipLength();
            }
            SyncAnimationState(force: true);
        }

        public void ResolveAnimator()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
            if (animator != null)
            {
                animator.applyRootMotion = false;
                animator.keepAnimatorStateOnDisable = true;
            }
        }

        private void HandleAttackStarted()
        {
            if (animator == null) ResolveAnimator();
            if (animator == null || animator.runtimeAnimatorController == null) return;

            CurrentAnimState = StateAttack;
            animator.SetInteger(AnimStateHash, StateAttack);
            float cycle = _ai != null ? _ai.AttackCycleDuration : attackClipLength;
            animator.speed = Mathf.Clamp(attackClipLength / Mathf.Max(0.25f, cycle), 0.35f, 3.0f);
            animator.Play("Attack", 0, 0f);
        }

        private void SyncAnimationState(bool force)
        {
            if (animator == null) { ResolveAnimator(); if (animator == null) return; }
            if (animator.runtimeAnimatorController == null) return;

            int desired = ComputeDesiredState();
            if (desired != StateAttack && !Mathf.Approximately(animator.speed, 1f))
            {
                animator.speed = 1f;
            }

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

            if (_ai.IsFiring) return StateAttack;
            if (_ai.CurrentState == EnemyRangedAI.RangedAIState.Idle || _ai.CurrentState == EnemyRangedAI.RangedAIState.Attack)
            {
                return StateIdle;
            }

            if (_agent == null || _agent.isStopped) return StateIdle;
            bool moving = _agent.velocity.sqrMagnitude >= walkVelocityThreshold * walkVelocityThreshold
                       || (_agent.hasPath && _agent.remainingDistance > _agent.stoppingDistance + 0.35f);
            return moving ? StateWalk : StateIdle;
        }

        private void CacheAttackClipLength()
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;
            foreach (var clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip != null && clip.name == "Attack" && clip.length > 0.05f)
                {
                    attackClipLength = clip.length;
                    return;
                }
            }
        }
    }
}
