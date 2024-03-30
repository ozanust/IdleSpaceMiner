public class Unit
{
	public UnitType Type;
	public int Level;
	public int Health;
	public int AttackPower;
	public float AttackSpeed;

	public Unit(int level, int firePower, int health, float attackSpeed, UnitType type)
	{
		Level = level;
		AttackPower = firePower;
		Health = health;
		AttackSpeed = attackSpeed;
		Type = type;
	}
}
