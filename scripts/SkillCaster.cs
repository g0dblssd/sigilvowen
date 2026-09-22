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
    private readonly Dictionary<string, int> _linkCharges = new();
    private float[] _cooldownLeft = System.Array.Empty<float>();
    private Node3D? _ownerPlayer;

    public void Setup(Node3D player)
    {
        _ownerPlayer = player;
    }

    public override void _Ready()
    {
        ResetLinkCharges();

        var meteor = SkillCatalog.ById("meteor");
        var elemental = SkillCatalog.ById("lightning_elemental");
        var chain = SkillCatalog.ById("chain_lightning");
        var golem = SkillCatalog.ById("bone_golem");
        var flameDash = SkillCatalog.ById("flame_dash");
        var fireball = SkillCatalog.ById("fireball");
        var frostbolt = SkillCatalog.ById("frostbolt");
        var iceNova = SkillCatalog.ById("ice_nova");
        var aetherPulse = SkillCatalog.ById("aether_pulse");
        var haste = SkillCatalog.ById("haste_aura");
        var fireLink = LinkCatalog.ById("fire_link");
        var lightningForm = LinkCatalog.ById("lightning_form");
        var chainExt = LinkCatalog.ById("chain_extension");

        Slots.Clear();
        Slots.Add(new Slot(meteor!, fireLink != null ? new List<SkillLinkData> { fireLink } : new()));
        Slots.Add(new Slot(elemental!, lightningForm != null ? new List<SkillLinkData> { lightningForm } : new()));
        Slots.Add(new Slot(chain!, chainExt != null ? new List<SkillLinkData> { chainExt } : new()));
        Slots.Add(new Slot(golem!, new List<SkillLinkData>()));
        Slots.Add(new Slot(flameDash!, new List<SkillLinkData>()));
        Slots.Add(new Slot(haste!, new List<SkillLinkData>()));
        Slots.Add(new Slot(fireball!, new List<SkillLinkData>()));
        Slots.Add(new Slot(frostbolt!, new List<SkillLinkData>()));
        Slots.Add(new Slot(iceNova!, new List<SkillLinkData>()));
        Slots.Add(new Slot(aetherPulse!, new List<SkillLinkData>()));
        // The initial demo links already occupy one charge on their host skill.
        foreach (var slot in Slots)
        {
            if (slot.Links.Count > 0)
            {
                TrySpendLinkCharges(slot.Skill, slot.Links.Count);
            }
        }
        _cooldownLeft = new float[Slots.Count];
        SelectedSlot = 0;
    }

    public int GetLinkCharges(SkillData skill)
    {
        return _linkCharges.TryGetValue(skill.Id, out int charges)
            ? charges
            : SkillData.MaxLinkCharges;
    }

    public bool TrySpendLinkCharges(SkillData skill, int amount)
    {
        int available = GetLinkCharges(skill);
        if (amount < 0 || available < amount)
        {
            return false;
        }

        _linkCharges[skill.Id] = available - amount;
        return true;
    }

    private void ResetLinkCharges()
    {
        _linkCharges.Clear();
        foreach (var skill in SkillCatalog.All)
        {
            _linkCharges[skill.Id] = SkillData.MaxLinkCharges;
        }
    }

    public override void _Process(double delta)
    {
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
        if (_ownerPlayer is PlayerController lockedPlayer && (lockedPlayer.IsControlLocked || lockedPlayer.Health <= 0f))
        {
            return;
        }
        if (SelectedSlot < 0 || SelectedSlot >= Slots.Count)
        {
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
        var resolved = LinkSystem.ResolveCast(slot.Skill, slot.Links);
        if (float.IsNaN(resolved.Damage) || float.IsInfinity(resolved.Damage))
        {
            resolved.Damage = 0f;
        }
        if (_ownerPlayer is PlayerController damageOwner)
        {
            resolved.Damage *= damageOwner.DamageMultiplier;
        }
        float progressionCooldown = _ownerPlayer is PlayerController progressionOwner
            ? progressionOwner.Progression.CooldownMultiplier
            : 1f;
        _cooldownLeft[SelectedSlot] = slot.Skill.Cooldown * resolved.CooldownMultiplier * progressionCooldown;

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
                }
                player.ShowCombatMessage("AETHER PULSE — no damage");
                return;
            }
            player.ActivateBuff(skill.Id, 6f * resolved.DurationMultiplier);
            var scene = GetTree().CurrentScene;
            if (scene != null)
            {
                SkillVfx.SpawnCastRing(scene, player.GlobalPosition, skill.DamageElement, 1.7f);
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
        player.DashTo(destination);
        foreach (var node in GetTree().GetNodesInGroup("enemies"))
        {
            if (node is Enemy enemy && IsInstanceValid(enemy) && enemy.GlobalPosition.DistanceTo(destination) < 2.7f)
            {
                enemy.TakeDamage(resolved.Damage, resolved.Element, resolved);
            }
        }
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
        SkillVfx.SpawnImpact(scene, target, resolved.Element, resolved.HasLightningInfusion || resolved.HasStun || resolved.LightningForm);
        SpawnExplosionVisual(scene, target, resolved.Element);

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
        proj.Configure(resolved.Damage, dir.Normalized(), resolved);
        scene.AddChild(proj);
        proj.GlobalPosition = from;
        SkillVfx.SpawnCastRing(scene, from, resolved.Element, 1f);
        if (resolved.HasLightningInfusion)
        {
            SkillVfx.SpawnLightningArcs(scene, from, 3);
        }
        GD.Print($"[Caster] Projectile {skill.DisplayName} chain={resolved.ChainCount}");
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
