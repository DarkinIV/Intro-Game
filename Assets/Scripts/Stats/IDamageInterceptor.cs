// Anything that wants to change incoming damage (like the Knight's Unkillable) uses this.
// Return how much damage should actually be taken.
public interface IDamageInterceptor
{
    int InterceptDamage(int amount, DamageType damageType, CharacterCombat attacker);
}
