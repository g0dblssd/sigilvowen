using System.Collections.Generic;
using Godot;

namespace Sigilwoven;

public partial class SkillCaster : Node
{
    public class Slot
    {
        public SkillData Skill;
        public List<SkillLinkData> Links = new();
        public Slot(SkillData skill, List<SkillLinkData> links)
        {
            Skill = skill;
            Links = links;
        }
    }

    public List<Slot> Slots = new();
    public int SelectedSlot;
    private float[] _cooldownLeft = System.Array.Empty<float>();
    private Node3D? _ownerPlayer;
    private float _primaryCooldown;

    public void Setup(Node3D player)
    {
        _ownerPlayer = player;
    }

    public override void _Ready()
    {
        HeroClass heroClass = _ownerPlayer is PlayerController player ? player.Progression.HeroClass : HeroClass.Runeblade;
        ConfigureClass(heroClass);
    }

    public void ConfigureClass(HeroClass heroClass)
    {
        string[] loadout = heroClass switch
        {
            HeroClass.Aetherist => new[] { "fireball", "frostbolt", "chain_lightning", "void_lance", "gravity_well", "meteor", "ice_nova", "arcane_echo", "flame_dash", "lightning_elemental" },
            HeroClass.Warden => new[] { "split_arrow", "venom_fang", "stone_shot", "venom_knives", "toxic_cloud", "plague_nova", "shadow_step", "frost_armor", "dread_hound", "haste_aura" },
            HeroClass.Berserker => new[] { "war_cry", "seismic_rage", "executioner_leap", "rune_cleave", "earthquake", "blood_lunge", "stone_skin", "blade_vortex", "firestorm", "haste_aura" },
            HeroClass.Necromancer => new[] { "bone_spear", "raise_legion", "corpse_bloom", "dread_hound", "bone_golem", "plague_nova", "toxic_cloud", "frost_armor", "shadow_step", "aether_pulse" },
            HeroClass.Shadowstalker => new[] { "fan_of_knives", "smoke_bomb", "death_mark", "shadow_step", "venom_knives", "venom_fang", "toxic_cloud", "haste_aura", "frost_spider", "plague_nova" },
            HeroClass.Templar => new[] { "holy_smite", "consecrated_ground", "judgment_bell", "stone_skin", "chain_lightning", "earthquake", "frost_armor", "lightning_elemental", "flame_dash", "haste_aura" },
            _ => new[] { "rune_cleave", "blade_vortex", "blood_lunge", "earthquake", "stone_skin", "flame_dash", "firestorm", "bone_golem", "frost_armor", "haste_aura" },
        };
        Slots.Clear();
        foreach (string id in loadout)
        {
            SkillData? skill = SkillCatalog.ById(id);
            if (skill != null) Slots.Add(new Slot(skill, new List<SkillLinkData>()));
        }
        // Keep one visible example of the Link system in every class loadout.
        if (Slots.Count > 0)
        {
            SkillLinkData? starter = LinkCatalog.ById(heroClass is HeroClass.Warden or HeroClass.Shadowstalker or HeroClass.Necromancer ? "venom_seal" : heroClass == HeroClass.Aetherist ? "chain_extension" : "stun_impacts");
            if (starter != null)
            {
                Slots[0].Links.Add(starter);
            }
        }
        _cooldownLeft = new float[Slots.Count];
        SelectedSlot = 0;
    }

    public int GetLinkCharges(SkillData skill)
    {
        int used = 0;
        foreach (Slot slot in Slots)
        {
            if (slot.Skill.Id == skill.Id) used += slot.Links.Count;
        }
        return Mathf.Max(0, SkillData.MaxLinkCharges - used);
    }

    public bool TryBindSlot(int slotIndex, SkillData skill, List<SkillLinkData> links, out string error)
    {
        error = "";
        if (slotIndex < 0 || slotIndex >= 10)
        {
            error = "Invalid hotbar slot.";
            return false;
        }
        if (_ownerPlayer is PlayerController player)
        {
            if (!skill.IsAllowedFor(player.Progression.HeroClass))
            {
                error = $"{skill.DisplayName} belongs to {skill.RequiredClass}.";
                return false;
            }
            if (!player.Progression.IsSkillUnlocked(skill.Id))
            {
                error = $"{skill.DisplayName} unlocks at level {player.Progression.GetSkillRequiredLevel(skill.Id)}.";
                return false;
            }
        }
        if (links.Count > SkillData.MaxLinkCharges)
        {
            error = $"Only {SkillData.MaxLinkCharges} links can be socketed into one skill.";
            return false;
        }
        foreach (SkillLinkData link in links)
        {
            string? incompatibility = LinkSystem.GetIncompatibilityReason(skill, link);
            if (incompatibility != null)
            {
                error = incompatibility;
                return false;
            }
        }
        int usedElsewhere = 0;
        for (int i = 0; i < Slots.Count; i++)
        {
            if (i != slotIndex && Slots[i].Skill.Id == skill.Id) usedElsewhere += Slots[i].Links.Count;
        }
        if (usedElsewhere + links.Count > SkillData.MaxLinkCharges)
        {
            error = $"{skill.DisplayName} has only {Mathf.Max(0, SkillData.MaxLinkCharges - usedElsewhere)} mix charges available.";
            return false;
        }
        while (Slots.Count <= slotIndex) Slots.Add(new Slot(skill, new List<SkillLinkData>()));
        Slots[slotIndex] = new Slot(skill, new List<SkillLinkData>(links));
        if (_cooldownLeft.Length < Slots.Count) System.Array.Resize(ref _cooldownLeft, Slots.Count);
        _cooldownLeft[slotIndex] = 0f;
        SelectedSlot = slotIndex;
        return true;
    }

    public override void _Process(double delta)
    {
        _primaryCooldown = Mathf.Max(0f, _primaryCooldown - (float)delta);
        for (int i = 0; i < _cooldownLeft.Length; i++)
        {
            if (_cooldownLeft[i] > 0f)
            {
                _cooldownLeft[i] -= (float)delta;
            }
        }
    }

    public string GetSelectedName()
    {
        if (Slots.Count == 0)
        {
            return "-";
        }
        var s = Slots[SelectedSlot];
        float cooldown = _cooldownLeft.Length > SelectedSlot ? Mathf.Max(0f, _cooldownLeft[SelectedSlot]) : 0f;
        string cooldownText = cooldown > 0f ? $" CD {cooldown:0.0}s" : " READY";
        return $"[{SelectedSlot + 1}] {s.Skill.DisplayName} | {s.Skill.ManaCost:0} mana | {s.Links.Count} links | mix {GetLinkCharges(s.Skill)}/{SkillData.MaxLinkCharges} |{cooldownText}";
    }

    public float GetCooldownRemaining(int index)
    {
        return index >= 0 && index < _cooldownLeft.Length ? Mathf.Max(0f, _cooldownLeft[index]) : 0f;
    }

    public void SelectAndCast(int index, Vector3 target)
    {
        if (index < 0 || index >= Slots.Count)
        {
            return;
        }
        SelectedSlot = index;
        TryCast(target);
    }

    public void TryCast(Vector3 target)
    {
        if (_ownerPlayer == null || Slots.Count == 0)
        {
            return;
        }
        if (_ownerPlayer is PlayerController lockedPlayer && (lockedPlayer.IsGameplayInputLocked || lockedPlayer.Health <= 0f))
        {
            return;
        }
        if (SelectedSlot < 0 || SelectedSlot >= Slots.Count)
        {
            return;
        }
        var selectedSkill = Slots[SelectedSlot].Skill;
        if (_ownerPlayer is PlayerController classOwner && !selectedSkill.IsAllowedFor(classOwner.Progression.HeroClass))
        {
            classOwner.ShowCombatMessage($"{selectedSkill.DisplayName.ToUpperInvariant()} REQUIRES {selectedSkill.RequiredClass}");
            return;
        }
        if (_ownerPlayer is PlayerController progressionPlayer && !progressionPlayer.Progression.IsSkillUnlocked(selectedSkill.Id))
        {
            int requiredLevel = progressionPlayer.Progression.GetSkillRequiredLevel(selectedSkill.Id);
            progressionPlayer.ShowCombatMessage($"{selectedSkill.DisplayName.ToUpperInvariant()} UNLOCKS AT LEVEL {requiredLevel}");
            return;
        }
        if (_cooldownLeft[SelectedSlot] > 0f)
        {
            if (_ownerPlayer is PlayerController player)
            {
                player.ShowCombatMessage($"{slotName(SelectedSlot)} recharging");
            }
            return;
        }
        var slot = Slots[SelectedSlot];
        if (_ownerPlayer is PlayerController manaOwner && !manaOwner.TrySpendMana(slot.Skill.ManaCost))
        {
            return;
        }
        if (_ownerPlayer is PlayerController animatedOwner)
        {
            animatedOwner.PlaySkillAnimation(slot.Skill);
        }
        var resolved = LinkSystem.ResolveCast(slot.Skill, slot.Links);
        if (float.IsNaN(resolved.Damage) || float.IsInfinity(resolved.Damage))
        {
            resolved.Damage = 0f;
        }
        if (_ownerPlayer is PlayerController damageOwner)
        {
            resolved.Damage *= damageOwner.DamageMultiplier * damageOwner.Progression.GetSkillDamageMultiplier(slot.Skill.Id);
            resolved.ElementalPenetration = damageOwner.ElementalPenetration;
            if (resolved.Damage > 0f && GD.Randf() < damageOwner.CriticalChance / 100f)
            {
                resolved.IsCritical = true;
                resolved.CriticalMultiplier = damageOwner.CriticalDamage / 100f;
                resolved.Damage *= resolved.CriticalMultiplier;
            }
        }
        resolved.StaggerDamage = Mathf.Max(4f, resolved.Damage * (resolved.Element == DamageElement.Physical ? 0.75f : 0.42f));
        float progressionCooldown = _ownerPlayer is PlayerController progressionOwner
            ? progressionOwner.CooldownMultiplier
            : 1f;
        _cooldownLeft[SelectedSlot] = slot.Skill.Cooldown * resolved.CooldownMultiplier * progressionCooldown;
        if (_ownerPlayer is PlayerController masteryOwner) masteryOwner.Progression.GainSkillMastery(slot.Skill.Id);

        switch (slot.Skill.Type)
        {
            case SkillType.Summon:
                CastSummon(slot.Skill, resolved, target);
                break;
            case SkillType.AreaAttack:
                CastArea(slot.Skill, resolved, target);
                break;
            case SkillType.Projectile:
                CastProjectile(slot.Skill, resolved, target);
                break;
            case SkillType.Buff:
                CastBuff(slot.Skill, resolved);
                break;
            case SkillType.Movement:
                CastMovement(slot.Skill, resolved, target);
                break;
        }
    }

    public void TryPrimaryAttack(Vector3 target)
    {
        if (_primaryCooldown > 0f || _ownerPlayer is not PlayerController player || player.IsGameplayInputLocked || player.Health <= 0f) return;
        Vector3 direction = target - player.GlobalPosition;
        direction.Y = 0f;
        if (direction.LengthSquared() < 0.04f) direction = -player.GlobalTransform.Basis.Z;
        direction = direction.Normalized();
        player.LookAt(player.GlobalPosition + direction, Vector3.Up, true);
        player.PlayCastAnimation();

        float baseDamage = player.Progression.HeroClass switch
        {
            HeroClass.Runeblade => 31f,
            HeroClass.Aetherist => 22f,
            HeroClass.Warden => 25f,
            HeroClass.Berserker => 36f,
            HeroClass.Necromancer => 23f,
            HeroClass.Shadowstalker => 28f,
            HeroClass.Templar => 30f,
            _ => 25f,
        };
        var resolved = new ResolvedCast
        {
            Element = player.Progression.HeroClass is HeroClass.Aetherist or HeroClass.Templar ? DamageElement.Lightning : player.Progression.HeroClass is HeroClass.Warden or HeroClass.Shadowstalker or HeroClass.Necromancer ? DamageElement.Poison : DamageElement.Physical,
            Damage = baseDamage * player.DamageMultiplier,
            StaggerDamage = player.Progression.HeroClass is HeroClass.Runeblade or HeroClass.Berserker or HeroClass.Templar ? 35f : 15f,
            ElementalPenetration = player.ElementalPenetration,
        };
        if (GD.Randf() < player.CriticalChance / 100f)
        {
            resolved.IsCritical = true;
            resolved.CriticalMultiplier = player.CriticalDamage / 100f;
            resolved.Damage *= resolved.CriticalMultiplier;
        }

        float targetDistance = player.GlobalPosition.DistanceTo(target);
        bool rangedPrimary = player.Progression.HeroClass is HeroClass.Aetherist or HeroClass.Necromancer
            || (player.Progression.HeroClass is HeroClass.Warden or HeroClass.Shadowstalker && targetDistance > 2.6f);
        resolved.AttackForm = rangedPrimary ? AttackForm.Projectile : AttackForm.Melee;
        if (rangedPrimary)
        {
            Node? scene = GetTree().CurrentScene;
            if (scene == null) return;
            var projectile = new Projectile();
            string projectileId = player.Progression.HeroClass switch { HeroClass.Warden => "stone_shot", HeroClass.Shadowstalker => "death_mark", HeroClass.Necromancer => "bone_spear", _ => "arc_surge" };
            projectile.Configure(projectileId, resolved.Damage, direction, resolved);
            projectile.Speed = player.Progression.HeroClass is HeroClass.Warden or HeroClass.Shadowstalker ? 25f : 20f;
            scene.AddChild(projectile);
            projectile.GlobalPosition = player.GlobalPosition + Vector3.Up * 1.35f;
            SkillVfx.SpawnCastRing(scene, player.GlobalPosition, resolved.Element, 0.72f);
            _primaryCooldown = player.Progression.HeroClass is HeroClass.Warden or HeroClass.Shadowstalker ? 0.58f : 0.72f;
            player.ShowCombatMessage(player.Progression.HeroClass switch { HeroClass.Warden => "BOW SHOT", HeroClass.Shadowstalker => "SHADOW KNIFE", HeroClass.Necromancer => "BONE SHARD", _ => "AETHER BOLT" });
            return;
        }

        float range = player.Progression.HeroClass is HeroClass.Runeblade or HeroClass.Berserker or HeroClass.Templar ? 2.75f : 2.15f;
        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
        {
            if (node is not Enemy enemy || !IsInstanceValid(enemy)) continue;
            Vector3 toEnemy = enemy.GlobalPosition - player.GlobalPosition;
            toEnemy.Y = 0f;
            if (toEnemy.Length() <= range && direction.Dot(toEnemy.Normalized()) > 0.12f) enemy.TakeDamage(resolved.Damage, resolved.Element, resolved);
        }
        foreach (Node node in GetTree().GetNodesInGroup("warden_pylons"))
        {
            if (node is not WardenPylon pylon || !IsInstanceValid(pylon)) continue;
            Vector3 toPylon = pylon.GlobalPosition - player.GlobalPosition;
            toPylon.Y = 0f;
            if (toPylon.Length() <= range && direction.Dot(toPylon.Normalized()) > 0.12f) pylon.TakeDamage(resolved.Damage, resolved.Element, resolved);
        }
        Node? impactScene = GetTree().CurrentScene;
        if (impactScene != null) SkillVfx.SpawnMeleeHit(impactScene, player.GlobalPosition + direction * 1.5f + Vector3.Up * 0.7f);
        _primaryCooldown = player.Progression.HeroClass switch { HeroClass.Berserker => 0.82f, HeroClass.Templar => 0.72f, HeroClass.Runeblade => 0.62f, _ => 0.4f };
        player.ShowCombatMessage(player.Progression.HeroClass switch { HeroClass.Berserker => "HEAVY CLEAVE", HeroClass.Templar => "HAMMER BLOW", HeroClass.Runeblade => "RUNE SLASH", _ => "DAGGER CUT" });
    }

    private string slotName(int index) => index >= 0 && index < Slots.Count ? Slots[index].Skill.DisplayName : "Skill";

    private void CastBuff(SkillData skill, ResolvedCast resolved)
    {
        if (_ownerPlayer is PlayerController player)
        {
            if (skill.Id == "aether_pulse")
            {
                var pulseScene = GetTree().CurrentScene;
                if (pulseScene != null)
                {
                    SkillVfx.SpawnCastRing(pulseScene, player.GlobalPosition, DamageElement.Lightning, 0.8f);
                    SkillVfx.SpawnLinkLayers(pulseScene, player.GlobalPosition, resolved);
                }
                player.ShowCombatMessage("AETHER PULSE — no damage");
                return;
            }
            player.ActivateBuff(skill.Id, 6f * resolved.DurationMultiplier);
            var scene = GetTree().CurrentScene;
            if (scene != null)
            {
                SkillVfx.SpawnCastRing(scene, player.GlobalPosition, skill.DamageElement, 1.7f);
                SkillVfx.SpawnLinkLayers(scene, player.GlobalPosition, resolved);
            }
        }
        GD.Print($"[Caster] Buff cast: {skill.DisplayName}");
    }

    private void CastMovement(SkillData skill, ResolvedCast resolved, Vector3 target)
    {
        if (_ownerPlayer is not PlayerController player)
        {
            return;
        }
        Vector3 from = player.GlobalPosition;
        Vector3 direction = target - from;
        direction.Y = 0f;
        if (direction.Length() < 0.3f)
        {
            return;
        }
        Vector3 destination = from + direction.Normalized() * Mathf.Min(7f, direction.Length());
        destination.Y = 0.2f;
        var scene = GetTree().CurrentScene;
        if (scene == null)
        {
            return;
        }
        SkillVfx.SpawnDashTrail(scene, from, destination, resolved.Element, resolved.HasLightningInfusion);
        SkillVfx.SpawnLinkLayers(scene, destination, resolved);
        player.DashTo(destination);
        foreach (var node in GetTree().GetNodesInGroup("enemies"))
        {
            if (node is Enemy enemy && IsInstanceValid(enemy) && enemy.GlobalPosition.DistanceTo(destination) < 2.7f)
            {
                enemy.TakeDamage(resolved.Damage, resolved.Element, resolved);
            }
        }
        DamagePylonsInRadius(destination, 2.7f, resolved);
        GD.Print($"[Caster] Movement {skill.DisplayName} dash={direction.Length():0.0} linked={resolved.HasLightningInfusion}");
    }

    private void CastSummon(SkillData skill, ResolvedCast resolved, Vector3 target)
    {
        var scene = GetTree().CurrentScene;
        if (scene == null || _ownerPlayer == null)
        {
            return;
        }
        var minion = new Minion();
        minion.Configure(skill, resolved, _ownerPlayer);
        scene.AddChild(minion);
        Vector3 spawn = target;
        spawn.Y = 0.2f;
        minion.GlobalPosition = spawn;
        SkillVfx.SpawnSummon(scene, spawn, resolved.Element, resolved.LightningForm);
        SkillVfx.SpawnLinkLayers(scene, spawn, resolved);
        GD.Print($"[Caster] Summoned {skill.DisplayName} lightningForm={resolved.LightningForm}");
    }

    private void CastArea(SkillData skill, ResolvedCast resolved, Vector3 target)
    {
        var scene = GetTree().CurrentScene;
        if (scene == null)
        {
            return;
        }
        target.Y = 0.1f;
        SkillVfx.SpawnSkillImpact(scene, skill, target, resolved);

        float radius = 4f;
        foreach (var n in GetTree().GetNodesInGroup("enemies"))
        {
            if (n is Enemy e && IsInstanceValid(e))
            {
                if (e.GlobalPosition.DistanceTo(target) <= radius)
                {
                    e.TakeDamage(resolved.Damage, resolved.Element, resolved);
                }
            }
        }
        DamagePylonsInRadius(target, radius, resolved);

        if (resolved.HasGroundFire)
        {
            var ground = new BurningGround();
            ground.Configure(resolved.GroundFireDps, resolved.GroundFireDuration, resolved.GroundFireRadius, resolved);
            scene.AddChild(ground);
            ground.GlobalPosition = target;
        }
        GD.Print($"[Caster] Area {skill.DisplayName} dmg={resolved.Damage} groundFire={resolved.HasGroundFire}");
    }

    private void CastProjectile(SkillData skill, ResolvedCast resolved, Vector3 target)
    {
        var scene = GetTree().CurrentScene;
        if (scene == null || _ownerPlayer == null)
        {
            return;
        }
        Vector3 from = _ownerPlayer.GlobalPosition + new Vector3(0, 1.4f, 0);
        Vector3 dir = target - from;
        dir.Y = 0f;
        if (dir.Length() < 0.5f)
        {
            dir = -_ownerPlayer.GlobalTransform.Basis.Z;
        }
        var proj = new Projectile();
        proj.Configure(skill.Id, resolved.Damage, dir.Normalized(), resolved);
        scene.AddChild(proj);
        proj.GlobalPosition = from;
        SkillVfx.SpawnCastRing(scene, from, resolved.Element, 1f);
        SkillVfx.SpawnLinkLayers(scene, from, resolved);
        GD.Print($"[Caster] Projectile {skill.DisplayName} chain={resolved.ChainCount}");
    }

    private void DamagePylonsInRadius(Vector3 center, float radius, ResolvedCast resolved)
    {
        foreach (Node node in GetTree().GetNodesInGroup("warden_pylons"))
        {
            if (node is WardenPylon pylon && IsInstanceValid(pylon) && pylon.GlobalPosition.DistanceTo(center) <= radius)
            {
                pylon.TakeDamage(resolved.Damage, resolved.Element, resolved);
            }
        }
    }

    private static void SpawnExplosionVisual(Node scene, Vector3 pos, DamageElement element)
    {
        var mat = new StandardMaterial3D
        {
            AlbedoColor = element == DamageElement.Fire ? new Color(1f, 0.4f, 0.1f) : new Color(0.6f, 0.8f, 1f),
            EmissionEnabled = true,
            Emission = new Color(1f, 0.5f, 0.1f),
            EmissionEnergyMultiplier = 2f,
        };
        var mesh = new MeshInstance3D { Mesh = new SphereMesh { Radius = 1.2f, Height = 2.4f } };
        mesh.SetSurfaceOverrideMaterial(0, mat);
        scene.AddChild(mesh);
        mesh.GlobalPosition = pos + new Vector3(0, 1f, 0);
        var tween = mesh.CreateTween();
        tween.TweenProperty(mesh, "scale", Vector3.One * 2f, 0.25);
        tween.TweenCallback(Callable.From(mesh.QueueFree));
    }
}
