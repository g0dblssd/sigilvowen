using Godot;

namespace Sigilwoven;

/// <summary>Repeatable multi-floor raid: clear escalating packs, then defeat the guardian.</summary>
public partial class DungeonController : Node
{
    private const int MaxFloors = 3;

    public event System.Action<bool>? RaidStateChanged;

    private PlayerController? _player;
    private Node3D? _world;
    private Button? _startButton;
    private Label? _statusLabel;
    private OptionButton? _difficultyOption;
    private Label? _difficultyDetails;
    private PanelContainer? _panel;
    private bool _unlocked;
    private bool _running;
    private int _packsRemaining;
    private int _currentFloor;
    private int _runNumber = 1;
    private int _difficultyIndex = 1;

    public void Setup(PlayerController player, Node3D world)
    {
        _player = player;
        _world = world;
        _player.Progression.Changed += RefreshPanel;
        BuildDifficultyStatue();
        RefreshPanel();
    }

    public override void _ExitTree()
    {
        if (_player != null) _player.Progression.Changed -= RefreshPanel;
    }

    public override void _Ready()
    {
        var layer = new CanvasLayer { Layer = 7 };
        AddChild(layer);

        var openButton = new Button { Text = "DUNGEONS  [D]", AnchorRight = 1f, OffsetLeft = -235f, OffsetRight = -18f, OffsetTop = 205f, OffsetBottom = 245f };
        openButton.Pressed += ToggleDungeonTab;
        layer.AddChild(openButton);

        _panel = new PanelContainer
        {
            AnchorLeft = 0.16f, AnchorRight = 0.84f, AnchorTop = 0.12f, AnchorBottom = 0.88f,
            Visible = false,
        };
        _panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.01f, 0.018f, 0.04f, 0.97f), BorderColor = new Color(0.24f, 0.62f, 0.9f),
            BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
            CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10, CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10,
            ContentMarginLeft = 12f, ContentMarginRight = 12f, ContentMarginTop = 10f, ContentMarginBottom = 10f,
        });
        layer.AddChild(_panel);

        var content = new VBoxContainer();
        _panel.AddChild(content);
        var title = new Label { Text = "ECHOING VAULT  //  DESCENDING RAID" };
        title.AddThemeFontSizeOverride("font_size", 18);
        title.Modulate = new Color(0.55f, 0.85f, 1f);
        content.AddChild(title);

        var description = new Label
        {
            Text = "Descend through 3 randomized high-density floors, break the hordes, defeat the guardian, earn a Link and Skill.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        description.CustomMinimumSize = new Vector2(285f, 54f);
        content.AddChild(description);

        content.AddChild(new Label { Text = "VAULT DIFFICULTY", Modulate = new Color(1f, 0.72f, 0.28f) });
        _difficultyOption = new OptionButton();
        _difficultyOption.AddItem("ADVENTURER  — LEARN THE VAULT", 0);
        _difficultyOption.AddItem("VETERAN  — INTENDED", 1);
        _difficultyOption.AddItem("TORMENT  — PUNISHING", 2);
        _difficultyOption.AddItem("ABYSSAL  — SUFFER", 3);
        _difficultyOption.AddItem("NIGHTMARE  — LEVEL 75", 4);
        _difficultyOption.AddItem("HELLBOUND  — LEVEL 150", 5);
        _difficultyOption.AddItem("INFERNO  — LEVEL 225", 6);
        _difficultyOption.AddItem("VOID ASCENDANT  — LEVEL 300 / PARAGON", 7);
        _difficultyOption.Selected = _difficultyIndex;
        _difficultyOption.ItemSelected += OnDifficultySelected;
        content.AddChild(_difficultyOption);
        _difficultyDetails = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(330f, 64f) };
        content.AddChild(_difficultyDetails);

        _statusLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _statusLabel.CustomMinimumSize = new Vector2(285f, 48f);
        content.AddChild(_statusLabel);

        _startButton = new Button { Text = "ENTER ECHOING VAULT" };
        _startButton.Pressed += StartDungeon;
        content.AddChild(_startButton);
        RefreshPanel();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.D)
        {
            ToggleDungeonTab();
            GetViewport().SetInputAsHandled();
        }
        else if (@event is InputEventKey esc && esc.Pressed && !esc.Echo && esc.Keycode == Key.Escape && _panel?.Visible == true)
        {
            _panel.Visible = false;
            GetViewport().SetInputAsHandled();
        }
    }

    private void ToggleDungeonTab()
    {
        if (_panel != null) _panel.Visible = !_panel.Visible;
    }

    public void UnlockDungeon()
    {
        _unlocked = true;
        RefreshPanel();
    }

    private void StartDungeon()
    {
        if (!_unlocked || _running || _player == null || _world == null)
        {
            return;
        }
        _running = true;
        _currentFloor = 1;
        _player.GlobalPosition = new Vector3(0f, 0.2f, 0f);
        RaidStateChanged?.Invoke(true);
        _player.ShowCombatMessage($"RAID RUN {_runNumber} STARTED");
        SpawnRaidFloor();
        if (_panel != null) _panel.Visible = false;
        RefreshPanel();
    }

    private void OnDifficultySelected(long selected)
    {
        if (_running)
        {
            if (_difficultyOption != null) _difficultyOption.Selected = _difficultyIndex;
            return;
        }
        int requested = Mathf.Clamp((int)selected, 0, 7);
        int required = RequiredLevel(requested);
        if ((_player?.Progression.Level ?? 1) < required)
        {
            _player?.ShowCombatMessage($"DIFFICULTY UNLOCKS AT LEVEL {required}");
            if (_difficultyOption != null) _difficultyOption.Selected = _difficultyIndex;
            return;
        }
        _difficultyIndex = requested;
        RefreshPanel();
    }

    private (string Name, float Health, float Damage, int Density, float Elite, float Rarity, int BonusDrops) GetDifficulty()
    {
        return _difficultyIndex switch
        {
            0 => ("ADVENTURER", 0.82f, 0.82f, -1, 0f, 0f, 0),
            2 => ("TORMENT", 1.75f, 1.45f, 2, 0.14f, 0.1f, 2),
            3 => ("ABYSSAL", 2.6f, 1.85f, 4, 0.28f, 0.18f, 4),
            4 => ("NIGHTMARE", 3.4f, 2.15f, 5, 0.34f, 0.24f, 5),
            5 => ("HELLBOUND", 4.7f, 2.6f, 7, 0.42f, 0.3f, 7),
            6 => ("INFERNO", 6.2f, 3.15f, 9, 0.52f, 0.38f, 9),
            7 => ("VOID ASCENDANT", 8.5f, 3.8f, 12, 0.65f, 0.48f, 12),
            _ => ("VETERAN", 1.38f, 1.28f, 1, 0.08f, 0.04f, 1),
        };
    }

    private void SpawnRaidFloor()
    {
        if (_player == null)
        {
            return;
        }

        int threatTier = CalculateAdaptiveThreatTier();
        var difficulty = GetDifficulty();
        int packCount = Mathf.Max(3, 4 + _currentFloor + threatTier / 2 + difficulty.Density / 2);
        float levelHealth = 1f + (_player.Progression.Level - 1) * 0.012f;
        float baseHp = (70f + _currentFloor * 24f + (_runNumber - 1) * 18f) * (1f + threatTier * 0.06f) * levelHealth;
        _packsRemaining = packCount;

        for (int packIndex = 0; packIndex < packCount; packIndex++)
        {
            int membersPerPack = GD.RandRange(Mathf.Max(4, 6 + _currentFloor + threatTier + difficulty.Density), 9 + _currentFloor * 2 + threatTier + difficulty.Density);
            Vector3 position = RollHordePosition(packIndex, packCount);
            var archetypes = new EnemyArchetype[membersPerPack];
            for (int memberIndex = 0; memberIndex < membersPerPack; memberIndex++)
            {
                float roll = GD.Randf();
                archetypes[memberIndex] = roll switch
                {
                    < 0.1f => EnemyArchetype.Brute,
                    < 0.2f => EnemyArchetype.Hexer,
                    < 0.3f => EnemyArchetype.Berserker,
                    < 0.39f => EnemyArchetype.Shieldbearer,
                    < 0.47f => EnemyArchetype.Leaper,
                    < 0.55f => EnemyArchetype.Necromancer,
                    < 0.68f => EnemyArchetype.Ghoul,
                    < 0.8f => EnemyArchetype.SkeletonArcher,
                    < 0.9f => EnemyArchetype.Vampire,
                    _ => EnemyArchetype.Raider,
                };
            }
            SpawnPack($"FLOOR {_currentFloor} // PACK {packIndex + 1}", position, baseHp, archetypes);
        }

        _player.SetObjectiveStatus($"{difficulty.Name} RAID  •  FLOOR {_currentFloor}/{MaxFloors}  •  {_packsRemaining} PACKS");
        _player.ShowCombatMessage($"FLOOR {_currentFloor} — THE HORDE STIRS");
        if (_statusLabel != null)
        {
            _statusLabel.Text = $"Floor {_currentFloor}/{MaxFloors}: {_packsRemaining} packs remain.";
        }
        GD.Print($"[Raid] Run {_runNumber}, floor {_currentFloor}: {packCount} randomized high-density hordes, adaptive threat {threatTier}.");
    }

    private int CalculateAdaptiveThreatTier()
    {
        if (_player == null) return 0;
        float buildPower = (_player.Progression.Level - 1) * 0.2f
            + _player.Inventory.DamagePercent * 0.08f
            + _player.Armor * 0.012f
            + (_player.CriticalChance - 5f) * 0.05f;
        return Mathf.Clamp(Mathf.FloorToInt(buildPower / 3f), 0, 4);
    }

    private Vector3 RollHordePosition(int index, int count)
    {
        // A stratified random angle prevents overlaps without recreating the old
        // perfectly even encounter ring. Radius and lateral jitter change per run.
        float sector = Mathf.Tau / count;
        float angle = sector * index + (float)GD.RandRange(-sector * 0.42f, sector * 0.42f);
        float radius = (float)GD.RandRange(18f, 51f);
        return new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
    }

    private void SpawnPack(string name, Vector3 position, float baseHp, EnemyArchetype[] archetypes)
    {
        if (_player == null || _world == null)
        {
            return;
        }
        var pack = new MobPack();
        pack.Setup(_player, name, 10.5f, false);
        for (int i = 0; i < archetypes.Length; i++)
        {
            float angle = GD.Randf() * Mathf.Tau;
            float scatter = Mathf.Sqrt(GD.Randf()) * (float)GD.RandRange(2.5f, 6.5f);
            var difficulty = GetDifficulty();
            EliteModifier elite = i == 0 || (i > 2 && GD.Randf() < 0.2f + difficulty.Elite) ? Enemy.RollEliteModifier() : EliteModifier.None;
            Vector3 offset = new(Mathf.Cos(angle) * scatter, 0f, Mathf.Sin(angle) * scatter);
            Enemy enemy = pack.AddMember(baseHp * (float)GD.RandRange(0.9f, 1.12f), archetypes[i], offset, elite);
            float levelDamage = 1f + (_player.Progression.Level - 1) * 0.0045f;
            enemy.ApplyDifficulty(difficulty.Health, difficulty.Damage * levelDamage, difficulty.Rarity, 1f + _difficultyIndex * 0.22f);
        }
        pack.Cleared += OnDungeonPackCleared;
        _world.AddChild(pack);
        pack.GlobalPosition = position;
    }

    private void OnDungeonPackCleared(MobPack pack)
    {
        pack.Cleared -= OnDungeonPackCleared;
        _packsRemaining = Mathf.Max(0, _packsRemaining - 1);
        if (_packsRemaining > 0)
        {
            _player?.SetObjectiveStatus($"RAID FLOOR {_currentFloor}/{MaxFloors}  •  {_packsRemaining} PACKS REMAIN");
            if (_statusLabel != null)
            {
                _statusLabel.Text = $"Floor {_currentFloor}/{MaxFloors}: {_packsRemaining} packs remain.";
            }
            return;
        }

        if (_currentFloor < MaxFloors)
        {
            _currentFloor++;
            if (_player != null)
            {
                _player.GlobalPosition = new Vector3(0f, 0.2f, 0f);
                _player.ShowCombatMessage($"DESCENDING TO FLOOR {_currentFloor}");
            }
        SpawnRaidFloor();
        if (_panel != null) _panel.Visible = false;
        }
        else
        {
            SpawnGuardian();
        }
    }

    private void SpawnGuardian()
    {
        if (_player == null || _world == null)
        {
            return;
        }
        var guardian = new Enemy();
        guardian.Configure(850f + (_runNumber - 1) * 120f, false, EnemyArchetype.Guardian);
        var difficulty = GetDifficulty();
        float levelDamage = 1f + (_player.Progression.Level - 1) * 0.0045f;
        guardian.ApplyDifficulty(difficulty.Health * (1f + (_player.Progression.Level - 1) * 0.012f), difficulty.Damage * levelDamage, difficulty.Rarity, 1f + _difficultyIndex * 0.22f);
        guardian.Died += OnGuardianDied;
        _world.AddChild(guardian);
        guardian.GlobalPosition = new Vector3(0f, 0f, -13f);
        guardian.JoinAmbush(_player);
        _player.SetObjectiveStatus("BOSS  •  SIGIL GUARDIAN");
        _player.ShowCombatMessage("THE SIGIL GUARDIAN AWAKENS");
        if (_statusLabel != null)
        {
            _statusLabel.Text = "Boss active: defeat the Sigil Guardian.";
        }
    }

    private void OnGuardianDied(Enemy guardian)
    {
        guardian.Died -= OnGuardianDied;
        if (_player == null)
        {
            return;
        }
        SkillLinkData? linkReward = _player.Progression.UnlockNextLink();
        int completionExperience = Mathf.RoundToInt((450f + _player.Progression.Level * 12f) * (1f + _difficultyIndex * 0.25f));
        _player.Progression.GainExperience(completionExperience);
        _player.RestoreMana(_player.MaxMana);
        string rewardSummary = BuildRewardSummary(linkReward, completionExperience);
        SpawnDifficultyRewards(guardian.GlobalPosition);
        _player.SetObjectiveStatus($"RAID CLEARED  •  {rewardSummary}");
        _player.ShowCombatMessage(rewardSummary);
        _running = false;
        _runNumber++;
        RaidStateChanged?.Invoke(false);
        RefreshPanel();
    }

    private void SpawnDifficultyRewards(Vector3 position)
    {
        if (_world == null || _player == null) return;
        var difficulty = GetDifficulty();
        for (int i = 0; i < difficulty.BonusDrops; i++)
        {
            float angle = Mathf.Tau * i / Mathf.Max(1, difficulty.BonusDrops);
            Vector3 offset = new(Mathf.Cos(angle) * 1.4f, 0.2f, Mathf.Sin(angle) * 1.4f);
            LootDrop.Spawn(_world, position + offset, ItemGenerator.Generate(_player.Progression.Level + _difficultyIndex * 3, difficulty.Rarity + 0.12f, _player.Progression.HeroClass));
        }
    }

    private static string BuildRewardSummary(SkillLinkData? linkReward, int experience)
    {
        if (linkReward != null)
        {
            return $"NEW LINK: {linkReward.DisplayName.ToUpperInvariant()}  •  +{experience} XP";
        }
        return $"GUARDIAN DEFEATED  •  +{experience} XP";
    }

    private void RefreshPanel()
    {
        if (_startButton != null)
        {
            _startButton.Disabled = !_unlocked || _running;
            _startButton.Text = _running ? $"RAID FLOOR {_currentFloor}/{MaxFloors}" : $"START DESCENDING RAID  //  RUN {_runNumber}";
        }
        if (_difficultyOption != null) _difficultyOption.Disabled = _running;
        if (_difficultyOption != null && _player != null)
        {
            for (int i = 0; i < _difficultyOption.ItemCount; i++) _difficultyOption.SetItemDisabled(i, _player.Progression.Level < RequiredLevel(i));
        }
        if (_difficultyDetails != null)
        {
            var difficulty = GetDifficulty();
            float levelLife = _player == null ? 1f : 1f + (_player.Progression.Level - 1) * 0.012f;
            float levelDamage = _player == null ? 1f : 1f + (_player.Progression.Level - 1) * 0.0045f;
            float experience = 1f + _difficultyIndex * 0.22f;
            _difficultyDetails.Text = $"{difficulty.Name}  •  Total enemy life ×{difficulty.Health * levelLife:0.00}  •  damage ×{difficulty.Damage * levelDamage:0.00}\nDensity {(difficulty.Density >= 0 ? "+" : "")}{difficulty.Density}  •  elite pressure +{difficulty.Elite * 100:0}%  •  XP ×{experience:0.00}  •  rarity +{difficulty.Rarity * 100:0}%  •  bonus relics {difficulty.BonusDrops}";
            _difficultyDetails.Modulate = _difficultyIndex switch { >= 7 => new Color(0.78f, 0.24f, 1f), >= 4 => new Color(1f, 0.16f, 0.12f), 3 => new Color(1f, 0.25f, 0.18f), 2 => new Color(1f, 0.55f, 0.18f), _ => new Color(0.58f, 0.82f, 1f) };
        }
        if (_statusLabel != null && !_running)
        {
            _statusLabel.Text = _unlocked
                ? "Ready. Skills unlock by hero level; every clear grants XP, loot and the next sealed Link."
                : "Locked: purge every surface camp once.";
        }
    }

    private static int RequiredLevel(int index) => index switch { 4 => 75, 5 => 150, 6 => 225, 7 => PlayerProgression.MaxLevel, _ => 1 };

    private void BuildDifficultyStatue()
    {
        if (_world == null) return;
        var shrine = new Node3D { Position = new Vector3(2f, 0f, 8f) };
        var stone = new StandardMaterial3D { AlbedoColor = new Color(0.12f, 0.1f, 0.16f), Metallic = 0.25f, Roughness = 0.72f, EmissionEnabled = true, Emission = new Color(0.25f, 0.03f, 0.45f), EmissionEnergyMultiplier = 1.2f };
        var pedestal = new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = 1f, BottomRadius = 1.3f, Height = 0.75f, RadialSegments = 8 }, Position = new Vector3(0f, 0.38f, 0f) }; pedestal.SetSurfaceOverrideMaterial(0, stone); shrine.AddChild(pedestal);
        var obelisk = new MeshInstance3D { Mesh = new PrismMesh { Size = new Vector3(1.05f, 3.4f, 1.05f) }, Position = new Vector3(0f, 2.05f, 0f) }; obelisk.SetSurfaceOverrideMaterial(0, stone); shrine.AddChild(obelisk);
        shrine.AddChild(new Label3D { Text = "VAULT OF TORMENT\n[D] CHOOSE DIFFICULTY", Position = new Vector3(0f, 4.1f, 0f), Billboard = BaseMaterial3D.BillboardModeEnum.Enabled, FontSize = 34, OutlineSize = 8, Modulate = new Color(0.76f, 0.38f, 1f) });
        shrine.AddChild(new OmniLight3D { Position = new Vector3(0f, 2.6f, 0f), LightColor = new Color(0.55f, 0.16f, 1f), LightEnergy = 2f, OmniRange = 7f });
        _world.AddChild(shrine);
    }
}
