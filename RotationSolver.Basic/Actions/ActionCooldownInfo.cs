﻿using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using static Dalamud.Interface.Utility.Raii.ImRaii;

namespace RotationSolver.Basic.Actions;
public readonly struct ActionCooldownInfo
{
    private readonly IBaseActionNew _action;
    public byte CoolDownGroup { get; }

    unsafe RecastDetail* CoolDownDetail => ActionManager.Instance()->GetRecastGroupDetail(CoolDownGroup - 1);

    private unsafe float RecastTime => CoolDownDetail == null ? 0 : CoolDownDetail->Total;

    /// <summary>
    /// 
    /// </summary>
    public float RecastTimeElapsed => RecastTimeElapsedRaw - DataCenter.WeaponElapsed;

    /// <summary>
    /// 
    /// </summary>
    unsafe float RecastTimeElapsedRaw => CoolDownDetail == null ? 0 : CoolDownDetail->Elapsed;

    /// <summary>
    /// 
    /// </summary>
    public unsafe bool IsCoolingDown => CoolDownDetail != null && CoolDownDetail->IsActive != 0;

    private float RecastTimeRemain => RecastTime - RecastTimeElapsedRaw;

    /// <summary>
    /// 
    /// </summary>
    public unsafe ushort MaxCharges => Math.Max(ActionManager.GetMaxCharges(_action.Info.AdjustedID, (uint)Player.Level), (ushort)1);

    /// <summary>
    /// 
    /// </summary>
    public bool HasOneCharge => !IsCoolingDown || RecastTimeElapsedRaw >= RecastTimeOneChargeRaw;

    /// <summary>
    /// 
    /// </summary>
    public ushort CurrentCharges => IsCoolingDown ? (ushort)(RecastTimeElapsedRaw / RecastTimeOneChargeRaw) : MaxCharges;

    private float RecastTimeOneChargeRaw => ActionManager.GetAdjustedRecastTime(ActionType.Action, _action.Info.AdjustedID) / 1000f;

    /// <summary>
    /// 
    /// </summary>
    public float RecastTimeRemainOneCharge => RecastTimeRemainOneChargeRaw - DataCenter.WeaponRemain;

    float RecastTimeRemainOneChargeRaw => RecastTimeRemain % RecastTimeOneChargeRaw;

    /// <summary>
    /// 
    /// </summary>
    public float RecastTimeElapsedOneCharge => RecastTimeElapsedOneChargeRaw - DataCenter.WeaponElapsed;

    float RecastTimeElapsedOneChargeRaw => RecastTimeElapsedRaw % RecastTimeOneChargeRaw;


    public ActionCooldownInfo(IBaseActionNew action)
    {
        _action = action;
        CoolDownGroup = _action.Action.GetCoolDownGroup();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="gcdCount"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    public bool ElapsedOneChargeAfterGCD(uint gcdCount = 0, float offset = 0)
        => ElapsedOneChargeAfter(DataCenter.GCDTime(gcdCount, offset));

    /// <summary>
    /// 
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    public bool ElapsedOneChargeAfter(float time)
        => IsCoolingDown && time <= RecastTimeElapsedOneCharge;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="gcdCount"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    public bool ElapsedAfterGCD(uint gcdCount = 0, float offset = 0)
        => ElapsedAfter(DataCenter.GCDTime(gcdCount, offset));

    /// <summary>
    /// 
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    public bool ElapsedAfter(float time)
        => IsCoolingDown && time <= RecastTimeElapsed;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="gcdCount"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    public bool WillHaveOneChargeGCD(uint gcdCount = 0, float offset = 0)
        => WillHaveOneCharge(DataCenter.GCDTime(gcdCount, offset));

    /// <summary>
    /// 
    /// </summary>
    /// <param name="remain"></param>
    /// <returns></returns>
    public bool WillHaveOneCharge(float remain)
        => HasOneCharge || RecastTimeRemainOneCharge <= remain;

    internal bool CooldownCheck(bool isEmpty, bool onLastAbility, bool ignoreClippingCheck, byte gcdCountForAbility)
    {
        if (!_action.Info.IsGeneralGCD)
        {
            if (IsCoolingDown)
            {
                if (_action.Info.IsRealGCD)
                {
                    if (!WillHaveOneChargeGCD(0, 0)) return false;
                }
                else
                {
                    if (!HasOneCharge && RecastTimeRemainOneChargeRaw > DataCenter.ActionRemain) return false;
                }
            }

            if (!isEmpty)
            {
                if (RecastTimeRemain > DataCenter.WeaponRemain + DataCenter.WeaponTotal * gcdCountForAbility)
                    return false;
            }
        }

        if (!_action.Info.IsRealGCD)
        {
            if (onLastAbility)
            {
                if (DataCenter.NextAbilityToNextGCD > _action.Info.AnimationLockTime + DataCenter.Ping + DataCenter.MinAnimationLock) return false;
            }
            else if (!ignoreClippingCheck)
            {
                if (DataCenter.NextAbilityToNextGCD < _action.Info.AnimationLockTime) return false;
            }
        }

        return true;
    }
}
