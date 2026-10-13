public enum StatRank { D, C, B, A, S }

public static class StatRankExtensions
{
    // Level 1 starting values per rank
    public static int StartVitality(this StatRank r) => r switch
    {
        StatRank.S => 140,
        StatRank.A => 120,
        StatRank.B => 100,
        StatRank.C => 80,
        _         => 60
    };
    public static int StartEnergy(this StatRank r) => r switch
    {
        StatRank.S => 130,
        StatRank.A => 110,
        StatRank.B => 90,
        StatRank.C => 75,
        _         => 60
    };
    public static int StartStrength(this StatRank r) => r switch
    {
        StatRank.S => 18,
        StatRank.A => 14,
        StatRank.B => 11,
        StatRank.C => 8,
        _         => 6
    };
    public static int StartMagic(this StatRank r) => r switch
    {
        StatRank.S => 18,
        StatRank.A => 14,
        StatRank.B => 11,
        StatRank.C => 8,
        _         => 6
    };
    public static float StartAttackSpeed(this StatRank r) => r switch
    {
        StatRank.S => 1.60f,
        StatRank.A => 1.35f,
        StatRank.B => 1.10f,
        StatRank.C => 0.90f,
        _         => 0.75f
    };
    public static int StartPace(this StatRank r) => r switch
    {
        StatRank.S => 8,
        StatRank.A => 7,
        StatRank.B => 6,
        StatRank.C => 5,
        _         => 4
    };

    // Per-level GAIN per rank – HIGHER rank = MORE scaling
    public static int LevelVitality(this StatRank r) => r switch
    {
        StatRank.S => 12,
        StatRank.A => 10,
        StatRank.B => 8,
        StatRank.C => 6,
        _         => 4
    };
    public static int LevelEnergy(this StatRank r) => r switch
    {
        StatRank.S => 10,
        StatRank.A => 8,
        StatRank.B => 6,
        StatRank.C => 5,
        _         => 3
    };
    public static int LevelStrength(this StatRank r) => r switch
    {
        StatRank.S => 5,
        StatRank.A => 4,
        StatRank.B => 3,
        StatRank.C => 2,
        _         => 1
    };
    public static int LevelMagic(this StatRank r) => r switch
    {
        StatRank.S => 5,
        StatRank.A => 4,
        StatRank.B => 3,
        StatRank.C => 2,
        _         => 1
    };
    public static float LevelAttackSpeed(this StatRank r) => r switch
    {
        StatRank.S => 0.04f,
        StatRank.A => 0.03f,
        StatRank.B => 0.02f,
        StatRank.C => 0.015f,
        _         => 0.01f
    };
    public static float LevelPace(this StatRank r) => r switch
    {
        StatRank.S => 2f,
        StatRank.A => 1.5f,
        StatRank.B => 1f,
        StatRank.C => 0,
        _         => 0
    };
}