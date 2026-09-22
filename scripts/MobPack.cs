using System;
using System.Collections.Generic;
using Godot;

namespace Sigilwoven;

/// <summary>A dormant group that enters combat together when the player approaches.</summary>
public partial class MobPack : Node3D
{
    private readonly List<Enemy> _members = new();
    private PlayerController? _player;
    private Label3D? _label;
    private MeshInstance3D? _ring;
    private StandardMaterial3D? _ringMaterial;
    private float _activationRadius = 5f;
    private bool _active;
    private bool _cleared;

    public string PackName { get; private set; } = "HOSTILE PACK";
    public bool IsActive => _active;
    public bool IsCleared => _cleared;
    public event Action<MobPack>? Activated;
    public event Action<MobPack>? Cleared;

    public void Setup(PlayerController player, string packName, float activationRadius = 5f)
    {
        _player = player;
        PackName = packName;
        _activationRadius = activationRadius;
        if (_label != null)
        {
            _label.Text = $"{PackName}  •  DORMANT";
        }
    }

    public Enemy AddMember(float baseHp, EnemyArchetype archetype, Vector3 localPosition)
    {
        var enemy = new Enemy();
        enemy.Configure(baseHp, false, archetype);
        AddChild(enemy);
        enemy.Position = localPosition;
        _members.Add(enemy);
        return enemy;
    }

    public override void _Ready()
    {
        _ringMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.5f, 0.12f, 0.16f, 0.72f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            EmissionEnabled = true,
            Emission = new Color(0.38f, 0.015f, 0.03f),
            EmissionEnergyMultiplier = 2f,
        };
        _ring = new MeshInstance3D
        {
            Mesh = new TorusMesh { InnerRadius = _activationRadius - 0.16f, OuterRadius = _activationRadius },
            Position = new Vector3(0f, 0.06f, 0f),
        };
        _ring.SetSurfaceOverrideMaterial(0, _ringMaterial);
        AddChild(_ring);

        _label = new Label3D
        {
            Text = $"{PackName}  •  DORMANT",
            FontSize = 34,
            OutlineSize = 7,
            Modulate = new Color(1f, 0.48f, 0.48f),
            Position = new Vector3(0f, 0.35f, 0f),
        };
        AddChild(_label);
    }

    public override void _Process(double delta)
    {
        if (_cleared || _player == null || !IsInstanceValid(_player))
        {
            return;
        }

        if (!_active && GlobalPosition.DistanceTo(_player.GlobalPosition) <= _activationRadius)
        {
            Activate();
        }
        if (_active && !HasLivingMembers())
        {
            _cleared = true;
            Cleared?.Invoke(this);
            QueueFree();
            return;
        }
        if (_ring != null)
        {
            _ring.RotateY((float)delta * (_active ? 1.9f : 0.45f));
        }
    }

    private void Activate()
    {
        if (_active || _player == null)
        {
            return;
        }
        _active = true;
        foreach (Enemy enemy in _members)
        {
            if (IsInstanceValid(enemy) && !enemy.IsQueuedForDeletion())
            {
                enemy.JoinAmbush(_player);
            }
        }
        if (_label != null)
        {
            _label.Text = $"{PackName}  •  ENGAGED";
            _label.Modulate = new Color(1f, 0.75f, 0.25f);
        }
        if (_ringMaterial != null)
        {
            _ringMaterial.Emission = new Color(1f, 0.22f, 0.02f);
            _ringMaterial.EmissionEnergyMultiplier = 3.4f;
        }
        _player.SetObjectiveStatus($"ENGAGED  •  {PackName}");
        Activated?.Invoke(this);
    }

    private bool HasLivingMembers()
    {
        foreach (Enemy enemy in _members)
        {
            if (IsInstanceValid(enemy) && !enemy.IsQueuedForDeletion())
            {
                return true;
            }
        }
        return false;
    }
}
