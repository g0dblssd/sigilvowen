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
    private bool _unlocked;
    private bool _running;
    private int _packsRemaining;
    private int _currentFloor;
    private int _runNumber = 1;

    public void Setup(PlayerController player, Node3D world)
    {
        _player = player;
        _world = world;
        RefreshPanel();
    }

    public override void _Ready()
    {
        var layer = new CanvasLayer { Layer = 7 };
        AddChild(layer);

        var panel = new PanelContainer
        {
            AnchorLeft = 1f,
            AnchorRight = 1f,
            OffsetLeft = -330f,
            OffsetRight = -18f,
            OffsetTop = 252f,
            OffsetBottom = 462f,
        };
        layer.AddChild(panel);

        var content = new VBoxContainer();
        panel.AddChild(content);
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

        _statusLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _statusLabel.CustomMinimumSize = new Vector2(285f, 48f);
        content.AddChild(_statusLabel);

        _startButton = new Button { Text = "ENTER ECHOING VAULT" };
        _startButton.Pressed += StartDungeon;
        content.AddChild(_startButton);
        RefreshPanel();
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
        RefreshPanel();
    }

    private void SpawnRaidFloor()
    {
        if (_player == null)
        {
            return;
        }

        int packCount = 4 + _currentFloor;
        float baseHp = 70f + _currentFloor * 24f + (_runNumber - 1) * 18f;
        _packsRemaining = packCount;

        for (int packIndex = 0; packIndex < packCount; packIndex++)
        {
            int membersPerPack = GD.RandRange(6 + _currentFloor, 9 + _currentFloor * 2);
            Vector3 position = RollHordePosition(packIndex, packCount);
            var archetypes = new EnemyArchetype[membersPerPack];
            for (int memberIndex = 0; memberIndex < membersPerPack; memberIndex++)
            {
                float roll = GD.Randf();
                archetypes[memberIndex] = roll switch
                {
                    < 0.2f => EnemyArchetype.Brute,
                    < 0.38f => EnemyArchetype.Hexer,
                    _ => EnemyArchetype.Raider,
                };
            }
            SpawnPack($"FLOOR {_currentFloor} // PACK {packIndex + 1}", position, baseHp, archetypes);
        }

        _player.SetObjectiveStatus($"RAID FLOOR {_currentFloor}/{MaxFloors}  •  {_packsRemaining} PACKS");
        _player.ShowCombatMessage($"FLOOR {_currentFloor} — THE HORDE STIRS");
        if (_statusLabel != null)
        {
            _statusLabel.Text = $"Floor {_currentFloor}/{MaxFloors}: {_packsRemaining} packs remain.";
        }
        GD.Print($"[Raid] Run {_runNumber}, floor {_currentFloor}: {packCount} randomized high-density hordes.");
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
            EliteModifier elite = i == 0 || (i > 2 && GD.Randf() < 0.1f) ? Enemy.RollEliteModifier() : EliteModifier.None;
            Vector3 offset = new(Mathf.Cos(angle) * scatter, 0f, Mathf.Sin(angle) * scatter);
            pack.AddMember(baseHp * (float)GD.RandRange(0.9f, 1.12f), archetypes[i], offset, elite);
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
        SkillData? skillReward = _player.Progression.UnlockNextSkill();
        _player.RestoreMana(_player.MaxMana);
        string rewardSummary = BuildRewardSummary(linkReward, skillReward);
        _player.SetObjectiveStatus($"RAID CLEARED  •  {rewardSummary}");
        _player.ShowCombatMessage(rewardSummary);
        _running = false;
        _runNumber++;
        RaidStateChanged?.Invoke(false);
        RefreshPanel();
    }

    private static string BuildRewardSummary(SkillLinkData? linkReward, SkillData? skillReward)
    {
        if (linkReward != null && skillReward != null)
        {
            return $"LINK: {linkReward.DisplayName.ToUpperInvariant()}  •  SKILL: {skillReward.DisplayName.ToUpperInvariant()}";
        }
        if (linkReward != null)
        {
            return $"NEW LINK: {linkReward.DisplayName.ToUpperInvariant()}";
        }
        if (skillReward != null)
        {
            return $"NEW SKILL: {skillReward.DisplayName.ToUpperInvariant()}";
        }
        return "GUARDIAN DEFEATED — PARAGON XP";
    }

    private void RefreshPanel()
    {
        if (_startButton != null)
        {
            _startButton.Disabled = !_unlocked || _running;
            _startButton.Text = _running ? $"RAID FLOOR {_currentFloor}/{MaxFloors}" : $"START DESCENDING RAID  //  RUN {_runNumber}";
        }
        if (_statusLabel != null && !_running)
        {
            _statusLabel.Text = _unlocked
                ? "Ready. Every clear grants the next sealed Link and Skill."
                : "Locked: purge every surface camp once.";
        }
    }
}
