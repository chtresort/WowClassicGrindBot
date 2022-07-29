﻿namespace Core
{
    public enum UI_ERROR
    {
        NONE = 0,
        ERR_BADATTACKFACING = 1,
        ERR_SPELL_FAILED_S = 2,
        ERR_SPELL_OUT_OF_RANGE = 3,
        ERR_BADATTACKPOS = 4,
        ERR_AUTOFOLLOW_TOO_FAR = 5,
        SPELL_FAILED_MOVING = 6,
        ERR_SPELL_COOLDOWN = 7,
        ERR_SPELL_FAILED_ANOTHER_IN_PROGRESS = 8,
        ERR_SPELL_FAILED_STUNNED = 9,
        ERR_SPELL_FAILED_INTERRUPTED = 10,
        SPELL_FAILED_ITEM_NOT_READY = 11,
        SPELL_FAILED_TRY_AGAIN = 12,
        SPELL_FAILED_NOT_READY = 13,
        SPELL_FAILED_TARGETS_DEAD = 14,
        ERR_LOOT_LOCKED = 15,
        ERR_ATTACK_PACIFIED = 16,

        MAX_ERROR_RANGE = 2000,

        CAST_START = 999998,
        CAST_SUCCESS = 999999
    }

    public static class UI_ERROR_Extensions
    {
        public static string ToStringF(this UI_ERROR value) => value switch
        {
            UI_ERROR.ERR_BADATTACKFACING => nameof(UI_ERROR.ERR_BADATTACKFACING),
            UI_ERROR.ERR_SPELL_FAILED_S => nameof(UI_ERROR.ERR_SPELL_FAILED_S),
            UI_ERROR.ERR_SPELL_OUT_OF_RANGE => nameof(UI_ERROR.ERR_SPELL_OUT_OF_RANGE),
            UI_ERROR.ERR_BADATTACKPOS => nameof(UI_ERROR.ERR_BADATTACKPOS),
            UI_ERROR.ERR_AUTOFOLLOW_TOO_FAR => nameof(UI_ERROR.ERR_AUTOFOLLOW_TOO_FAR),
            UI_ERROR.SPELL_FAILED_MOVING => nameof(UI_ERROR.SPELL_FAILED_MOVING),
            UI_ERROR.ERR_SPELL_COOLDOWN => nameof(UI_ERROR.ERR_SPELL_COOLDOWN),
            UI_ERROR.ERR_SPELL_FAILED_ANOTHER_IN_PROGRESS => nameof(UI_ERROR.ERR_SPELL_FAILED_ANOTHER_IN_PROGRESS),
            UI_ERROR.ERR_SPELL_FAILED_STUNNED => nameof(UI_ERROR.ERR_SPELL_FAILED_STUNNED),
            UI_ERROR.ERR_SPELL_FAILED_INTERRUPTED => nameof(UI_ERROR.ERR_SPELL_FAILED_INTERRUPTED),
            UI_ERROR.SPELL_FAILED_ITEM_NOT_READY => nameof(UI_ERROR.SPELL_FAILED_ITEM_NOT_READY),
            UI_ERROR.SPELL_FAILED_TRY_AGAIN => nameof(UI_ERROR.SPELL_FAILED_TRY_AGAIN),
            UI_ERROR.SPELL_FAILED_NOT_READY => nameof(UI_ERROR.SPELL_FAILED_NOT_READY),
            UI_ERROR.SPELL_FAILED_TARGETS_DEAD => nameof(UI_ERROR.SPELL_FAILED_TARGETS_DEAD),
            UI_ERROR.ERR_LOOT_LOCKED => nameof(UI_ERROR.ERR_LOOT_LOCKED),
            UI_ERROR.ERR_ATTACK_PACIFIED => nameof(UI_ERROR.ERR_ATTACK_PACIFIED),
            UI_ERROR.MAX_ERROR_RANGE => nameof(UI_ERROR.MAX_ERROR_RANGE),
            UI_ERROR.CAST_START => nameof(UI_ERROR.CAST_START),
            UI_ERROR.CAST_SUCCESS => nameof(UI_ERROR.CAST_SUCCESS),
            UI_ERROR.NONE => nameof(UI_ERROR.NONE),
            _ => nameof(UI_ERROR.NONE)
        };
    }
}