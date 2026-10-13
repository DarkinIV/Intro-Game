/*
    Used to tell combat feedback what kind of damage was dealt.

    Existing attacks remain Physical because CharacterStats.TakeDamage()
    defaults to DamageType.Physical.
*/
public enum DamageType
{
    Physical,
    Magic
}
