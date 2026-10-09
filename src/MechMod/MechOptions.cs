using ThunderRoad;

namespace MechMod
{
    /// <summary>
    /// Settings exposed in the in-game Mod Options menu (Book → Mods → Mech Suit).
    /// </summary>
    public static class MechOptions
    {
        const string Core = "Mech Core";
        const string Thrusters = "Thrusters";
        const string Slam = "Ground Slam";
        const string Fists = "Power Fists";

        [ModOption(name = "Mech Suit Active", tooltip = "Toggle the suit. You can also hold both Alt-Use buttons for one second.", category = Core, order = 0)]
        public static bool SuitActive = false;

        [ModOption(name = "Move Speed", tooltip = "Walking/running speed multiplier while suited.", category = Core, valueSourceName = nameof(Multipliers), defaultValueIndex = 4, order = 1)]
        public static float SpeedMultiplier = 1.4f;

        [ModOption(name = "Jump Power", tooltip = "Jump force multiplier while suited.", category = Core, valueSourceName = nameof(Multipliers), defaultValueIndex = 10, order = 2)]
        public static float JumpMultiplier = 2f;

        [ModOption(name = "Armor", tooltip = "Fraction of incoming damage the plating absorbs.", category = Core, valueSourceName = nameof(Fractions), defaultValueIndex = 6, order = 3)]
        public static float DamageReduction = 0.6f;

        [ModOption(name = "Hull Repair / sec", tooltip = "Health regenerated per second while suited.", category = Core, valueSourceName = nameof(RegenRates), defaultValueIndex = 2, order = 4)]
        public static float RegenPerSecond = 2f;

        [ModOption(name = "No Fall Damage", tooltip = "Hydraulic legs absorb landings.", category = Core, order = 5)]
        public static bool NoFallDamage = true;

        [ModOption(name = "Thrusters Enabled", tooltip = "Hold Trigger with an empty, downward-pointing hand to fly.", category = Thrusters, order = 10)]
        public static bool ThrustersEnabled = true;

        [ModOption(name = "Thrust Force", category = Thrusters, valueSourceName = nameof(Forces), defaultValueIndex = 3, order = 11)]
        public static float ThrustForce = 40f;

        [ModOption(name = "Ground Slam Enabled", tooltip = "Fall fast and hit the ground to unleash a shockwave.", category = Slam, order = 20)]
        public static bool SlamEnabled = true;

        [ModOption(name = "Slam Radius", category = Slam, valueSourceName = nameof(Radii), defaultValueIndex = 3, order = 21)]
        public static float SlamRadius = 6f;

        [ModOption(name = "Slam Damage", category = Slam, valueSourceName = nameof(Damages), defaultValueIndex = 3, order = 22)]
        public static float SlamDamage = 30f;

        [ModOption(name = "Power Fists Enabled", tooltip = "Fast empty-handed punches send enemies flying.", category = Fists, order = 30)]
        public static bool FistsEnabled = true;

        [ModOption(name = "Punch Damage", category = Fists, valueSourceName = nameof(Damages), defaultValueIndex = 2, order = 31)]
        public static float PunchDamage = 20f;

        public static ModOptionFloat[] Multipliers() => Range(1f, 4f, 0.1f);
        public static ModOptionFloat[] Fractions() => Range(0f, 0.9f, 0.1f);
        public static ModOptionFloat[] RegenRates() => Range(0f, 10f, 1f);
        public static ModOptionFloat[] Forces() => Range(10f, 100f, 10f);
        public static ModOptionFloat[] Radii() => Range(3f, 12f, 1f);
        public static ModOptionFloat[] Damages() => Range(0f, 100f, 10f);

        static ModOptionFloat[] Range(float min, float max, float step)
        {
            int count = (int)System.Math.Round((max - min) / step) + 1;
            var values = new ModOptionFloat[count];
            for (int i = 0; i < count; i++)
            {
                float value = (float)System.Math.Round(min + step * i, 2);
                values[i] = new ModOptionFloat(value.ToString("0.##"), value);
            }
            return values;
        }
    }
}
