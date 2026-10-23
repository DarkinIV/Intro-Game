public enum StatRank { D, C, B, A, S }

public static class StatRankExtensions
{
    // Starting stats at level 1
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
        StatRank.S => 14,
        StatRank.A => 12,
        StatRank.B => 10,
        StatRank.C => 6,
        _         => 4
    };
    public static int StartMagic(this StatRank r) => r switch
    {
        StatRank.S => 12,
        StatRank.A => 8,
        StatRank.B => 4,
        StatRank.C => 0,
        _         => 0
    };
    public static float StartAttackSpeed(this StatRank r) => r switch
    {
        StatRank.S => 1.30f,
        StatRank.A => 1.15f,
        StatRank.B => 1f,
        StatRank.C => 0.95f,
        _         => 0.85f
    };
    public static float StartPace(this StatRank r) => r switch
    {
        StatRank.S => 7f,
        StatRank.A => 6.5f,
        StatRank.B => 6f,
        StatRank.C => 5f,
        _         => 4f
    };

    // How much each stat goes up every level
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
        StatRank.S => 6,
        StatRank.A => 5,
        StatRank.B => 4,
        StatRank.C => 3,
        _         => 2
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
        StatRank.S => 4,
        StatRank.A => 3,
        StatRank.B => 1,
        StatRank.C => 0,
        _         => 0
    };
    public static float LevelAttackSpeed(this StatRank r) => r switch
    {
        StatRank.S => 0.02f,
        StatRank.A => 0.01f,
        StatRank.B => 0f,
        StatRank.C => 0f,
        _         => 0f
    };
    public static float LevelPace(this StatRank r) => r switch
    {
        StatRank.S => 2f,
        StatRank.A => 1.5f,
        StatRank.B => 1f,
        StatRank.C => 0,
        _         => 0
    };

    // Agility, for jumping and wall moves. These don't change with level.
    public static int AirJumps(this StatRank r) => r switch
    {
        StatRank.S => 1,
        StatRank.A => 1,
        _         => 0
    };
    public static float JumpHeightMultiplier(this StatRank r) => r switch
    {
        StatRank.S => 1.15f,
        StatRank.A => 1.07f,
        _         => 1f
    };
    public static float WallTimeMultiplier(this StatRank r) => r switch
    {
        StatRank.S => 1.5f,
        StatRank.A => 1.3f,
        StatRank.B => 1f,
        StatRank.C => 0.85f,
        _         => 0.7f
    };
    // Lower is better here, it multiplies the stamina cost of wall moves and slides
    public static float TraversalStaminaMultiplier(this StatRank r) => r switch
    {
        StatRank.S => 0.5f,
        StatRank.A => 0.7f,
        StatRank.B => 1f,
        StatRank.C => 1.2f,
        _         => 1.4f
    };
}