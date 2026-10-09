using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace MechMod
{
    /// <summary>
    /// Entry point. ThunderRoad discovers ThunderScript subclasses in mod DLLs automatically,
    /// so no catalog JSON is needed. Each frame this keeps the suit's stat modifiers in sync
    /// with <see cref="MechOptions"/> and drives the thruster / slam / fist abilities.
    /// </summary>
    public class MechSuitScript : ThunderScript
    {
        const float ToggleHoldTime = 1f;
        const float SlamMinFallSpeed = 9f;
        const float PunchMinSpeed = 4.5f;
        const float PunchRadius = 0.18f;
        const float PunchCooldown = 0.35f;

        Creature suitedCreature;
        bool applied;
        bool fallDamageBefore;
        float toggleHeldFor;
        float peakFallSpeed;
        bool wasGrounded = true;
        readonly Dictionary<Side, float> nextPunchTime = new Dictionary<Side, float> { { Side.Left, 0f }, { Side.Right, 0f } };
        readonly Collider[] overlapBuffer = new Collider[64];
        readonly HashSet<Creature> hitCreatures = new HashSet<Creature>();
        readonly HashSet<Rigidbody> hitBodies = new HashSet<Rigidbody>();

        public override void ScriptDisable()
        {
            base.ScriptDisable();
            RemoveSuit();
        }

        public override void ScriptUpdate()
        {
            base.ScriptUpdate();

            Creature player = Player.local != null ? Player.local.creature : null;
            if (player == null || player.isKilled)
            {
                RemoveSuit();
                return;
            }

            HandleToggleGesture(player);

            if (!MechOptions.SuitActive)
            {
                RemoveSuit();
                return;
            }

            // Re-apply when the player body changes (level load, respawn) or options change.
            ApplySuit(player);
            Regenerate(player);
            if (MechOptions.FistsEnabled)
            {
                PowerFist(player, Side.Left);
                PowerFist(player, Side.Right);
            }
            if (MechOptions.SlamEnabled) TrackGroundSlam(player);
        }

        public override void ScriptFixedUpdate()
        {
            base.ScriptFixedUpdate();
            if (!applied || !MechOptions.ThrustersEnabled || suitedCreature == null) return;
            Thrusters(suitedCreature);
        }

        // ---------------------------------------------------------------- suit state

        void HandleToggleGesture(Creature player)
        {
            var left = player.GetHand(Side.Left)?.playerHand?.controlHand;
            var right = player.GetHand(Side.Right)?.playerHand?.controlHand;
            bool held = left != null && right != null && left.alternateUsePressed && right.alternateUsePressed;

            if (!held)
            {
                toggleHeldFor = 0f;
                return;
            }
            if (toggleHeldFor < 0f) return; // already toggled during this hold

            toggleHeldFor += Time.deltaTime;
            if (toggleHeldFor >= ToggleHoldTime)
            {
                MechOptions.SuitActive = !MechOptions.SuitActive;
                toggleHeldFor = -1f;
                Haptic(player, Side.Left, 1f);
                Haptic(player, Side.Right, 1f);
                Debug.Log(MechOptions.SuitActive ? "[MechSuit] Online" : "[MechSuit] Offline");
            }
        }

        void ApplySuit(Creature player)
        {
            if (applied && suitedCreature != player) RemoveSuit();

            if (!applied)
            {
                fallDamageBefore = Player.fallDamage;
                player.OnDamageEvent += OnSuitedCreatureDamaged;
                suitedCreature = player;
                applied = true;
                wasGrounded = true;
                peakFallSpeed = 0f;
            }

            // Speed modifiers are keyed by handler, so calling this every frame just refreshes
            // the values and picks up option changes made in the book menu.
            Player.local.locomotion.SetSpeedModifier(this,
                MechOptions.SpeedMultiplier,  // forward
                MechOptions.SpeedMultiplier,  // backward
                MechOptions.SpeedMultiplier,  // strafe
                MechOptions.SpeedMultiplier,  // running
                MechOptions.JumpMultiplier);  // jump

            Player.fallDamage = !MechOptions.NoFallDamage && fallDamageBefore;
        }

        void RemoveSuit()
        {
            if (!applied) return;
            applied = false;

            if (Player.local != null && Player.local.locomotion != null)
                Player.local.locomotion.RemoveSpeedModifier(this);
            Player.fallDamage = fallDamageBefore;
            if (suitedCreature != null) suitedCreature.OnDamageEvent -= OnSuitedCreatureDamaged;
            suitedCreature = null;
        }

        void OnSuitedCreatureDamaged(CollisionInstance collision, EventTime time)
        {
            // Armor plating: give back a share of every hit once it has landed.
            if (time != EventTime.OnEnd || suitedCreature == null || suitedCreature.isKilled) return;
            float absorbed = collision.damageStruct.damage * MechOptions.DamageReduction;
            if (absorbed > 0f) suitedCreature.Heal(absorbed, suitedCreature);
        }

        void Regenerate(Creature player)
        {
            if (MechOptions.RegenPerSecond <= 0f || player.currentHealth >= player.maxHealth) return;
            player.Heal(MechOptions.RegenPerSecond * Time.deltaTime, player);
        }

        // ---------------------------------------------------------------- thrusters

        void Thrusters(Creature player)
        {
            var body = Player.local.locomotion.physicBody;
            if (body == null) return;

            foreach (Side side in new[] { Side.Left, Side.Right })
            {
                RagdollHand hand = player.GetHand(side);
                var control = hand?.playerHand?.controlHand;
                if (control == null || !control.usePressed || hand.grabbedHandle != null) continue;

                // Thrust pushes opposite to where the palm faces, so aim palms at the ground to rise.
                Vector3 palmDirection = hand.PalmDir;
                if (Vector3.Dot(palmDirection, Vector3.down) < 0.3f) continue;

                body.AddForce(-palmDirection * MechOptions.ThrustForce, ForceMode.Acceleration);
                control.HapticShort(0.3f);
            }
        }

        // ---------------------------------------------------------------- ground slam

        void TrackGroundSlam(Creature player)
        {
            var locomotion = Player.local.locomotion;
            bool grounded = locomotion.isGrounded;

            if (!grounded && locomotion.physicBody != null)
                peakFallSpeed = Mathf.Max(peakFallSpeed, -locomotion.physicBody.velocity.y);

            if (grounded && !wasGrounded && peakFallSpeed >= SlamMinFallSpeed)
            {
                // Harder landings hit harder, capped at double strength.
                float power = Mathf.Clamp(peakFallSpeed / SlamMinFallSpeed, 1f, 2f);
                Shockwave(player, player.transform.position, MechOptions.SlamRadius * power, MechOptions.SlamDamage * power, 25f * power, true);
                Haptic(player, Side.Left, 1f);
                Haptic(player, Side.Right, 1f);
            }

            if (grounded) peakFallSpeed = 0f;
            wasGrounded = grounded;
        }

        // ---------------------------------------------------------------- power fists

        void PowerFist(Creature player, Side side)
        {
            if (Time.time < nextPunchTime[side]) return;

            RagdollHand hand = player.GetHand(side);
            if (hand == null || hand.grabbedHandle != null || hand.physicBody == null) return;

            Vector3 velocity = hand.physicBody.velocity - Player.local.locomotion.physicBody.velocity;
            if (velocity.magnitude < PunchMinSpeed) return;

            Vector3 fist = hand.transform.position;
            int count = Physics.OverlapSphereNonAlloc(fist, PunchRadius, overlapBuffer, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Creature target = overlapBuffer[i].GetComponentInParent<Creature>();
                if (target == null || target == player || target.isKilled) continue;

                float strength = velocity.magnitude / PunchMinSpeed;
                DamageCreature(target, MechOptions.PunchDamage * strength);
                target.ragdoll.SetState(Ragdoll.State.Destabilized);
                foreach (RagdollPart part in target.ragdoll.parts)
                    part.physicBody?.AddForce(velocity.normalized * 12f * strength, ForceMode.VelocityChange);

                hand.playerHand.controlHand.HapticShort(1f);
                nextPunchTime[side] = Time.time + PunchCooldown;
                return;
            }
        }

        // ---------------------------------------------------------------- shared

        void Shockwave(Creature source, Vector3 origin, float radius, float damage, float force, bool destabilize)
        {
            hitCreatures.Clear();
            hitBodies.Clear();
            int count = Physics.OverlapSphereNonAlloc(origin, radius, overlapBuffer, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider collider = overlapBuffer[i];
                if (collider.transform.IsChildOf(Player.local.transform)) continue;

                Creature creature = collider.GetComponentInParent<Creature>();
                if (creature == source) continue;
                if (creature != null && !creature.isKilled && hitCreatures.Add(creature))
                {
                    float falloff = 1f - Mathf.Clamp01(Vector3.Distance(origin, creature.transform.position) / radius);
                    DamageCreature(creature, damage * Mathf.Max(falloff, 0.25f));
                    if (destabilize) creature.ragdoll.SetState(Ragdoll.State.Destabilized);
                }

                Rigidbody rb = collider.attachedRigidbody;
                if (rb != null && !rb.isKinematic && hitBodies.Add(rb))
                    rb.AddExplosionForce(force, origin, radius, 1.5f, ForceMode.VelocityChange);
            }
        }

        static void DamageCreature(Creature creature, float damage)
        {
            if (damage <= 0f) return;
            creature.Damage(new CollisionInstance(new DamageStruct(DamageType.Blunt, damage)));
        }

        static void Haptic(Creature player, Side side, float intensity)
        {
            player.GetHand(side)?.playerHand?.controlHand?.HapticShort(intensity);
        }
    }
}
