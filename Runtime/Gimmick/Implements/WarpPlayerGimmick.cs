using System;
using UnityEngine;

namespace ClusterVR.CreatorKit.Gimmick.Implements
{
    public class WarpPlayerGimmick : MonoBehaviour, IWarpPlayerGimmick
    {
        [SerializeField, PlayerGimmickKey] GimmickKey key = new GimmickKey(Target.Player);
        [SerializeField] Transform targetTransform;
        [SerializeField] bool keepPosition;
        [SerializeField] bool keepRotation;

        Target IGimmick.Target => key.Target;
        string IGimmick.Key => key.Key;
        ParameterType IGimmick.ParameterType => ParameterType.Signal;

        public event PlayerGimmickEventHandler OnRun;
        public Vector3 TargetPosition => targetTransform.position;
        public Quaternion TargetRotation => targetTransform.rotation;
        public bool KeepPosition => keepPosition;
        public bool KeepRotation => keepRotation;

        DateTime lastTriggeredAt;

        public void Run(GimmickValue value, DateTime current)
        {
            if (targetTransform == null) return;
            if (value.TimeStamp <= lastTriggeredAt) return;
            lastTriggeredAt = value.TimeStamp;
            if ((current - value.TimeStamp).TotalSeconds > Constants.Gimmick.TriggerExpireSeconds) return;
            OnRun?.Invoke(this);
        }
    }
}