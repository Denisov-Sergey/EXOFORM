using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PandemicWars.Scripts.Ecs.Components.UnitComponents
{
    /// <summary>
    /// Компонент для управления анимациями юнита
    /// </summary>
    public class UnitAnimationComponent : IComponentData
    {
        [Header("Animator Reference")]
        public Animator Animator;

        [Header("Animation States")]
        public UnitAnimationState CurrentState = UnitAnimationState.Idle;
        public UnitAnimationState PreviousState = UnitAnimationState.Idle;
        public float StateChangeTime;

        [Header("Animation Names")]
        public string IdleAnimationName = "Idle";
        public string MoveAnimationName = "Move";
        public string AttackAnimationName = "Attack";
        public string DeathAnimationName = "Death";
        public string SelectionAnimationName = "Selected";

        [Header("Animator Parameters")]
        public string SpeedParameterName = "Speed";
        public string IsMovingParameterName = "IsMoving";
        public string IsSelectedParameterName = "IsSelected";
        public string TriggerAttackParameterName = "Attack";
        public string TriggerDeathParameterName = "Death";

        [Header("Animation Settings")]
        public float TransitionSpeed = 0.2f;
        public bool UseRootMotion = false;
        public float AnimationSpeedMultiplier = 1f;

        [Header("Internal State")]
        public float3 PreviousPosition;
        public bool HasPreviousPosition;
        public bool WasSelected;
        public float LastAnimationTime;
    }

    /// <summary>
    /// Состояния анимации юнита
    /// </summary>
    public enum UnitAnimationState : byte
    {
        Idle = 0,
        Moving = 1,
        Attacking = 2,
        Dead = 3,
        Stunned = 4,
        Celebrating = 5
    }

    /// <summary>
    /// Компонент для настройки анимационных триггеров
    /// </summary>
    public struct AnimationTriggerComponent : IComponentData
    {
        public bool TriggerAttack;
        public bool TriggerDeath;
        public bool TriggerSelection;
        public bool TriggerHit;
        public float TriggerTime;
    }

    /// <summary>
    /// Компонент для синхронизации скорости анимации с движением
    /// </summary>
    public struct AnimationSpeedSyncComponent : IComponentData
    {
        public bool SyncMoveSpeedWithAnimation;
        public float BaseAnimationSpeed;
        public float CurrentAnimationSpeed;
        public float SpeedSmoothTime;
        public float SpeedVelocity; // Для SmoothDamp
    }
}